# ✅ Checklist - Configuración de Telemetría Aspire

## 📋 Verificación Paso a Paso

Usa este checklist para asegurarte de que todo está configurado correctamente.

### ✅ Paso 1: Verificar archivos modificados

- [x] `src/telemetry.ts` - Sin valores hardcodeados, lee de `import.meta.env` y `window`
- [x] `src/vite-env.d.ts` - Tipos TypeScript para `Window` e `ImportMetaEnv`
- [x] `index.html` - Script para recibir variables de Aspire
- [x] `.env` - Variables para desarrollo local
- [x] `public/test-env.html` - Página de test de variables

### ✅ Paso 2: Compilación exitosa

```bash
cd AspireApp1.Web
pnpm install
pnpm run build
```

**Resultado esperado:** `✓ built in X.XXs`

### ✅ Paso 3: Configurar el AppHost de Aspire

**Ubicación**: `../AspireApp1.AppHost/Program.cs` (o similar)

**Código necesario:**
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] 
    ?? "http://localhost:4318/v1/traces";

var frontend = builder.AddNpmApp("frontend", "../AspireApp1.Web")
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithExternalHttpEndpoints()
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otlpEndpoint)
    .WithEnvironment("OTEL_SERVICE_NAME", "aspireapp1-web")
    .WithEnvironment("OTEL_SERVICE_VERSION", "0.1.0");

builder.Build().Run();
```

**Ver documentación completa**: `ASPIRE_APPHOST_CONFIG.md`

- [ ] AppHost configurado con `.WithEnvironment()` para las 3 variables
- [ ] AppHost compila sin errores
- [ ] AppHost se ejecuta correctamente

### ✅ Paso 4: Ejecutar desde Aspire

```bash
cd ../AspireApp1.AppHost
dotnet run
```

**Verificar:**
- [ ] El Dashboard de Aspire se abre automáticamente
- [ ] El servicio `frontend` aparece en el Dashboard
- [ ] El estado del servicio es "Running" (verde)

### ✅ Paso 5: Verificar inyección de variables

**Opción A: Página de test**
1. Abre `http://localhost:5173/test-env.html` (o el puerto configurado)
2. Verifica que muestre:
   - [ ] ✅ `window.OTEL_EXPORTER_OTLP_ENDPOINT` configurado
   - [ ] ✅ `window.OTEL_SERVICE_NAME` configurado
   - [ ] ✅ `window.OTEL_SERVICE_VERSION` configurado

**Opción B: Consola del navegador**
1. Abre la aplicación Vue.js (`http://localhost:5173`)
2. Abre DevTools (F12) → Console
3. Ejecuta:
```javascript
console.log(window.OTEL_EXPORTER_OTLP_ENDPOINT);
console.log(window.OTEL_SERVICE_NAME);
console.log(window.OTEL_SERVICE_VERSION);
```
4. Verifica que muestre los valores correctos (no `undefined`)

### ✅ Paso 6: Verificar inicialización de OpenTelemetry

En la consola del navegador, deberías ver:

```
🔧 Inicializando OpenTelemetry...
📍 Variables de entorno detectadas:
   - Endpoint OTLP: http://localhost:4318/v1/traces
   - Service Name: aspireapp1-web
   - Service Version: 0.1.0
✓ Resource creado: aspireapp1-web 0.1.0
✓ OTLPTraceExporter creado
✓ BatchSpanProcessor creado
✓ WebTracerProvider creado con spanProcessors y recursos
✓ Provider registrado
✓ Instrumentaciones registradas
✅ OpenTelemetry inicializado correctamente
📡 Endpoint OTLP: http://localhost:4318/v1/traces
```

- [ ] Mensaje "✅ OpenTelemetry inicializado correctamente" aparece
- [ ] No hay advertencias "⚠️ OTEL_* no está configurado"
- [ ] El endpoint OTLP es correcto

### ✅ Paso 7: Verificar trazas en el Dashboard

1. Abre el Dashboard de Aspire (normalmente `http://localhost:15xxx`)
2. Ve a la sección **"Traces"** en el menú lateral
3. En la aplicación Vue.js, haz algunas acciones (navegar, hacer peticiones HTTP)
4. Verifica en el Dashboard:
   - [ ] El servicio `aspireapp1-web` aparece en la lista de servicios
   - [ ] Las trazas aparecen en tiempo real
   - [ ] Las peticiones HTTP se muestran correctamente
   - [ ] Puedes hacer clic en una traza para ver detalles

