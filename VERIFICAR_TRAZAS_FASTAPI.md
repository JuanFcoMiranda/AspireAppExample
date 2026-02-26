# ✅ GUÍA RÁPIDA: Verificar Trazas de FastAPI

## 🔧 Cambios Aplicados

### main.py - SIMPLIFICADO
- ❌ Eliminada instrumentación duplicada
- ❌ Eliminado TracerProvider duplicado
- ✅ Ahora `configure_telemetry()` hace TODO el trabajo
- ✅ Agregado endpoint `/telemetry-test` para pruebas

## 🚀 PASOS PARA VERIFICAR (EN ORDEN)

### 1️⃣ Reinicia la Aplicación Aspire

**IMPORTANTE**: Detén completamente y vuelve a arrancar.

```bash
# CTRL+C para detener
# Luego vuelve a ejecutar desde Visual Studio o CLI
```

### 2️⃣ Verifica los Logs de Inicio de FastAPI

En la consola/terminal donde corre FastAPI, deberías ver:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔧 Configurando OpenTelemetry para FastAPI
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 Variables de entorno detectadas:
   OTEL_EXPORTER_OTLP_ENDPOINT = https://localhost:XXXX
   OTEL_EXPORTER_OTLP_TRACES_ENDPOINT = ...
   OTEL_SERVICE_NAME = fastapi

✅ Endpoint OTLP detectado correctamente: https://localhost:XXXX
✅ Resource creado con service.name = 'fastapi'
✅ Traces configurado: http://localhost:XXXX/v1/traces
✅ Metrics configurado: http://localhost:XXXX/v1/metrics
✅ Logs configurado: http://localhost:XXXX/v1/logs
✅ Logging handler agregado
✅ FastAPI instrumentado

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ OpenTelemetry configurado correctamente
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**✅ Si ves esto:** OpenTelemetry está configurado correctamente.

**❌ Si ves `⚠️ NINGUNA variable configurada por Aspire`:**
- Significa que `.WithOtlpExporter()` no está funcionando
- Verifica que esté en `AppHost.cs` línea 26-27
- Reinicia Aspire completamente

### 3️⃣ Encuentra el Puerto de FastAPI

En el Dashboard de Aspire:
1. Ve a la página principal (lista de servicios)
2. Busca el servicio **fastapi**
3. Copia la URL (ej: `http://localhost:PORT`)

### 4️⃣ Haz Peticiones de Prueba

#### Opción A: Navegador (más fácil)

```
http://localhost:PORT/telemetry-test
```

Este endpoint crea una traza de prueba explícita.

#### Opción B: curl

```bash
curl http://localhost:PORT/hello-world
curl http://localhost:PORT/health
curl http://localhost:PORT/telemetry-test
```

### 5️⃣ Espera 5-10 Segundos

Los exportadores envían trazas en **batches cada 5 segundos**.

No busques inmediatamente en el Dashboard.

### 6️⃣ Verifica en el Dashboard de Aspire

1. **Abre el Dashboard**: `http://localhost:15888` (o el puerto asignado)
2. **Ve a "Traces"**
3. **Busca el servicio**: `fastapi`
4. **Deberías ver:**
   - `GET /hello-world`
   - `GET /health`
   - `GET /telemetry-test`
   - `manual-test-span` (de la traza manual)

## 🐛 TROUBLESHOOTING

### ❌ Problema 1: No veo logs de inicio de FastAPI

**Causa**: FastAPI no se está iniciando correctamente.

**Solución**:
1. Verifica que FastAPI aparezca en el Dashboard de Aspire
2. Busca errores en los logs del AppHost
3. Verifica que los paquetes de OpenTelemetry estén instalados:
   ```bash
   cd FastApiService
   uv pip list | grep opentelemetry
   ```

### ❌ Problema 2: Veo "⚠️ NINGUNA variable configurada por Aspire"

**Causa**: `.WithOtlpExporter()` no está funcionando.

**Solución**:

Verifica `AppHost.cs` línea 20-28:

