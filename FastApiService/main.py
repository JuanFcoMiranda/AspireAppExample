import logging
import os

import uvicorn
from fastapi import FastAPI
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider

from telemetry import configure_telemetry

service_name = os.getenv("SERVICE_NAME", "fastapi")

resource = Resource.create({"service.name": service_name})
provider = TracerProvider(resource=resource)

app = FastAPI()
FastAPIInstrumentor.instrument_app(app, tracer_provider=provider)

# Configure telemetry for a standalone dashboard
tracer = configure_telemetry(app, service_name=service_name)
logger = logging.getLogger(__name__)

@app.get("/hello-world")
async def hello():
    logger.info("hello-world endpoint called")
    return {"message": "Hola desde FastAPI"}

@app.get("/health")
async def health():
    logger.info("Health check called")
    return {"status": "ok"}

@app.get("/simulate-error")
async def simulate_error():
    logger.warning("This is a simulated warning.")
    logger.error("This is a simulated error.")
    return {"message": "Simulated warning and error logs generated"}

# Función de inicio que lee PORT del entorno
def start():
    """Inicia uvicorn con configuración desde variables de entorno."""
    port = int(os.getenv("PORT", "8000"))
    host = os.getenv("HOST", "0.0.0.0")
    uvicorn.run(app, host=host, port=port)

if __name__ == "__main__":
    start()