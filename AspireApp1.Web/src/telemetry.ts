import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { XMLHttpRequestInstrumentation } from '@opentelemetry/instrumentation-xml-http-request';

export function initTelemetry() {
  try {
    console.log('🔧 Inicializando OpenTelemetry...');

    // Obtener configuración del endpoint OTLP desde las variables de entorno
    const otlpEndpoint = import.meta.env.VITE_OTEL_EXPORTER_OTLP_ENDPOINT || 'http://localhost:18889/v1/traces';

    // Configurar el exportador OTLP
    const exporter = new OTLPTraceExporter({
      url: otlpEndpoint,
      headers: {
        'Content-Type': 'application/json',
      },
    });
    console.log('✓ OTLPTraceExporter creado');

    // Crear el procesador de spans
    const spanProcessor = new BatchSpanProcessor(exporter);
    console.log('✓ BatchSpanProcessor creado');

    // Configurar el proveedor de trazas con el span processor
    // Nueva forma recomendada: pasar spanProcessors en el constructor
    const provider = new WebTracerProvider({
      spanProcessors: [spanProcessor],
    });
    console.log('✓ WebTracerProvider creado con spanProcessors');

    // Registrar el proveedor globalmente
    provider.register();
    console.log('✓ Provider registrado');

    // Registrar instrumentaciones automáticas
    registerInstrumentations({
      instrumentations: [
        new FetchInstrumentation({
          propagateTraceHeaderCorsUrls: [/.*/],
          clearTimingResources: true,
        }),
        new XMLHttpRequestInstrumentation({
          propagateTraceHeaderCorsUrls: [/.*/],
          clearTimingResources: true,
        }),
      ],
    });
    console.log('✓ Instrumentaciones registradas');

    console.log('✅ OpenTelemetry inicializado correctamente');
    console.log('📡 Endpoint OTLP:', otlpEndpoint);
  } catch (error) {
    console.error('❌ Error al inicializar OpenTelemetry:', error);
    console.error('Stack:', (error as Error).stack);
  }
}
