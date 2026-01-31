using Scalar.AspNetCore;
using DotNetApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Añadir Redis/Valkey client para caché
builder.AddRedisClient("valkey");

// Añadir PostgreSQL con Entity Framework Core
builder.AddNpgsqlDbContext<AppDbContext>("aspiredb");

// No configurar BaseAddress, se obtendrá dinámicamente de la configuración
builder.Services.AddHttpClient("fastapi");

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

app.MapGet("/call-fastapi", async (IHttpClientFactory factory, StackExchange.Redis.IConnectionMultiplexer redis, IConfiguration configuration) =>
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

    // Obtener la URL del servicio FastAPI desde la configuración de Aspire
    // Aspire inyecta las URLs como variables de entorno simples
    var fastapiUrl = configuration["FASTAPI_HTTP"]; // Primera opción: variable de entorno simple

    // Remover cualquier trailing slash
    //fastapiUrl = fastapiUrl.TrimEnd('/');

    // Si no está en caché, hacer la llamada a FastAPI
    var client = factory.CreateClient("fastapi");
    var response = await client.GetStringAsync($"{fastapiUrl}/hello-world");

    // Guardar en caché por 60 segundos
    await db.StringSetAsync(cacheKey, response, TimeSpan.FromSeconds(60));

    return Results.Ok(new
    {
        message = "Respuesta desde .NET API",
        fromFastApi = response,
        timestamp = DateTime.UtcNow,
        cached = false,
        fastapiUrl = fastapiUrl // Añadir para debug
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

    // Endpoints para TodoItems (CRUD con PostgreSQL)
    app.MapGet("/todos", async (AppDbContext db) =>
    {
        var todos = await db.TodoItems.ToListAsync();
        return Results.Ok(todos);
    })
    .WithName("GetTodos")
    .AddOpenApiOperationTransformer((operation, context, ct) =>
    {
        operation.Summary = "GetTodos";
        operation.Description = "Obtiene todos los items de la lista de tareas desde PostgreSQL";
        return Task.CompletedTask;
    });

    app.MapGet("/todos/{id}", async (int id, AppDbContext db) =>
    {
        var todo = await db.TodoItems.FindAsync(id);
        return todo is not null ? Results.Ok(todo) : Results.NotFound();
    })
    .WithName("GetTodoById")
    .AddOpenApiOperationTransformer((operation, context, ct) =>
    {
        operation.Summary = "GetTodoById";
        operation.Description = "Obtiene un item específico por su ID";
        return Task.CompletedTask;
    });

    app.MapPost("/todos", async (TodoItem todo, AppDbContext db) =>
    {
        db.TodoItems.Add(todo);
        await db.SaveChangesAsync();
        return Results.Created($"/todos/{todo.Id}", todo);
    })
    .WithName("CreateTodo")
    .AddOpenApiOperationTransformer((operation, context, ct) =>
    {
        operation.Summary = "CreateTodo";
        operation.Description = "Crea un nuevo item en la lista de tareas";
        return Task.CompletedTask;
    });

    app.MapPut("/todos/{id}", async (int id, TodoItem updatedTodo, AppDbContext db) =>
    {
        var todo = await db.TodoItems.FindAsync(id);
        if (todo is null) return Results.NotFound();

        todo.Title = updatedTodo.Title;
        todo.IsCompleted = updatedTodo.IsCompleted;
        await db.SaveChangesAsync();

        return Results.Ok(todo);
    })
    .WithName("UpdateTodo")
    .AddOpenApiOperationTransformer((operation, context, ct) =>
    {
        operation.Summary = "UpdateTodo";
        operation.Description = "Actualiza un item existente";
        return Task.CompletedTask;
    });

    app.MapDelete("/todos/{id}", async (int id, AppDbContext db) =>
    {
        var todo = await db.TodoItems.FindAsync(id);
        if (todo is null) return Results.NotFound();

        db.TodoItems.Remove(todo);
        await db.SaveChangesAsync();

        return Results.NoContent();
    })
    .WithName("DeleteTodo")
    .AddOpenApiOperationTransformer((operation, context, ct) =>
    {
        operation.Summary = "DeleteTodo";
        operation.Description = "Elimina un item de la lista de tareas";
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

// Endpoint de diagnóstico para ver las configuraciones disponibles
app.MapGet("/debug/config", (IConfiguration configuration) =>
{
    var allConfigs = new Dictionary<string, string>();

    // Obtener todas las claves de configuración relacionadas con servicios
    foreach (var item in configuration.AsEnumerable())
    {
        if (item.Key.Contains("fastapi", StringComparison.OrdinalIgnoreCase) || 
            item.Key.Contains("services", StringComparison.OrdinalIgnoreCase) ||
            item.Key.Contains("ConnectionStrings", StringComparison.OrdinalIgnoreCase))
        {
            allConfigs[item.Key] = item.Value ?? "null";
        }
    }

    return Results.Ok(new
    {
        message = "Configuraciones disponibles relacionadas con fastapi",
        configurations = allConfigs,
        timestamp = DateTime.UtcNow
    });
})
.WithName("DebugConfig")
.AddOpenApiOperationTransformer((operation, context, ct) =>
{
    operation.Summary = "DebugConfig";
    operation.Description = "Muestra las configuraciones disponibles para debugging";
    return Task.CompletedTask;
});

app.MapDelete("/cache/clear", async (StackExchange.Redis.IConnectionMultiplexer redis) =>
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