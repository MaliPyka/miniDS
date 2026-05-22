from contextlib import asynccontextmanager
from fastapi import FastAPI
from app.core.database import engine, Base
from app.api.auth import router as auth_router
from app.api.ws import router as ws_router
from app.models import User, Server, Channel, Message
from app.api.rooms import router as rooms_router
from app.api.voice import router as voice_router

@asynccontextmanager
async def lifespan(app: FastAPI):
    async with engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)
    yield

app = FastAPI(lifespan=lifespan)
app.include_router(auth_router)
app.include_router(ws_router)
app.include_router(rooms_router)
app.include_router(voice_router)