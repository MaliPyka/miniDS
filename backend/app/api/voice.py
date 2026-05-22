from fastapi import APIRouter
from app.services.livekit_service import create_voice_token

router = APIRouter(prefix="/voice", tags=["voice"])

@router.get("/token/{channel_id}/{username}")
async def get_voice_token(channel_id: int, username: str):
    token = await create_voice_token(username, channel_id)
    return {"token": token, "url": "ws://localhost:7880"}