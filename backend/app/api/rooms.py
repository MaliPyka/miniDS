from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.database import get_session
from app.schemas.servers import ChannelCreate, ServerCreate
from app.services.server_service import create_channel, create_server

router = APIRouter(prefix="/rooms", tags=["rooms"])


@router.post("/create_server")
async def create_server_cmd(data: ServerCreate, session: AsyncSession = Depends(get_session)):
    server = await create_server(session, data.name, data.owner_id)
    return server



@router.post("/create_channel")
async def create_channel_cmd(data: ChannelCreate, session: AsyncSession = Depends(get_session)):
    channel = await create_channel(session, data.server_id, data.name)
    return channel
