import logging
import os

import uvicorn
from fastapi import FastAPI

from telemetry import configure_telemetry

# Obtener el nombre del servicio desde variables de entorno
service_name = os.getenv("OTEL_SERVICE_NAME") or os.getenv("SERVICE_NAME", "fastapi")

# Crear la aplicación FastAPI
app = FastAPI(title=service_name)

# Configurar telemetría (OpenTelemetry + Aspire)
# Esto configura TracerProvider, MeterProvider, LoggerProvider e instrumenta FastAPI
tracer = configure_telemetry(app, service_name=service_name)

# Logger estándar de Python (se envía automáticamente a OpenTelemetry)
logger = logging.getLogger(__name__)

@app.get("/hello-world")
async def hello(msg: str = None):
    logger.info("hello-world endpoint called")
    print(msg)
    return {"message": "Hola desde FastAPI"}

@app.get("/health")
async def health():
    logger.info("Health check called")
    return {"status": "ok"}

# Función de inicio que lee PORT del entorno
def start():
    """Inicia uvicorn con configuración desde variables de entorno."""
    port = int(os.getenv("PORT", "8000"))
    host = os.getenv("HOST", "0.0.0.0")
    uvicorn.run(app, host=host, port=port)

if __name__ == "__main__":
    start()