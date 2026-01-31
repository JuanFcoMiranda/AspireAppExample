using Projects;

var builder = DistributedApplication.CreateBuilder(args);

//builder.AddServiceDefaults();

// Obtener el endpoint OTLP del dashboard de Aspire
// El dashboard de Aspire expone OTLP en el puerto 18889 por defecto cuando se ejecuta desde Visual Studio
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:18889";

// Servicio Valkey (Redis-compatible) para caché
var valkey = builder.AddValkey("valkey");

// Base de datos PostgreSQL con volumen persistente
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("aspire-postgres-data")
    .AddDatabase("aspiredb");

// Proyecto FastAPI
var python = builder.AddUvicornApp(
        name: "fastapi",
        appDirectory: "../FastApiService",
        app: "main:app")
    .WithUv()
    .WithEnvironment("SERVICE_NAME", "fastapi")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithReference(valkey)
    .WithReference(postgres);

// Proyecto .NET API que consume el servicio FastAPI
var dotnetApi = builder.AddProject<DotNetApi>("dotnet-api")
    .WithReference(python)
    .WithReference(valkey)
    .WithReference(postgres)
    .WithHttpEndpoint(port: 5001, name: "custom-http");

// Proyecto Vue web que consume la API .NET
// El endpoint OTLP del dashboard de Aspire está disponible en el puerto 18889
builder.AddViteApp("web", appDirectory: "../AspireApp1.Web")
    .WithPnpm()
    .WithExternalHttpEndpoints()
    .WithEnvironment("VITE_OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:18889/v1/traces")
    .WithEnvironment("VITE_API_BASE_URL", dotnetApi.GetEndpoint("custom-http"));

await builder.Build().RunAsync();