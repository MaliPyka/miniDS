from app.models.message import Message


async def create_message(session, channel_id, user_id, data):
    message = Message(channel_id=channel_id, author_id = user_id, content = data)
    session.add(message)
    await session.commit()
    await session.refresh(message)
    return message