```csharp
var fastApi = builder.AddUvicornApp(
        name: "fastapi",
        appDirectory: "../FastApiService",
        app: "main:app")
    .WithUv()
    .WithReference(postgres)
    .WithExternalHttpEndpoints()
    .WithOtlpExporter();           // ← DEBE ESTAR AQUÍ
```

Si falta, agrégalo y reinicia.

### ❌ Problema 3: Logs correctos pero no aparecen trazas

**Verifica el endpoint:**

En los logs de FastAPI, busca:
```
✅ Traces configurado: http://localhost:XXXX/v1/traces
```

¿El puerto es correcto? ¿Es HTTP o HTTPS?

**Prueba manual:**

```bash
# Verifica que el endpoint responda
curl -X POST http://localhost:XXXX/v1/traces \
  -H "Content-Type: application/json" \
  -d '{"test": "data"}'
```

Si da error 404 o Connection Refused, el puerto es incorrecto.

### ❌ Problema 4: Error SSL/TLS

Si ves en los logs de FastAPI:
```
SSL: CERTIFICATE_VERIFY_FAILED
```

**Ya está solucionado** en `telemetry.py` con:
```python
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
```

Y el código convierte HTTPS → HTTP automáticamente para localhost.

### ❌ Problema 5: Trazas aparecen pero vacías

Si ves trazas pero sin detalles, puede ser que:
1. Los spans no se estén creando correctamente
2. La instrumentación no se aplicó

**Solución**: Usa el endpoint `/telemetry-test` que crea una traza explícita.

## 📊 VERIFICACIÓN COMPLETA

Ejecuta **todos** estos endpoints en orden:

```bash
PORT=5001  # Reemplaza con el puerto de tu FastAPI

curl http://localhost:$PORT/health
curl http://localhost:$PORT/hello-world
curl http://localhost:$PORT/simulate-error
curl http://localhost:$PORT/telemetry-test
```

Espera **10 segundos** y luego verifica en el Dashboard:

- [ ] Servicio "fastapi" aparece en la lista ✅
- [ ] Sección "Traces" muestra trazas de fastapi ✅
- [ ] Ves `GET /health` ✅
- [ ] Ves `GET /hello-world` ✅
- [ ] Ves `GET /telemetry-test` ✅
- [ ] Ves `manual-test-span` (span creado manualmente) ✅
- [ ] Sección "Logs" muestra logs de fastapi ✅
- [ ] Logs muestran "hello-world endpoint called" ✅

## 🎯 RESULTADO ESPERADO

En el Dashboard de Aspire → Traces → Servicio "fastapi":

```
┌─────────────────────────────────────────────────┐
│ Traces for: fastapi                             │
├─────────────────────────────────────────────────┤
│ GET /hello-world            [200ms] [200 OK]   │
│ GET /health                 [50ms]  [200 OK]   │
│ GET /telemetry-test         [100ms] [200 OK]   │
│   └─ manual-test-span       [5ms]              │
│ GET /simulate-error         [80ms]  [200 OK]   │
└─────────────────────────────────────────────────┘
```

## 💡 TIPS

1. **Primero verifica los logs de inicio** - Si ves los ✅, está configurado
2. **Espera 10 segundos** después de hacer peticiones
3. **Refresca el Dashboard** (F5) si no ves trazas
4. **Usa `/telemetry-test`** para crear trazas explícitas
5. **Verifica el filtro de tiempo** en el Dashboard (debe incluir últimos minutos)

## 🆘 SI NADA FUNCIONA

Comparte:
1. **Logs completos de inicio de FastAPI** (desde "🔧 Configurando...")
2. **Screenshot del Dashboard** (sección Traces)
3. **Puerto de FastAPI** que estás usando
4. **Resultado de**: `curl http://localhost:PORT/telemetry-test`

---

💡 **IMPORTANTE**: Después de cualquier cambio en `AppHost.cs`, **SIEMPRE reinicia completamente Aspire** (no solo FastAPI).
