from fastapi import APIRouter, WebSocket, WebSocketDisconnect
from app.core.database import async_session
from app.services import message_service, user_service
from app.services.security import decode_access_token


router = APIRouter(prefix="/ws")

class ConnectManager:
    def __init__(self):
        self.active_connections: list[WebSocket] = []

    async def connect(self, websocket: WebSocket):
        await websocket.accept()
        self.active_connections.append(websocket)

    def disconnect(self, websocket: WebSocket):
        self.active_connections.remove(websocket)

    async def broadcast(self, message: str):
        for Connection in self.active_connections:
            await Connection.send_text(message)


manager = ConnectManager()


@router.websocket("/{token}/{channel_id}")
async def websocket_endpoint(websocket: WebSocket, token: str, channel_id: int):
    user_data = decode_access_token(token)
    username = user_data["sub"]
    
    async with async_session() as session:
        user = await user_service.get_user_by_username(session, username)
    
    await manager.connect(websocket)
    async with async_session() as session:
        history = await message_service.get_messages(session, channel_id)
        for message, username in history:
            await websocket.send_text(f"{username}: {message.content}")
    try:
        while True:
            data = await websocket.receive_text()
            async with async_session() as session:
                await message_service.create_message(session, channel_id, user.id, data)
            await manager.broadcast(f"{username}: {data}")
    except WebSocketDisconnect:
        manager.disconnect(websocket)


@router.get("/test")
def test():
    return {"ok": True}

