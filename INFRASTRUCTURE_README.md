# Aspire App - Configuración de Infraestructura

## Recursos configurados

### 1. Valkey (Redis-compatible) - Caché
- **Nombre del recurso**: `valkey`
- **Conectado a**: DotNetApi, FastAPI
- **Uso**: Caché distribuido para respuestas HTTP

#### Configuración en .NET
```csharp
builder.AddRedisClient("valkey");
```

#### Configuración en FastAPI
```python
import redis
import os

redis_connection = os.getenv("ConnectionStrings__valkey", "localhost:6379")
redis_client = redis.from_url(redis_connection)
```

### 2. PostgreSQL - Base de datos con volumen persistente
- **Nombre del recurso**: `postgres`
- **Base de datos**: `aspiredb`
- **Volumen**: `aspire-postgres-data` (datos persistentes)
- **Conectado a**: DotNetApi, FastAPI

#### Configuración en .NET
```csharp
builder.AddNpgsqlDbContext<AppDbContext>("aspiredb");
```

#### Configuración en FastAPI
```python
import os
from sqlalchemy import create_engine

DATABASE_URL = os.getenv("ConnectionStrings__aspiredb")
engine = create_engine(DATABASE_URL)
```

## Endpoints disponibles en DotNetApi

### Caché y FastAPI
- `GET /call-fastapi` - Llama a FastAPI con caché (60 segundos TTL)
- `DELETE /cache/clear` - Limpia el caché

### TodoItems (CRUD con PostgreSQL)
- `GET /todos` - Obtiene todos los items
- `GET /todos/{id}` - Obtiene un item por ID
- `POST /todos` - Crea un nuevo item
- `PUT /todos/{id}` - Actualiza un item existente
- `DELETE /todos/{id}` - Elimina un item

### Otros
- `GET /hello` - Endpoint de prueba local

## Variables de entorno proporcionadas por Aspire

### Para FastAPI
- `ConnectionStrings__valkey` - Cadena de conexión a Valkey/Redis
- `ConnectionStrings__aspiredb` - Cadena de conexión a PostgreSQL
- `PORT` - Puerto donde debe escuchar FastAPI
- `SERVICE_NAME` - Nombre del servicio para telemetría
- `OTEL_EXPORTER_OTLP_ENDPOINT` - Endpoint para exportar telemetría

### Para DotNetApi
- Las referencias se inyectan automáticamente a través de Service Defaults

## Persistencia de datos

- **PostgreSQL**: Los datos se guardan en el volumen Docker `aspire-postgres-data`
- **Valkey**: Los datos en caché son volátiles (se pierden al reiniciar)

## Ejecución

1. Asegúrate de tener Docker ejecutándose
2. Ejecuta el proyecto AppHost desde Visual Studio
3. Aspire automáticamente:
   - Levantará contenedores de Valkey y PostgreSQL
   - Creará el volumen persistente para PostgreSQL
   - Inyectará las variables de entorno necesarias

## Telemetría

Todos los servicios exportan telemetría (trazas, métricas, logs) al dashboard de Aspire en `http://localhost:18889`
