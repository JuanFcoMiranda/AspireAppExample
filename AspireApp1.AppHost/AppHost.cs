using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Servicio Valkey (Redis-compatible) para caché
var valkey = builder.AddValkey("valkey")
    .WithDataVolume();

var username = builder.AddParameter("username", secret: true);
var password = builder.AddParameter("password", secret: true);

// Base de datos PostgreSQL con volumen persistente
var postgres = builder.AddPostgres("postgres")
    .WithUserName(username)
    .WithPassword(password)
    .WithDataVolume("aspire-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("aspireDb");

// Proyecto FastAPI
var fastApi = builder.AddUvicornApp(
        name: "fastapi",
        appDirectory: "../FastApiService",
        app: "main:app")
    .WithUv()
    .WithReference(postgres)
    .WithExternalHttpEndpoints()
    .WithOtlpExporter();

// Proyecto .NET API que consume el servicio FastAPI
var dotnetApi = builder.AddProject<DotNetApi>("dotnet-api")
    .WithReference(fastApi)
    .WaitFor(fastApi)
    .WithReference(valkey)
    .WaitFor(valkey)
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithExternalHttpEndpoints()
    .WithOtlpExporter();

// Proyecto Vue web que consume la API .NET
// La telemetría del navegador está desactivada (ver telemetry.ts)
// Usa Chrome DevTools y Vue DevTools para debugging del frontend
builder.AddViteApp("web", appDirectory: "../AspireApp1.Web")
    .WithPnpm()
    .WithViteConfig("./vite.config.ts")
    .WithReference(dotnetApi)
    .WaitFor(dotnetApi)
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();