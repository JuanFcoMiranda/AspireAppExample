# ✅ Solución: Trazas de FastAPI al Dashboard de Aspire

## 🚨 Problemas Encontrados

1. **AppHost.cs**: El servicio `fastapi` NO tenía `.WithOtlpExporter()`
2. **telemetry.py**: Usaba endpoint hardcodeado `http://localhost:18889` (incorrecto)

## ✅ Cambios Aplicados

### 1. AppHost.cs
```csharp
// Antes (❌):
var fastApi = builder.AddUvicornApp(...)
    .WithUv()
    .WithReference(postgres);

// Ahora (✅):
var fastApi = builder.AddUvicornApp(...)
    .WithUv()
    .WithReference(postgres)
    .WithExternalHttpEndpoints()    // ← Agregado
    .WithOtlpExporter();            // ← Agregado (CRÍTICO)
```

### 2. telemetry.py
```python
# Antes (❌):
otlp_endpoint = os.getenv("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:18889")

# Ahora (✅):
otlp_endpoint = os.getenv("OTEL_EXPORTER_OTLP_ENDPOINT")
# Con fallback inteligente y logging de diagnóstico
```

### 3. Logging de Diagnóstico
Agregado logging para ver en la consola de FastAPI:
- ✅ Endpoint de trazas usado
- ✅ Endpoint de métricas usado
- ✅ Endpoint de logs usado

## 🚀 Pasos para Verificar

### 1. Reinicia la Aplicación Aspire

Detén y vuelve a arrancar desde el AppHost.

### 2. Verifica los Logs de FastAPI

En la consola donde corre FastAPI, deberías ver:

```
✅ Using OTLP endpoint from Aspire: https://localhost:XXXX
✅ Traces endpoint: https://localhost:XXXX/v1/traces
✅ Metrics endpoint: https://localhost:XXXX/v1/metrics
✅ Logs endpoint: https://localhost:XXXX/v1/logs
```

Si ves `⚠️ OTEL_EXPORTER_OTLP_ENDPOINT not configured`, significa que Aspire no está pasando la variable correctamente (pero con `.WithOtlpExporter()` debería hacerlo).

### 3. Genera Trazas en FastAPI

Haz peticiones a los endpoints:

```bash
# Desde otro terminal o navegador:
curl http://localhost:PORT/hello-world
curl http://localhost:PORT/health
curl http://localhost:PORT/simulate-error
```

O simplemente abre en el navegador:
```
http://localhost:PORT/hello-world
```

### 4. Verifica en el Dashboard de Aspire

1. Abre el Dashboard: `http://localhost:15888` (o el puerto asignado)
2. Ve a la sección **Traces**
3. Busca el servicio: **fastapi**
4. Deberías ver:
   - Traza de `/hello-world`
   - Traza de `/health`
   - Traza de `/simulate-error`

### 5. Verifica en Logs

1. En el Dashboard → **Logs**
2. Filtra por servicio: **fastapi**
3. Deberías ver:
   - `hello-world endpoint called`
   - `Health check called`
   - `This is a simulated warning`
   - `This is a simulated error`

## 🐛 Troubleshooting

### Problema 1: Aún no aparecen trazas

**Verifica en los logs de FastAPI:**

```
✅ Using OTLP endpoint from Aspire: ...
```

Si no aparece, significa que la variable de entorno no se está configurando.

**Solución:**
1. Verifica que el AppHost tenga `.WithOtlpExporter()` (ya lo agregamos)
2. Reinicia completamente la aplicación Aspire
3. Verifica en el Dashboard que el servicio `fastapi` aparezca en la lista

### Problema 2: Error de conexión SSL/TLS

Si ves errores como:
```
SSL: CERTIFICATE_VERIFY_FAILED
```

**Causa**: El endpoint usa HTTPS con certificado autofirmado.

**Solución temporal (solo desarrollo)**:

Agrega al inicio de `telemetry.py`:

```python
import ssl
ssl._create_default_https_context = ssl._create_unverified_context
```

O mejor, en `telemetry.py`, modifica los exportadores:

```python
from opentelemetry.exporter.otlp.proto.http.trace_exporter import OTLPSpanExporter

# Agregar parámetro para ignorar SSL en desarrollo
trace_exporter = OTLPSpanExporter(
    endpoint=f"{otlp_endpoint}/v1/traces",
    session=None,  # Usar sesión por defecto
)
```

### Problema 3: Puerto incorrecto

Si el endpoint que FastAPI detecta es incorrecto, puedes sobrescribirlo manualmente en `AppHost.cs`:

```csharp
var fastApi = builder.AddUvicornApp(...)
    .WithUv()
    .WithReference(postgres)
    .WithExternalHttpEndpoints()
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4318") // ← Puerto correcto
    .WithOtlpExporter();
```

**NOTA**: El puerto puede ser diferente. Busca en los logs de inicio del Dashboard el texto "OTLP" para encontrar el puerto correcto.

## ✅ Verificación Final

Ejecuta estos comandos para generar trazas:

```bash
# Terminal 1: Ver logs de FastAPI en tiempo real
# (en el Dashboard de Aspire → Logs → Filtrar por "fastapi")

# Terminal 2: Generar trazas
curl http://localhost:PORT/hello-world
curl http://localhost:PORT/health
curl http://localhost:PORT/simulate-error
```

Luego verifica:
- [ ] Dashboard → Traces → Servicio "fastapi" ✅
- [ ] Dashboard → Logs → Servicio "fastapi" ✅
- [ ] Dashboard → Metrics → Servicio "fastapi" ✅

## 📊 Comparación: Antes vs. Ahora

| Componente | Antes | Ahora |
|------------|-------|-------|
| **dotnet-api** | ✅ Con `.WithOtlpExporter()` | ✅ Sin cambios |
| **fastapi** | ❌ Sin `.WithOtlpExporter()` | ✅ Con `.WithOtlpExporter()` |
| **web** | ⚠️ Intentaba enviar (no funciona) | ✅ Desactivado (correcto) |

## 🎯 Estado Final

Todos los servicios backend deberían enviar trazas al Dashboard:

- ✅ **dotnet-api** → Trazas visibles
- ✅ **fastapi** → Trazas visibles (después de los cambios)
- ✅ **postgres** → Conexiones visibles
- ✅ **valkey** → Comandos visibles
- ⚠️ **web** → No envía trazas (por diseño, usa Chrome DevTools)

---

💡 **TIP**: Si después de reiniciar siguen sin aparecer las trazas, comparte los logs de la consola de FastAPI (las líneas que empiezan con ✅ o ⚠️) para diagnosticar el problema.
