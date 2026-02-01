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
.WithName("CallFastAPI")
.AddOpenApiOperationTransformer((operation, context, ct) =>
{
    // Per-endpoint tweaks
    operation.Summary = "CallFastAPI";
    operation.Description = "Llama al endpoint /hello-world de FastAPI y devuelve la respuesta";
        return Task.CompletedTask;
    });

    app.MapGet("/hello", () => new { message = "Hola desde .NET" })
.WithName("CallHelloWorld")
.AddOpenApiOperationTransformer((operation, context, ct) =>
{
    // Per-endpoint tweaks
    operation.Summary = "CallHelloWorld";
    operation.Description = "Endpoint de prueba local";
    return Task.CompletedTask;
});


app.MapDelete("/cache/clear", async (IConnectionMultiplexer redis) =>
{
    var db = redis.GetDatabase();
    await db.KeyDeleteAsync("fastapi:hello-world");
    return Results.Ok(new { message = "Caché limpiado", timestamp = DateTime.UtcNow });
})
.WithName("ClearCache")
.AddOpenApiOperationTransformer((operation, context, ct) =>
{
    operation.Summary = "ClearCache";
    operation.Description = "Limpia el caché de las llamadas a FastAPI";
    return Task.CompletedTask;
});

await app.RunAsync();