// Script de diagnóstico de telemetría
// Ejecutar en la consola del navegador después de cargar la aplicación

console.log('🔍 === DIAGNÓSTICO DE TELEMETRÍA ASPIRE ===\n');

// 1. Verificar variables de entorno
console.log('1️⃣ VARIABLES DE ENTORNO:');
console.log('   window.OTEL_EXPORTER_OTLP_ENDPOINT:', window.OTEL_EXPORTER_OTLP_ENDPOINT);
console.log('   window.OTEL_SERVICE_NAME:', window.OTEL_SERVICE_NAME);
console.log('   window.OTEL_SERVICE_VERSION:', window.OTEL_SERVICE_VERSION);

// 2. Verificar OpenTelemetry
console.log('\n2️⃣ OPENTELEMETRY:');
const hasOtel = typeof window !== 'undefined' && window.OTEL_EXPORTER_OTLP_ENDPOINT !== undefined;
console.log('   Estado:', hasOtel ? '✅ Configurado' : '❌ No configurado');

// 3. Test de conectividad al endpoint OTLP
console.log('\n3️⃣ TEST DE CONECTIVIDAD AL ENDPOINT OTLP:');
const endpoint = window.OTEL_EXPORTER_OTLP_ENDPOINT || 'http://localhost:4318/v1/traces';
console.log('   Endpoint:', endpoint);
console.log('   Probando conexión...');

fetch(endpoint, {
    method: 'OPTIONS',
    mode: 'cors'
})
.then(response => {
    console.log('   ✅ Endpoint accesible');
    console.log('   Status:', response.status);
    console.log('   Headers CORS:', response.headers.get('Access-Control-Allow-Origin'));
})
.catch(error => {
    console.error('   ❌ Error de conexión:', error.message);
    console.log('   Posibles causas:');
    console.log('   - Dashboard de Aspire no está ejecutándose');
    console.log('   - Puerto incorrecto (verifica que sea 4318 para HTTP o 4317 para gRPC)');
    console.log('   - El endpoint no incluye /v1/traces');
});

// 4. Verificar que la app puede enviar peticiones
console.log('\n4️⃣ TEST DE ENVÍO DE TRAZA:');
console.log('   Simulando envío de traza...');
console.log('   (Haz click en un botón de la aplicación y observa el Network tab)');

// 5. Verificar Network tab
console.log('\n5️⃣ VERIFICACIÓN EN NETWORK TAB:');
console.log('   Abre DevTools → Network');
console.log('   Filtra por: "traces" o "v1/traces"');
console.log('   Haz click en un botón de la app');
console.log('   Deberías ver:');
console.log('   - POST a ' + endpoint);
console.log('   - Status: 200 OK o 202 Accepted');

// 6. Información para debugging
console.log('\n6️⃣ INFORMACIÓN PARA DEBUGGING:');
console.log('   User Agent:', navigator.userAgent);
console.log('   URL actual:', window.location.href);

// 7. Instrucciones
console.log('\n7️⃣ PRÓXIMOS PASOS:');
console.log('   1. Verifica que el Dashboard de Aspire esté ejecutándose');
console.log('   2. Verifica que el puerto sea el correcto (4318 para HTTP/Protobuf)');
console.log('   3. Haz click en un botón de la app');
console.log('   4. Ve a Network tab y busca peticiones a /v1/traces');
console.log('   5. Si ves status 200/202 pero no aparecen en Dashboard:');
console.log('      - Espera 10 segundos');
console.log('      - Recarga el Dashboard');
console.log('      - Ve a Traces → Filtra por "aspireapp1-web"');

console.log('\n📚 Más información: Ver archivo DIAGNOSTICO_TRAZAS.md');
console.log('='.repeat(60));

