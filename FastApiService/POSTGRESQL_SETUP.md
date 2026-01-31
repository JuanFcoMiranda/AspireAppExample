# Configuración de PostgreSQL para FastAPI

Para conectar FastAPI con PostgreSQL a través de Aspire, necesitas configurar lo siguiente:

## 1. Instalar las dependencias

```bash
pip install psycopg2-binary sqlalchemy
```

## 2. Configurar la conexión

Aspire proporciona la cadena de conexión a través de la variable de entorno `ConnectionStrings__aspiredb`:

```python
import os
from sqlalchemy import create_engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker

# Obtener la cadena de conexión de Aspire
DATABASE_URL = os.getenv("ConnectionStrings__aspiredb")

# Crear el engine de SQLAlchemy
engine = create_engine(DATABASE_URL)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()

# Dependency para obtener la sesión de DB
def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()
```

## 3. Ejemplo de modelo

```python
from sqlalchemy import Column, Integer, String, Boolean, DateTime
from datetime import datetime

class TodoItem(Base):
    __tablename__ = "TodoItems"
    
    Id = Column(Integer, primary_key=True, index=True)
    Title = Column(String(200), nullable=False)
    IsCompleted = Column(Boolean, default=False, nullable=False)
    CreatedAt = Column(DateTime, default=datetime.utcnow, nullable=False)
```

## 4. Ejemplo de endpoint

```python
from fastapi import Depends
from sqlalchemy.orm import Session

@app.get("/todos")
async def get_todos(db: Session = Depends(get_db)):
    todos = db.query(TodoItem).all()
    return todos
```

## Notas

- El volumen `aspire-postgres-data` asegura que los datos persistan entre reinicios
- La base de datos se llama `aspiredb`
- Aspire maneja automáticamente la cadena de conexión completa
