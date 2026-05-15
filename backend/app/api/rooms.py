from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.database import get_session
from app.schemas.servers import ChannelCreate, ServerCreate, AddMember
from app.services.server_service import create_channel, create_server, get_channels, delete_channel, get_channel
from app.services.security import decode_access_token
from app.services.user_service import get_user_by_username
from app.services.memberships_service import add_member

router = APIRouter(prefix="/rooms", tags=["rooms"])


@router.post("/create_server")
async def create_server_cmd(data: ServerCreate, session: AsyncSession = Depends(get_session)):
    server = await create_server(session, data.name, data.owner_id)
    return server



@router.post("/create_channel")
async def create_channel_cmd(data: ChannelCreate, session: AsyncSession = Depends(get_session)):
    user_data = decode_access_token(data.token)
    user = await get_user_by_username(session, user_data["sub"])
    channel = await create_channel(session, data.server_id, data.name, user.id)
    await add_member(session, user.id, channel.id)
    return channel



@router.get("/channels/{server_id}/{token}")
async def get_channels_cmd(server_id: int, token: str, session: AsyncSession = Depends(get_session)):
    user_data = decode_access_token(token)
    user = await get_user_by_username(session, user_data["sub"])
    channels = await get_channels(session, server_id, user.id)
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


@router.post("/add_member")
async def add_member_cmd(data: AddMember, session: AsyncSession = Depends(get_session)):
    user = await get_user_by_username(session, data.username)
    if not user:
        raise HTTPException(status_code=404, detail="Пользователь не найден")
    
    await add_member(session, user.id, data.channel_id)
    return {"message": f"User: {data.username} added!"}
