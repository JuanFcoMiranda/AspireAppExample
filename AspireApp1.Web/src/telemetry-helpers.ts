import { trace } from '@opentelemetry/api';

/**
 * Helper para trackear clicks de botones con OpenTelemetry
 */
export function trackButtonClick(buttonName: string, properties?: Record<string, any>) {
    const tracer = trace.getTracer('web', '1.0.0');
    const span = tracer.startSpan(`button-click: ${buttonName}`);

    span.setAttribute('button.name', buttonName);
    span.setAttribute('component', 'vue');
    span.setAttribute('interaction.type', 'click');

    if (properties) {
        Object.entries(properties).forEach(([key, value]) => {
            span.setAttribute(key, String(value));
        });
    }

    span.end();
    console.log(`📊 Traza enviada: button-click: ${buttonName}`);
}

/**
 * Helper para trackear llamadas a API con OpenTelemetry
 */
export async function trackApiCall<T>(
    path: string, 
    method: string,
    apiCall: () => Promise<T>
): Promise<T> {
    const tracer = trace.getTracer('web', '1.0.0');
    const span = tracer.startSpan(`api-call: ${method} ${path}`);

    span.setAttribute('http.method', method);
    span.setAttribute('http.url', path);
    span.setAttribute('component', 'vue');

    const startTime = performance.now();

    try {
        const result = await apiCall();
        const duration = performance.now() - startTime;

        span.setAttribute('http.status_code', 200);
        span.setAttribute('http.duration_ms', Math.round(duration));
        span.setStatus({ code: 1 }); // OK

        console.log(`📊 Traza enviada: api-call: ${method} ${path} (${Math.round(duration)}ms)`);

        return result;
    } catch (error) {
        const duration = performance.now() - startTime;

        span.setAttribute('http.status_code', 500);
        span.setAttribute('http.duration_ms', Math.round(duration));
        span.setAttribute('error', true);
        span.setAttribute('error.message', error instanceof Error ? error.message : String(error));
        span.setStatus({ code: 2, message: 'Error' }); // ERROR

        console.error(`❌ Error en API call: ${method} ${path}`, error);

        throw error;
    } finally {
        span.end();
    }
}
