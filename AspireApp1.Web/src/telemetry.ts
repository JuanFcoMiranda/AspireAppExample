import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { Resource } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { UserInteractionInstrumentation } from '@opentelemetry/instrumentation-user-interaction';
import { trace } from '@opentelemetry/api';

/**
 * Inicializa OpenTelemetry para enviar trazas al Dashboard de Aspire
 * vía proxy en dotnet-api
 */
export function initTelemetry() {
    try {
        // Obtener la URL base del API desde window (configurada por Aspire)
        const apiBaseUrl = (window as any).API_BASE_URL || 'http://localhost:5000';

        // El proxy en dotnet-api que reenvía al Dashboard de Aspire
        const proxyEndpoint = `${apiBaseUrl}/api/telemetry/traces`;

        console.log('🔧 Inicializando OpenTelemetry');
        console.log(`   → Proxy: ${proxyEndpoint}`);
        console.log(`   → Service: web`);

        // Crear recurso con información del servicio
        const resource = new Resource({
            [ATTR_SERVICE_NAME]: 'web',
            [ATTR_SERVICE_VERSION]: '1.0.0',
        });

        // Configurar exportador OTLP HTTP
        const exporter = new OTLPTraceExporter({
            url: proxyEndpoint,
            headers: {
                'Content-Type': 'application/json',
            },
            timeoutMillis: 10000,
        });

        // Configurar procesador de spans
        const spanProcessor = new BatchSpanProcessor(exporter, {
            maxQueueSize: 2048,
            maxExportBatchSize: 512,
            scheduledDelayMillis: 2000, // Enviar cada 2 segundos
        });

        // Crear proveedor de trazas
        const provider = new WebTracerProvider({
            resource: resource,
            spanProcessors: [spanProcessor],
        });

        // Registrar el proveedor globalmente
        provider.register();

        // Registrar instrumentación de interacciones del usuario (clicks, etc.)
        registerInstrumentations({
            instrumentations: [
                new UserInteractionInstrumentation({
                    eventNames: ['click', 'submit'],
                    shouldPreventSpanCreation: (eventType, element, span) => {
                        // No crear spans para clicks en html/body
                        const tagName = element.tagName.toLowerCase();
                        if (tagName === 'html' || tagName === 'body') {
                            return true;
                        }
                        return false;
                    },
                }),
            ],
        });

        console.log('✅ OpenTelemetry configurado correctamente');
        console.log('   → Instrumentación de clicks activa');
        console.log('   → Las trazas se enviarán al Dashboard de Aspire');

        // Crear traza inicial
        const tracer = trace.getTracer('web', '1.0.0');
        const span = tracer.startSpan('app-loaded');
        span.setAttribute('url', window.location.href);
        span.end();

        console.log('✅ Traza inicial enviada');

        return provider;

    } catch (error) {
        console.error('❌ Error al inicializar OpenTelemetry:', error);
        return null;
    }
}

/**
 * Helper para crear trazas manuales desde componentes Vue
 */
export function trackButtonClick(buttonName: string, properties?: Record<string, any>) {
    const tracer = trace.getTracer('web', '1.0.0');
    const span = tracer.startSpan(`button-click: ${buttonName}`);

    span.setAttribute('button.name', buttonName);
    span.setAttribute('component', 'vue');

    if (properties) {
        Object.entries(properties).forEach(([key, value]) => {
            span.setAttribute(key, String(value));
        });
    }

    span.end();
    console.log(`📊 Traza enviada: button-click: ${buttonName}`);
}
