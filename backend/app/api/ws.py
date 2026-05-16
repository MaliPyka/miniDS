from fastapi import APIRouter, WebSocket, WebSocketDisconnect
from app.core.database import async_session
from app.services import message_service, user_service, memberships_service
from app.services.security import decode_access_token


router = APIRouter(prefix="/ws")

class ConnectManager:
    def __init__(self):
        self.active_connections: dict[int, list[WebSocket]] = {}

    async def connect(self, websocket: WebSocket, channel_id: int):
        if channel_id not in self.active_connections:
            self.active_connections[channel_id] = []
        self.active_connections[channel_id].append(websocket)

    def disconnect(self, websocket: WebSocket, channel_id: int):
        if channel_id in self.active_connections:
            if websocket in self.active_connections[channel_id]:
                self.active_connections[channel_id].remove(websocket)

    async def broadcast(self, message: str, channel_id: int):
        if channel_id in self.active_connections:
            for connection in self.active_connections[channel_id]:
                await connection.send_text(message)


manager = ConnectManager()


@router.websocket("/{token}/{channel_id}")
async def websocket_endpoint(websocket: WebSocket, token: str, channel_id: int):
    user_data = decode_access_token(token)
    username = user_data["sub"]

    await websocket.accept()
    
    async with async_session() as session:
        user = await user_service.get_user_by_username(session, username)
        is_member = await memberships_service.is_member(session, user.id, channel_id)

    if not is_member:
        await websocket.close(code=4003)
        return
    
    await manager.connect(websocket, channel_id)
    
    async with async_session() as session:
        history = await message_service.get_messages(session, channel_id)
        for message, username in history:
            await websocket.send_text(f"{username}: {message.content}")
        members = await memberships_service.get_channel_members(session, channel_id)
        await websocket.send_text(f"MEMBERS:{','.join(members)}")
    try:
        while True:
            data = await websocket.receive_text()
            async with async_session() as session:
                await message_service.create_message(session, channel_id, user.id, data)
            await manager.broadcast(f"{username}: {data}", channel_id)
    except WebSocketDisconnect:
        manager.disconnect(websocket, channel_id)



