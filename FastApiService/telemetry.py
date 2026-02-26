import logging
import os
import ssl
import urllib3

from opentelemetry import metrics, trace
from opentelemetry._logs import set_logger_provider
from opentelemetry.exporter.otlp.proto.http._log_exporter import OTLPLogExporter
from opentelemetry.exporter.otlp.proto.http.metric_exporter import OTLPMetricExporter
from opentelemetry.exporter.otlp.proto.http.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor
from opentelemetry.sdk.metrics import MeterProvider
from opentelemetry.sdk.metrics.export import PeriodicExportingMetricReader
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor

# Disable SSL warnings for development (Aspire uses self-signed certificates)
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

def configure_telemetry(app, service_name: str = "app"):
    """Configure OpenTelemetry for FastAPI application."""

    # Get OTLP endpoint from environment (configured by Aspire)
    otlp_endpoint = os.getenv("OTEL_EXPORTER_OTLP_ENDPOINT")

    if not otlp_endpoint:
        # Fallback to individual endpoint if available
        traces_endpoint = os.getenv("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "")
        otlp_endpoint = traces_endpoint.replace("/v1/traces", "") if traces_endpoint else None

    if not otlp_endpoint:
        # Last fallback for standalone testing
        otlp_endpoint = "http://localhost:4318"
        print(f"⚠️  OTLP endpoint not configured by Aspire, using fallback: {otlp_endpoint}")

    # Convert HTTPS to HTTP for localhost (avoid SSL issues)
    if otlp_endpoint.startswith("https://localhost") or otlp_endpoint.startswith("https://127.0.0.1"):
        otlp_endpoint = otlp_endpoint.replace("https://", "http://")

    # Get service name from environment
    service_name_final = os.getenv("OTEL_SERVICE_NAME", service_name)

    print(f"🔧 Configuring OpenTelemetry: service='{service_name_final}', endpoint={otlp_endpoint}")

    try:
        # Create resource
        resource = Resource.create({"service.name": service_name_final})

        # Configure Tracing
        trace_provider = TracerProvider(resource=resource)
        trace_exporter = OTLPSpanExporter(endpoint=f"{otlp_endpoint}/v1/traces", timeout=10)
        trace_provider.add_span_processor(BatchSpanProcessor(trace_exporter))
        trace.set_tracer_provider(trace_provider)

        # Configure Metrics
        metric_exporter = OTLPMetricExporter(endpoint=f"{otlp_endpoint}/v1/metrics", timeout=10)
        metric_reader = PeriodicExportingMetricReader(metric_exporter, export_interval_millis=5000)
        meter_provider = MeterProvider(resource=resource, metric_readers=[metric_reader])
        metrics.set_meter_provider(meter_provider)

        # Configure Logging
        logger_provider = LoggerProvider(resource=resource)
        log_exporter = OTLPLogExporter(endpoint=f"{otlp_endpoint}/v1/logs", timeout=10)
        logger_provider.add_log_record_processor(BatchLogRecordProcessor(log_exporter))
        set_logger_provider(logger_provider)

        # Add logging handler
        handler = LoggingHandler(level=logging.NOTSET, logger_provider=logger_provider)
        logging.getLogger().addHandler(handler)

        # Instrument FastAPI application
        FastAPIInstrumentor.instrument_app(app)

        print(f"✅ OpenTelemetry configured successfully")

    except Exception as e:
        print(f"❌ Error configuring OpenTelemetry: {type(e).__name__}: {e}")
        import traceback
        traceback.print_exc()

    return trace.get_tracer(__name__)
