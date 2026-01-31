# Ejemplo completo de integración FastAPI con Valkey y PostgreSQL

## Instalación de dependencias

```bash
pip install redis psycopg2-binary sqlalchemy
```

## main.py - Ejemplo completo

```python
from fastapi import FastAPI, Depends
from sqlalchemy import create_engine, Column, Integer, String, Boolean, DateTime
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, Session
from datetime import datetime
import redis
import os
import json

app = FastAPI()

# ============================================
# Configuración de Redis/Valkey
# ============================================
redis_connection = os.getenv("ConnectionStrings__valkey", "localhost:6379")
redis_client = redis.from_url(redis_connection)

# ============================================
# Configuración de PostgreSQL
# ============================================
DATABASE_URL = os.getenv("ConnectionStrings__aspiredb", "postgresql://user:password@localhost/aspiredb")
engine = create_engine(DATABASE_URL)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()

# Modelo de base de datos
class TodoItem(Base):
    __tablename__ = "TodoItems"
    
    Id = Column(Integer, primary_key=True, index=True)
    Title = Column(String(200), nullable=False)
    IsCompleted = Column(Boolean, default=False, nullable=False)
    CreatedAt = Column(DateTime, default=datetime.utcnow, nullable=False)

# Crear tablas
Base.metadata.create_all(bind=engine)

# Dependency para DB
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

# ============================================
# Endpoints con caché
# ============================================

@app.get("/hello-world")
async def hello_world():
    """Endpoint simple con caché"""
    cache_key = "hello:world"
    
    # Intentar obtener del caché
    cached = redis_client.get(cache_key)
    if cached:
        return {
            "message": cached.decode(),
            "cached": True,
            "timestamp": datetime.utcnow().isoformat()
        }
    
    # Si no está en caché, generar respuesta
    response = "¡Hola desde FastAPI!"
    redis_client.setex(cache_key, 60, response)  # 60 segundos TTL
    
    return {
        "message": response,
        "cached": False,
        "timestamp": datetime.utcnow().isoformat()
    }

# ============================================
# Endpoints CRUD con PostgreSQL
# ============================================

@app.get("/todos")
async def get_todos(db: Session = Depends(get_db)):
    """Obtiene todos los items con caché"""
    cache_key = "todos:all"
    
    # Intentar obtener del caché
    cached = redis_client.get(cache_key)
    if cached:
        return {
            "data": json.loads(cached),
            "cached": True
        }
    
    # Si no está en caché, obtener de la base de datos
    todos = db.query(TodoItem).all()
    todos_dict = [
        {
            "id": todo.Id,
            "title": todo.Title,
            "isCompleted": todo.IsCompleted,
            "createdAt": todo.CreatedAt.isoformat()
        }
        for todo in todos
    ]
    
    # Guardar en caché por 30 segundos
    redis_client.setex(cache_key, 30, json.dumps(todos_dict))
    
    return {
        "data": todos_dict,
        "cached": False
    }

@app.post("/todos")
async def create_todo(title: str, db: Session = Depends(get_db)):
    """Crea un nuevo item e invalida el caché"""
    todo = TodoItem(Title=title, IsCompleted=False)
    db.add(todo)
    db.commit()
    db.refresh(todo)
    
    # Invalidar caché
    redis_client.delete("todos:all")
    
    return {
        "id": todo.Id,
        "title": todo.Title,
        "isCompleted": todo.IsCompleted,
        "createdAt": todo.CreatedAt.isoformat()
    }

@app.put("/todos/{todo_id}")
async def update_todo(todo_id: int, title: str = None, is_completed: bool = None, db: Session = Depends(get_db)):
    """Actualiza un item e invalida el caché"""
    todo = db.query(TodoItem).filter(TodoItem.Id == todo_id).first()
    if not todo:
        return {"error": "Todo not found"}, 404
    
    if title is not None:
        todo.Title = title
    if is_completed is not None:
        todo.IsCompleted = is_completed
    
    db.commit()
    db.refresh(todo)
    
    # Invalidar caché
    redis_client.delete("todos:all")
    redis_client.delete(f"todo:{todo_id}")
    
    return {
        "id": todo.Id,
        "title": todo.Title,
        "isCompleted": todo.IsCompleted,
        "createdAt": todo.CreatedAt.isoformat()
    }

@app.delete("/todos/{todo_id}")
async def delete_todo(todo_id: int, db: Session = Depends(get_db)):
    """Elimina un item e invalida el caché"""
    todo = db.query(TodoItem).filter(TodoItem.Id == todo_id).first()
    if not todo:
        return {"error": "Todo not found"}, 404
    
    db.delete(todo)
    db.commit()
    
    # Invalidar caché
    redis_client.delete("todos:all")
    redis_client.delete(f"todo:{todo_id}")
    
    return {"message": "Todo deleted successfully"}

# ============================================
# Endpoint para limpiar caché
# ============================================

@app.delete("/cache/clear")
async def clear_cache():
    """Limpia todo el caché"""
    redis_client.flushdb()
    return {"message": "Cache cleared successfully"}

# ============================================
# Función de inicio (para Aspire)
# ============================================

def start():
    import uvicorn
    port = int(os.getenv("PORT", 8000))
    uvicorn.run(app, host="0.0.0.0", port=port)

if __name__ == "__main__":
    start()
```

## Características implementadas

✅ **Caché con Valkey/Redis**
- Caché de respuestas HTTP (60 segundos TTL)
- Caché de consultas a base de datos (30 segundos TTL)
- Invalidación automática al modificar datos
- Endpoint para limpiar caché manualmente

✅ **Base de datos PostgreSQL**
- CRUD completo de TodoItems
- Persistencia con volumen Docker
- ORM con SQLAlchemy
- Creación automática de tablas

✅ **Integración con Aspire**
- Lee cadenas de conexión de variables de entorno
- Compatible con telemetría OpenTelemetry
- Configuración automática de puerto
