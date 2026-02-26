using Scalar.AspNetCore;
using DotNetApi.Data;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Añadir Redis/Valkey client para caché
builder.AddRedisClient("valkey");

// Añadir PostgreSQL con Entity Framework Core
builder.AddNpgsqlDbContext<AppDbContext>("aspiredb");

builder.Services.AddHttpClient("fastapi", client =>
{
    client.BaseAddress = new Uri("http+https://fastapi");
});

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevLocalhost", policy =>
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin))
                return false;
            var uri = new Uri(origin);
            return uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host.EndsWith(".localhost");
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

// Aplicar migraciones automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapDefaultEndpoints();
app.UseCors("DevLocalhost");
app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/call-fastapi", async (IHttpClientFactory factory, IConnectionMultiplexer redis) =>
    {
        var db = redis.GetDatabase();
        var cacheKey = "fastapi:hello-world";

        // Intentar obtener del caché
        var cachedValue = await db.StringGetAsync(cacheKey);

        if (cachedValue.HasValue)
        {
            return Results.Ok(new
            {
                message = "Respuesta desde .NET API (desde caché)",
                fromFastApi = cachedValue.ToString(),
                timestamp = DateTime.UtcNow,
                cached = true
            });
        }

        // Si no está en caché, hacer la llamada a FastAPI
        var client = factory.CreateClient("fastapi");
        var response = await client.GetStringAsync("/hello-world");

        // Guardar en caché por 60 segundos
        await db.StringSetAsync(cacheKey, response, TimeSpan.FromSeconds(60));

        return Results.Ok(new
        {
            message = "Respuesta desde .NET API",
            fromFastApi = response,
            timestamp = DateTime.UtcNow,
            cached = false,
            fastapiUrl = client.BaseAddress
        });
    })
    .WithName("CallFastAPI");

app.MapGet("/hello", () => new { message = "Hola desde .NET" })
.WithName("CallHelloWorld")
.AddOpenApiOperationTransformer((operation, context, ct) =>
{
    // Per-endpoint tweaks
    operation.Summary = "CallHelloWorld";
    operation.Description = "Endpoint de prueba local";
    return Task.CompletedTask;
});

// 🔥 PROXY para trazas de OpenTelemetry desde el navegador
// Recibe trazas del navegador Vue y las reenvía al Dashboard de Aspire
app.MapPost("/api/telemetry/traces", async (HttpContext context) =>
{
    try
    {
        // Leer el body de la petición
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        if (string.IsNullOrEmpty(body))
        {
            return Results.BadRequest(new { error = "Body vacío" });
        }

        // Obtener el endpoint OTLP del Dashboard de Aspire desde variables de entorno
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        if (string.IsNullOrEmpty(otlpEndpoint))
        {
            // Fallback a puerto estándar si no está configurado
            otlpEndpoint = "http://localhost:4318";
        }

        // Asegurar que termine con /v1/traces
        if (!otlpEndpoint.EndsWith("/v1/traces"))
        {
            otlpEndpoint = otlpEndpoint.TrimEnd('/') + "/v1/traces";
        }

        // Convertir HTTPS a HTTP para localhost (evitar problemas de certificado)
        if (otlpEndpoint.StartsWith("https://localhost") || otlpEndpoint.StartsWith("https://127.0.0.1"))
        {
            otlpEndpoint = otlpEndpoint.Replace("https://", "http://");
        }

        Console.WriteLine($"[TELEMETRY PROXY] → Enviando {body.Length} bytes a {otlpEndpoint}");

        // Crear HttpClient y enviar al Dashboard
        var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(otlpEndpoint, content);

        Console.WriteLine($"[TELEMETRY PROXY] ← Status: {(int)response.StatusCode} {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            return Results.Ok(new { success = true, message = "Trazas enviadas correctamente" });
        }
        else
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[TELEMETRY PROXY] ❌ Error: {responseBody}");
            return Results.StatusCode((int)response.StatusCode);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TELEMETRY PROXY] ❌ Excepción: {ex.Message}");
        return Results.Problem(
            title: "Error en el proxy de telemetría",
            detail: ex.Message,
            statusCode: 503
        );
    }
})
.AllowAnonymous()
.WithName("TelemetryProxy")
.WithOpenApi();

await app.RunAsync();