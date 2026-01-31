# FastApiService

Servicio FastAPI con OpenTelemetry para .NET Aspire.

## Problemas Resueltos

El proyecto tenía los siguientes problemas:

1. **Puerto ocupado**: El puerto 8000 estaba siendo utilizado por otro proceso
2. **Configuración OTLP incompleta**: El exportador OTLP no tenía configurado el endpoint
3. **Falta de punto de entrada**: No había forma directa de ejecutar el servidor

## Cómo Ejecutar

### Opción 1: Ejecutar directamente con Python
```bash
python main.py
```

### Opción 2: Ejecutar con uvicorn
```bash
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

### Opción 3: Ejecutar con uv (recomendado para desarrollo)
```bash
uv run uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

## Variables de Entorno

- `SERVICE_NAME`: Nombre del servicio para OpenTelemetry (default: "fastapi")
- `PORT`: Puerto en el que escucha el servidor (default: 8000)
- `OTEL_EXPORTER_OTLP_ENDPOINT`: Endpoint OTLP para telemetría (default: "http://localhost:4318")

## Endpoints

- `GET /health` - Health check endpoint
- `GET /hello` - Endpoint de prueba
- `GET /docs` - Documentación interactiva de la API (Swagger UI)
- `GET /redoc` - Documentación alternativa (ReDoc)

## Verificar que funciona

```powershell
Invoke-WebRequest -Uri http://localhost:8000/health -UseBasicParsing
```

## Solución de Problemas

### Error: Puerto 8000 ya en uso

Si recibes el error `[Errno 10048] error while attempting to bind on address`, significa que otro proceso está usando el puerto 8000.

**Solución:**
```powershell
# Ver qué proceso usa el puerto 8000
netstat -ano | findstr :8000

# Detener el proceso (reemplaza PID con el número que viste)
Stop-Process -Id PID -Force
```

O simplemente usa otro puerto:
```bash
PORT=8001 python main.py
```
