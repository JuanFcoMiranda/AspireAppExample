# Solución de Trazas OpenTelemetry desde el Proyecto Web - ACTUALIZADO

## ⚠️ Problema Identificado

Las trazas no llegaban al Dashboard de Aspire debido a:
1. **Puerto incorrecto** - Se usaba 18889 pero el correcto es 4318 (HTTP OTLP)
2. **Exportador incorrecto** - Se usaba Protobuf pero HTTP JSON es mejor para navegadores
3. **Posibles problemas de CORS** - Los navegadores tienen restricciones al enviar a endpoints OTLP

## ✅ Solución Implementada

### 1. **package.json** - Cambio de exportador
   - ❌ Antes: `@opentelemetry/exporter-trace-otlp-proto`
   - ✅ Ahora: `@opentelemetry/exporter-trace-otlp-http`
   - **Razón**: El exportador HTTP es más compatible con navegadores

### 2. **AppHost.cs** - Puerto correcto
   - ❌ Antes: `http://localhost:18889/v1/traces`
   - ✅ Ahora: `http://localhost:4318/v1/traces`
   - **Razón**: 4318 es el puerto estándar para OTLP HTTP

### 3. **telemetry.ts** - Configuración HTTP
   - Cambiado a usar `OTLPTraceExporter` con HTTP
   - Content-Type: `application/json` (antes: `application/x-protobuf`)
   - Agregado `ignoreUrls` para evitar instrumentar llamadas al OTLP endpoint
   - Mejor logging y manejo de errores
   - Traza de inicialización automática

### 4. **vite.config.ts** y **.env.local**
   - Actualizados para usar puerto 4318

### 5. **diagnostico.html**
   - Nueva herramienta de diagnóstico en `/diagnostico.html`
   - Permite probar conectividad y CORS
   - Pruebas de endpoints alternativos

## 🚀 Pasos para Aplicar la Solución

### 1. Instalar el nuevo paquete
```bash
cd AspireApp1.Web
pnpm remove @opentelemetry/exporter-trace-otlp-proto
pnpm add @opentelemetry/exporter-trace-otlp-http@^0.53.0
```

### 2. Reiniciar la aplicación
```bash
# Detén la aplicación Aspire si está corriendo
# Vuélvela a iniciar desde el AppHost
```

### 3. Verificar en el navegador
1. Abre DevTools (F12)
2. Ve a la pestaña **Console**
3. Deberías ver:
   ```
   ✅ OpenTelemetry inicializado correctamente!
   📡 Endpoint: http://localhost:4318/v1/traces
   ```
4. Ve a la pestaña **Network**
5. Filtra por "traces"
6. Espera 1-2 segundos
7. Deberías ver peticiones POST a `/v1/traces`
8. Status: **200 OK**

### 4. Verificar en el Dashboard
1. Abre el Dashboard de Aspire (http://localhost:15888 o el puerto asignado)
2. Ve a la sección **Traces**
3. Deberías ver:
   - Servicio: **web**
   - Traza inicial: **app-initialization**
   - Trazas de interacción cuando hagas clicks

## 🐛 Herramienta de Diagnóstico

Abre en tu navegador: **http://localhost:5173/diagnostico.html**

Esta página te permite:
- ✅ Verificar variables de entorno
- ✅ Probar conectividad al endpoint OTLP
- ✅ Verificar configuración de CORS
- ✅ Probar endpoints alternativos
- ✅ Enviar trazas de prueba
- ✅ Ver logs del sistema

## ❌ Problemas Comunes y Soluciones

### 1. Error: "net::ERR_CONNECTION_REFUSED"
**Problema**: El Dashboard de Aspire no está corriendo o el puerto está equivocado.

**Solución**:
```bash
# Verifica que el AppHost esté corriendo
# Verifica el puerto en los logs del Dashboard
# Debería decir algo como: "OTLP endpoint listening on http://localhost:4318"
```

### 2. Error de CORS
**Problema**: El navegador bloquea las peticiones por CORS.

**Solución**: Aspire debería permitir CORS automáticamente. Si no:
1. Verifica que estés usando el puerto 4318
2. Verifica los logs del Dashboard
3. Si persiste, abre un issue en el repositorio de Aspire

### 3. No aparecen trazas en el Dashboard
**Causas posibles**:
- Las trazas se envían pero hay un delay de 1-2 segundos
- El filtro del Dashboard está configurado incorrectamente
- El servicio no se identifica como "web"

**Solución**:
1. Espera 5 segundos después de interactuar con la página
2. En el Dashboard, busca por servicio "web"
3. Verifica en Network tab que las peticiones son 200 OK
4. Usa la herramienta de diagnóstico

### 4. TypeScript error en telemetry.ts
**Problema**: Error al importar el nuevo paquete.

**Solución**:
```bash
cd AspireApp1.Web
# Asegúrate de haber instalado el paquete correcto
pnpm install
# Reinicia el servidor de desarrollo
pnpm dev
```

## 📊 Diferencias Clave: Proto vs HTTP

| Característica | Proto (Antes) | HTTP (Ahora) |
|----------------|---------------|--------------|
| **Content-Type** | `application/x-protobuf` | `application/json` |
| **Compatibilidad** | Requiere configuración especial | Funciona out-of-the-box |
| **CORS** | Puede requerir preflight | Manejado automáticamente |
| **Debugging** | Binario, difícil de inspeccionar | JSON legible en Network tab |
| **Tamaño** | Más compacto | Ligeramente más grande |

Para navegadores web, **HTTP es la mejor opción**.

## 🎯 Prueba Manual Rápida

Ejecuta en la consola del navegador:

```javascript
// Importar API de OpenTelemetry
import('@opentelemetry/api').then(({ trace }) => {
  const tracer = trace.getTracer('test-manual');
  const span = tracer.startSpan('test-from-console');
  span.setAttribute('test', 'manual');
  span.setAttribute('timestamp', Date.now());
  span.end();
  console.log('✅ Traza enviada! Búscala en el Dashboard');
});
```

Luego busca en el Dashboard: servicio "web", traza "test-from-console".

## 📝 Checklist de Verificación

- [ ] Instalado `@opentelemetry/exporter-trace-otlp-http`
- [ ] Removido `@opentelemetry/exporter-trace-otlp-proto`
- [ ] Reiniciada la aplicación Aspire
- [ ] Console muestra "OpenTelemetry inicializado correctamente"
- [ ] Network tab muestra peticiones POST a `/v1/traces`
- [ ] Status de las peticiones es 200 OK
- [ ] Dashboard de Aspire muestra el servicio "web"
- [ ] Al hacer click en la página, aparecen nuevas trazas

## 🆘 Si Nada Funciona

1. **Verifica el puerto del Dashboard**:
   - Busca en los logs de inicio: "OTLP endpoint"
   - Puede que no sea 4318, ajusta según lo que veas

2. **Prueba modo desarrollo independiente**:
   ```bash
   cd AspireApp1.Web
   pnpm dev
   # Abre http://localhost:5173/diagnostico.html
   ```

3. **Verifica que Aspire acepte trazas**:
   - Verifica que los servicios backend (dotnet-api, fastapi) sí envíen trazas
   - Si ellos funcionan pero el web no, es un problema de CORS/navegador

4. **Revisa los logs del Dashboard de Aspire**:
   - Busca errores relacionados con OTLP
   - Verifica que el endpoint esté activo

---

💡 **TIP**: La herramienta `/diagnostico.html` es tu mejor amigo para troubleshooting.
