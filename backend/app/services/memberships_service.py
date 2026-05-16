from sqlalchemy import select

from app.models.server import Membership
from app.models.user import User


async def add_member(session, user_id, channel_id):
    member = Membership(user_id=user_id, channel_id=channel_id)
    session.add(member)
    await session.commit()
    await session.refresh(member)
    return member


async def is_member(session, user_id, channel_id) -> bool:
    result = await session.execute(select(Membership).where(Membership.user_id == user_id, Membership.channel_id == channel_id))
    member = result.scalar_one_or_none()
    return member is not None


async def get_channel_members(session, channel_id):
    result = await session.execute(select(User.username).join(Membership, Membership.user_id == User.id).where(Membership.channel_id == channel_id))
    return result.scalars().all()