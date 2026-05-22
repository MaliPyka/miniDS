from livekit import api

LIVEKIT_URL = "ws://livekit:7880"
API_KEY = "devkey"
API_SECRET = "secret"

async def create_voice_token(username: str, channel_id: int) -> str:
    token = api.AccessToken(API_KEY, API_SECRET)
    token.with_identity(username)
    token.with_name(username)
    token.with_grants(api.VideoGrants(
        room_join=True,
        room=f"channel_{channel_id}"
    ))
    return token.to_jwt()