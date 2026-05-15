from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.database import get_session
from app.schemas.servers import ChannelCreate, ServerCreate
from app.services.server_service import create_channel, create_server, get_channels, delete_channel, get_channel

router = APIRouter(prefix="/rooms", tags=["rooms"])


@router.post("/create_server")
async def create_server_cmd(data: ServerCreate, session: AsyncSession = Depends(get_session)):
    server = await create_server(session, data.name, data.owner_id)
    return server



@router.post("/create_channel")
async def create_channel_cmd(data: ChannelCreate, session: AsyncSession = Depends(get_session)):
    channel = await create_channel(session, data.server_id, data.name)
    return channel



@router.get("/channels/{server_id}")
async def get_channels_cmd(server_id: int, session: AsyncSession = Depends(get_session)):
    channels = await get_channels(session, server_id)
    return channels


@router.delete("/delete_channels/{channel_id}")
async def delete_channels_cmd(channel_id: int, session: AsyncSession = Depends(get_session)):
    if not await get_channel(session, channel_id):
        raise HTTPException(
            status_code=400,
            detail="Канал не найден"
        )

    try:
        await delete_channel(session, channel_id)
        return {"message": "deleted"}
    except ValueError as val_err:
        raise HTTPException(
            status_code=400,
            detail=f"Не удалось удалить: {str(val_err)}"
        )
