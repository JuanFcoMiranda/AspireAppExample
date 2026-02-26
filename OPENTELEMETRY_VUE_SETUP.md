# ✅ OpenTelemetry en Vue - Configuración Completa

## 🎯 ¿Qué se Implementó?

Se ha configurado **OpenTelemetry** en la aplicación Vue para enviar trazas al **Dashboard de Aspire** a través de un proxy en `dotnet-api`.

### Arquitectura

```
Vue (navegador) → dotnet-api/api/telemetry/traces → Aspire Dashboard
              (proxy CORS OK)                     (sin CORS)
```

## ✅ Estado Actual

1. ✅ **Paquetes instalados**: OpenTelemetry JS
2. ✅ **telemetry.ts configurado**: Inicialización automática
3. ✅ **Proxy en dotnet-api**: Endpoint `/api/telemetry/traces`
4. ✅ **Helpers creados**: `trackButtonClick`, `trackApiCall`
5. ✅ **App.vue actualizado**: Ejemplo de uso

## 🚀 Cómo Usar

### 1. Reinicia la Aplicación

```bash
# Detén Aspire (Ctrl+C)
# Vuelve a ejecutar desde Visual Studio
```

### 2. Abre la Aplicación Web

Abre el navegador en la URL de la aplicación Vue (ej: `http://localhost:5173`)

### 3. Abre la Consola del Navegador

Presiona `F12` y ve a la pestaña **Console**. Deberías ver:

```
🔧 Inicializando OpenTelemetry
   → Proxy: http://localhost:XXXX/api/telemetry/traces
   → Service: web
✅ OpenTelemetry configurado correctamente
   → Instrumentación de clicks activa
   → Las trazas se enviarán al Dashboard de Aspire
✅ Traza inicial enviada
```

### 4. Interactúa con los Botones

Haz click en los botones:
- **"Load /hello"**
- **"Load /call-fastapi"**

En la consola verás:

```
📊 Traza enviada: button-click: load-hello
📊 Traza enviada: api-call: GET /hello (123ms)
```

### 5. Verifica en el Dashboard de Aspire

1. **Abre el Dashboard de Aspire** (ej: `http://localhost:15888`)
2. **Ve a la sección "Traces"**
3. **Busca el servicio "web"**
4. **Deberías ver:**
   - `app-loaded` (traza inicial)
   - `button-click: load-hello`
   - `button-click: load-fastapi`
   - `api-call: GET /hello`
   - `api-call: GET /call-fastapi`
   - `click` (clicks automáticos de UserInteractionInstrumentation)

### 6. Verifica los Logs del dotnet-api

En los logs de `dotnet-api` verás:

```
[TELEMETRY PROXY] → Enviando 1234 bytes a http://localhost:4318/v1/traces
[TELEMETRY PROXY] ← Status: 200 OK
```

## 📊 Tipos de Trazas que Verás

### 1. Traza Inicial
```
app-loaded
  - url: http://localhost:5173
```

### 2. Clicks de Botones (Manual)
```
button-click: load-hello
  - button.name: load-hello
  - button.action: load-api-data
  - api.endpoint: /hello
  - component: vue
  - interaction.type: click
```

### 3. Llamadas a API (Manual)
```
api-call: GET /hello
  - http.method: GET
  - http.url: /hello
  - http.status_code: 200
  - http.duration_ms: 123
  - component: vue
```

### 4. Clicks Automáticos (UserInteractionInstrumentation)
```
click button
  - target_element: BUTTON
  - target_xpath: /html/body/div/section/div/button[1]
  - event_type: click
```

## 🛠️ Uso Manual en Componentes Vue

### Trackear Click de Botón

```typescript
import { trackButtonClick } from '@/telemetry-helpers';

const handleClick = () => {
  trackButtonClick('mi-boton', {
    'button.action': 'submit',
    'form.id': 'contact-form'
  });
  
  // Tu lógica aquí
};
```

### Trackear Llamada a API

```typescript
import { trackApiCall } from '@/telemetry-helpers';

const loadData = async () => {
  try {
    const data = await trackApiCall('/api/data', 'GET', async () => {
      const res = await fetch('/api/data');
      return res.json();
    });
    
    // Usar data
  } catch (error) {
    // Manejar error (la traza ya se envió con el error)
  }
};
```

### Trackear Evento Personalizado

```typescript
import { trace } from '@opentelemetry/api';

const tracer = trace.getTracer('web', '1.0.0');
const span = tracer.startSpan('user-login');

span.setAttribute('user.email', 'user@example.com');
span.setAttribute('login.method', 'google');

// ... lógica de login ...

span.end();
```

## 🐛 Troubleshooting

### ❌ No veo trazas en el Dashboard

**Verifica en la consola del navegador:**
1. ¿Ves los mensajes de "✅ OpenTelemetry configurado"?
2. ¿Ves los mensajes "📊 Traza enviada"?

**Verifica la pestaña Network:**
1. Presiona F12 → Network
2. Filtra por "telemetry"
3. ¿Ves peticiones `POST /api/telemetry/traces`?
4. ¿Status 200 OK?

**Verifica los logs de dotnet-api:**
1. ¿Ves `[TELEMETRY PROXY] → Enviando...`?
2. ¿Status 200 OK o hay errores?

### ❌ Error 503 en /api/telemetry/traces

**Causa**: El proxy no puede conectar con el Dashboard de Aspire.

**Solución**:
1. Verifica que el Dashboard esté corriendo
2. Verifica los logs de dotnet-api para ver el endpoint que usa
3. El endpoint debería ser `http://localhost:4318/v1/traces`

### ❌ Trazas llegan pero no aparecen en Dashboard

**Posibles causas:**
1. Espera 5-10 segundos (hay delay)
2. Refresca el Dashboard (F5)
3. Verifica el filtro de tiempo en el Dashboard
4. Busca específicamente el servicio "web"

### ❌ Error de CORS

Si ves errores de CORS en la consola:

**Solución**: Verifica que `dotnet-api` tenga CORS habilitado:

```csharp
// En Program.cs ya está configurado
app.UseCors("DevLocalhost");
```

## 📚 Documentación de OpenTelemetry

- [OpenTelemetry JS](https://opentelemetry.io/docs/languages/js/)
- [Web SDK](https://github.com/open-telemetry/opentelemetry-js/tree/main/packages/opentelemetry-sdk-trace-web)
- [OTLP Exporter](https://github.com/open-telemetry/opentelemetry-js/tree/main/experimental/packages/exporter-trace-otlp-http)

## ✅ Checklist de Verificación

- [ ] Aspire corriendo
- [ ] Navegador abierto en la app Vue
- [ ] Console muestra "✅ OpenTelemetry configurado correctamente"
- [ ] Hice click en los botones
- [ ] Console muestra "📊 Traza enviada"
- [ ] Network tab muestra `POST /api/telemetry/traces` → 200 OK
- [ ] Logs de dotnet-api muestran `[TELEMETRY PROXY] ← Status: 200 OK`
- [ ] Dashboard de Aspire → Traces → Servicio "web" → Veo trazas

## 🎉 ¡Listo!

Ahora tienes telemetría completa en tu aplicación Vue enviando trazas al Dashboard de Aspire. Cada click y cada llamada a API se trackea automáticamente.

---

💡 **TIP**: Puedes ver la correlación completa frontend-backend en el Dashboard de Aspire cuando haces llamadas que van de Vue → dotnet-api → fastapi.
