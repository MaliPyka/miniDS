from sqlalchemy import select
from app.models.message import Message
from app.models.server import Server, Channel
from app.models.user import User



async def create_user(session, username, hashed_password):
    user = User(username=username, hashed_password=hashed_password)
    session.add(user)
    await session.commit()
    await session.refresh(user)
    return user


async def get_user_by_username(session, username):
    result = await session.execute(select(User).where(User.username == username))
    user = result.scalar_one_or_none()
    return user



