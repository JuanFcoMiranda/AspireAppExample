# Configuración de Telemetría Aspire para Vue.js

Este proyecto está configurado para enviar telemetría (trazas) a .NET Aspire Dashboard usando OpenTelemetry.

## 📋 Requisitos previos

1. Tener .NET Aspire Dashboard ejecutándose (normalmente en el puerto 4318 para HTTP/Protobuf)
2. Node.js 18+ y pnpm instalado

## 🔧 Configuración

### 1. Variables de entorno

El proyecto utiliza las siguientes variables de entorno en el archivo `.env`:

```env
# Endpoint OTLP donde Aspire Dashboard escucha las trazas
VITE_OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318/v1/traces

# Nombre del servicio (aparecerá en el Dashboard)
VITE_OTEL_SERVICE_NAME=aspireapp1-web

# Versión del servicio
VITE_OTEL_SERVICE_VERSION=0.1.0
```

### 2. Ajustar el endpoint para tu configuración Aspire

Si tu proyecto Aspire está configurado en puertos diferentes:

- **HTTP/Protobuf (recomendado)**: `http://localhost:4318/v1/traces`
- **gRPC**: `http://localhost:4317/v1/traces`

Actualiza la variable `VITE_OTEL_EXPORTER_OTLP_ENDPOINT` en tu archivo `.env`.

### 3. Conectar con el AppHost de Aspire

En tu proyecto AppHost de .NET Aspire, asegúrate de agregar este proyecto frontend:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Agregar el proyecto Vue.js frontend
var frontend = builder.AddNpmApp("frontend", "../AspireApp1.Web")
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithExternalHttpEndpoints();

// O si estás usando un proyecto Vite/Vue independiente
// var frontend = builder.AddProject<Projects.AspireApp1_Web>("frontend");

builder.Build().Run();
```

## 🚀 Ejecución

1. **Iniciar el AppHost de Aspire**:
   ```bash
   cd ../AspireApp1.AppHost
   dotnet run
   ```
   Esto iniciará el Dashboard de Aspire (normalmente en `http://localhost:15xxx`)

2. **Iniciar el proyecto Vue.js**:
   ```bash
   pnpm install
   pnpm dev
   ```

3. **Verificar la telemetría**:
   - Abre el Dashboard de Aspire
   - Navega a la sección "Traces"
   - Deberías ver las trazas del servicio `aspireapp1-web`
   - Las llamadas HTTP (fetch/XHR) se instrumentan automáticamente

## 📊 ¿Qué se monitorea?

El proyecto está configurado para capturar automáticamente:

- ✅ Todas las llamadas `fetch()`
- ✅ Todas las peticiones `XMLHttpRequest`
- ✅ Información del servicio (nombre, versión)
- ✅ Propagación de contexto de trazas (trace context headers)

## 🔍 Debugging

Para ver logs de OpenTelemetry en la consola del navegador, abre DevTools. Verás mensajes como:

```
🔧 Inicializando OpenTelemetry...
✓ Resource creado: aspireapp1-web 0.1.0
✓ OTLPTraceExporter creado
✓ BatchSpanProcessor creado
✓ WebTracerProvider creado con spanProcessors y recursos
✓ Provider registrado
✓ Instrumentaciones registradas
✅ OpenTelemetry inicializado correctamente
📡 Endpoint OTLP: http://localhost:4318/v1/traces
```

## ⚠️ Troubleshooting

### Error de CORS

Si ves errores de CORS al enviar trazas, asegúrate de que el Dashboard de Aspire permita CORS desde tu origen:

```csharp
// En tu AppHost
builder.AddProject<Projects.AspireApp1_Web>("frontend")
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:5173");
```

### No se ven trazas en el Dashboard

1. Verifica que el endpoint OTLP esté correcto
2. Comprueba la consola del navegador para errores
3. Asegúrate de que el Dashboard de Aspire esté corriendo
4. Verifica que el puerto 4318 esté accesible

### Error "Cannot find module '@opentelemetry/...'"

Ejecuta:
```bash
pnpm install
```

Luego reinicia el servidor de desarrollo y el IDE.

## 📦 Paquetes utilizados

- `@opentelemetry/api` - API principal de OpenTelemetry
- `@opentelemetry/sdk-trace-web` - SDK de trazas para navegador
- `@opentelemetry/exporter-trace-otlp-proto` - Exportador OTLP (Protobuf)
- `@opentelemetry/instrumentation-fetch` - Auto-instrumentación de fetch
- `@opentelemetry/instrumentation-xml-http-request` - Auto-instrumentación de XHR
- `@opentelemetry/resources` - Gestión de recursos del servicio
- `@opentelemetry/semantic-conventions` - Convenciones semánticas de OpenTelemetry

## 🎯 Próximos pasos

- Agregar trazas manuales en tu código Vue.js usando `@opentelemetry/api`
- Configurar métricas personalizadas
- Agregar logs estructurados
- Configurar sampling para producción