### ✅ Paso 8: Verificar peticiones HTTP (Network tab)

1. En DevTools → Network tab
2. Filtra por "traces" o "v1/traces"
3. Recarga la página o haz una acción
4. Verifica:
   - [ ] Peticiones POST a `http://localhost:4318/v1/traces`
   - [ ] Status: 200 OK (o 202 Accepted)
   - [ ] No hay errores 403, 404, o CORS

## 🔧 Troubleshooting

### ❌ Variables no están en `window`

**Síntoma**: En test-env.html o consola, `window.OTEL_*` son `undefined`

**Causas posibles:**
1. No estás ejecutando desde Aspire AppHost → Usa `.env` con variables `VITE_*`
2. AppHost no configurado correctamente → Revisa `ASPIRE_APPHOST_CONFIG.md`
3. Aspire no está inyectando las variables → Verifica logs del AppHost

**Solución temporal**: Usa variables de `.env`:
```env
VITE_OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318/v1/traces
VITE_OTEL_SERVICE_NAME=aspireapp1-web
VITE_OTEL_SERVICE_VERSION=0.1.0
```

### ❌ "Telemetría deshabilitada" en consola

**Síntoma**: Ves advertencias `⚠️ OTEL_* no está configurado`

**Causa**: Ni `import.meta.env.VITE_*` ni `window.OTEL_*` están configurados

**Solución**:
1. Si ejecutas con `pnpm dev`: Configura `.env`
2. Si ejecutas desde Aspire: Configura AppHost

### ❌ No aparecen trazas en Dashboard

**Síntomas posibles:**
- OpenTelemetry se inicializa correctamente
- No hay errores en consola
- Pero no se ven trazas en el Dashboard

**Verificar:**
1. **Endpoint correcto**:
   - HTTP/Protobuf: `http://localhost:4318/v1/traces` ✓
   - gRPC: `http://localhost:4317/v1/traces`
   
2. **Dashboard está escuchando**:
   - Abre el Dashboard de Aspire
   - Ve a "Traces" - ¿aparece la sección?
   - Revisa logs del AppHost

3. **Peticiones llegan al servidor**:
   - Network tab → busca peticiones a `/v1/traces`
   - Status debe ser 200 o 202
   - Si es 404: endpoint incorrecto
   - Si es 403 o CORS: problema de permisos

4. **Puerto correcto del Dashboard**:
   - El Dashboard puede usar puertos diferentes según la configuración
   - Verifica en los logs del AppHost cuál es el puerto OTLP
   - Actualiza el endpoint si es necesario

### ❌ Error de CORS

**Síntoma**: En Network tab ves errores "CORS policy"

**Causa**: El Dashboard de Aspire no permite peticiones desde tu origen

**Solución**:
1. Verifica que estés ejecutando desde el mismo origen que Aspire
2. O configura CORS en el Dashboard (normalmente automático con Aspire)

## 📚 Documentación de Referencia

- **[ASPIRE_APPHOST_CONFIG.md](ASPIRE_APPHOST_CONFIG.md)** - Configurar AppHost (⭐ importante)
- **[CONFIGURACION_FINAL.md](CONFIGURACION_FINAL.md)** - Resumen de cambios
- **[INICIO_RAPIDO.md](INICIO_RAPIDO.md)** - Inicio rápido
- **[ASPIRE_SETUP.md](ASPIRE_SETUP.md)** - Setup detallado

## ✅ Todo OK

Si todos los pasos anteriores funcionan:

- ✅ Variables configuradas
- ✅ OpenTelemetry inicializado
- ✅ Trazas visibles en Dashboard
- ✅ Peticiones HTTP monitoreadas

**¡Felicidades! Tu aplicación Vue.js está completamente integrada con Aspire Dashboard! 🎉**

## 🆘 ¿Necesitas ayuda?

Si sigues teniendo problemas después de seguir este checklist:

1. Revisa los logs del AppHost de Aspire
2. Revisa la consola del navegador completa
3. Verifica el Network tab para ver las peticiones
4. Consulta la documentación de Aspire: https://learn.microsoft.com/dotnet/aspire/

## 📝 Notas Finales

- **Desarrollo local sin Aspire**: Usa variables en `.env` con prefijo `VITE_`
- **Producción/Staging con Aspire**: Las variables se inyectan automáticamente en `window`
- **Sin configuración**: La telemetría se deshabilita automáticamente (la app sigue funcionando)

