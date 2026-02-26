# 🚨 SOLUCIÓN SIMPLIFICADA - Trazas Web

## El Problema

Los navegadores **NO PUEDEN** enviar trazas directamente al Dashboard de Aspire por CORS.

## La Solución

Usar un **PROXY** en el backend (.NET API) que reenvía las trazas.

```
Navegador → dotnet-api/api/telemetry/traces → Aspire Dashboard
         (sin CORS)                           (funciona)
```

## 🚀 PASOS PARA PROBAR

### 1. Reinicia TODO
```bash
# Detén la aplicación Aspire (CTRL+C)
# Vuélvela a iniciar desde Visual Studio o CLI
```

### 2. Abre el Navegador
- Ve a la aplicación web (ej: `http://localhost:5173`)
- Abre **DevTools (F12)**
- Ve a la pestaña **Console**

### 3. Verifica los Logs en Console

✅ **SI FUNCIONA**, verás:
```
🔧 Inicializando OpenTelemetry...
📦 Modo: PROXY (backend .NET)
📍 Configuración:
   - Proxy URL: http://localhost:XXXX/api/telemetry/traces
   - Servicio: web
✓ Exporter creado → http://localhost:XXXX/api/telemetry/traces
✓ BatchSpanProcessor creado
✓ Provider registrado
✓ Instrumentaciones registradas
✅ OpenTelemetry inicializado!
🎯 Traza inicial enviada
```

❌ **SI NO FUNCIONA**, verás errores. Copia TODO y compártelo.

### 4. Verifica Network Tab
1. Ve a la pestaña **Network** en DevTools
2. Filtra por: **"telemetry"**
3. Haz click en la página
4. Espera 2-3 segundos
5. Deberías ver: **POST /api/telemetry/traces**
6. Status: **200 OK** (verde)

❌ **Si ves 503 o error de red**: El dotnet-api no está corriendo o no es accesible

### 5. Verifica los Logs del dotnet-api

En la consola/terminal donde corre el dotnet-api, busca:
```
[TELEMETRY PROXY] → Enviando XXX bytes a http://localhost:4318/v1/traces
[TELEMETRY PROXY] ← Status: 200 OK
[TELEMETRY PROXY] ✅ Trazas enviadas correctamente
```

✅ **Si ves esto**: Las trazas están llegando al Dashboard

❌ **Si ves errores**: Comparte los logs

### 6. Verifica el Dashboard de Aspire
1. Abre el Dashboard (ej: `http://localhost:15888`)
2. Ve a **Traces**
3. Busca servicio: **web**
4. Deberías ver:
   - Traza: `app-initialization`
   - Trazas de clicks cuando interactúes

## 🐛 TROUBLESHOOTING

### Problema 1: "Proxy URL: undefined/api/telemetry/traces"

**Causa**: No se detecta la URL del dotnet-api

**Solución**:
```javascript
// En la consola del navegador:
console.log(window.API_BASE_URL)
```

Si es `undefined`:
1. Verifica que el AppHost esté pasando la variable
2. Reinicia la aplicación Aspire
3. Verifica que dotnet-api esté en la lista de servicios

### Problema 2: Network tab muestra "Failed to fetch"

**Causa**: El dotnet-api no responde

**Soluciones**:
1. Verifica que dotnet-api esté corriendo (busca en el Dashboard de Aspire)
2. Verifica la URL en la consola del navegador
3. Intenta abrir manualmente: `http://localhost:XXXX/api/telemetry/traces` en el navegador
   - Debería dar error 400 (normal, no hay body), pero responder

### Problema 3: Status 200 OK pero no aparecen trazas

**Causa**: Las trazas llegan pero el Dashboard no las muestra

**Soluciones**:
1. **Espera 5-10 segundos** después de interactuar (hay un delay)
2. **Refresca el Dashboard** de Aspire
3. Verifica el **filtro de tiempo** (TimeRange) en el Dashboard
4. Busca específicamente el servicio **"web"**
5. Verifica los logs del dotnet-api para confirmar que sí se envían

### Problema 4: Logs del dotnet-api muestran "Error de red" o "503"

**Causa**: El Dashboard de Aspire no está escuchando en el puerto esperado

**Soluciones**:
1. Busca en los logs de inicio del Dashboard el puerto OTLP
2. Puede estar en un puerto diferente (18889, 4317, etc.)
3. Si encuentras el puerto correcto, actualiza `DotNetApi/Program.cs` línea ~147:
   ```csharp
   var otlpEndpoint = "http://localhost:TU_PUERTO_AQUI/v1/traces";
   ```

## 📊 Prueba Manual Rápida

En la **consola del navegador**, ejecuta:

```javascript
import('@opentelemetry/api').then(({ trace }) => {
  const tracer = trace.getTracer('test', '1.0');
  const span = tracer.startSpan('TEST-MANUAL');
  span.setAttribute('test', 'valor');
  span.end();
  console.log('✅ Traza enviada! Espera 3s y busca en Dashboard');
});
```

Espera 3-5 segundos y busca en el Dashboard: servicio **web**, traza **TEST-MANUAL**.

## ✅ Checklist

- [ ] Aplicación Aspire reiniciada
- [ ] Console muestra "OpenTelemetry inicializado!"
- [ ] Console muestra URL del proxy (no undefined)
- [ ] Network tab muestra POST /api/telemetry/traces
- [ ] Network tab muestra status 200 OK (verde)
- [ ] Logs del dotnet-api muestran "[TELEMETRY PROXY] ✅"
- [ ] Dashboard muestra servicio "web"
- [ ] Dashboard muestra trazas

## 🆘 Si Nada Funciona

Comparte esta información:

1. **Logs completos de la consola del navegador** (desde "Inicializando OpenTelemetry")
2. **Screenshot del Network tab** (filtrado por "telemetry")
3. **Logs del dotnet-api** (busca líneas con [TELEMETRY PROXY])
4. **Puerto del Dashboard** (ej: http://localhost:15888)
5. **Versión de Aspire** que estás usando

---

💡 **NOTA**: Esta solución DEBE funcionar porque:
- ✅ El navegador puede hablar con su propio backend (sin CORS)
- ✅ El backend puede hablar con el Dashboard sin problemas
- ✅ Es el patrón estándar para telemetría web
