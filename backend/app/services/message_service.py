from sqlalchemy import select

from app.models.message import Message
from app.models.user import User


async def create_message(session, channel_id, user_id, data):
    message = Message(channel_id=channel_id, author_id = user_id, content = data)
    session.add(message)
    await session.commit()
    await session.refresh(message)
    return message


async def get_messages(session, channel_id, limit = 50):
    messages = await session.execute(select(Message, User.username).join(User, Message.author_id == User.id).where(Message.channel_id == channel_id).order_by(Message.created_at).limit(limit))
    return messages.all()