from pydantic import BaseModel

class ServerCreate(BaseModel):
    name: str
    owner_id: int

class ChannelCreate(BaseModel):
    name: str
    server_id: int
    token: str

class AddMember(BaseModel):
    username: str
    channel_id: int