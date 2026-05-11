from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession, async_sessionmaker
from sqlalchemy.orm import DeclarativeBase
from app.core.config import db_url
from typing import AsyncGenerator

DB_PATH = db_url

engine = create_async_engine(
    DB_PATH,
    connect_args={"server_settings": {"application_name": "minicord"}},
    pool_pre_ping=True
)

async def get_session() -> AsyncGenerator:
    async with async_session() as session:
        yield session

async_session = async_sessionmaker(bind=engine,class_=AsyncSession,expire_on_commit=False)

class Base(DeclarativeBase):
    pass
