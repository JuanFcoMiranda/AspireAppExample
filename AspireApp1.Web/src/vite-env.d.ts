/// <reference types="vite/client" />

// Extensión de Window para variables de entorno de Aspire
interface Window {
    OTEL_EXPORTER_OTLP_ENDPOINT?: string;
    OTEL_SERVICE_NAME?: string;
    OTEL_SERVICE_VERSION?: string;
}

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string
  readonly VITE_OTEL_EXPORTER_OTLP_ENDPOINT?: string
  readonly VITE_OTEL_SERVICE_NAME?: string
  readonly VITE_OTEL_SERVICE_VERSION?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
