from sqlalchemy import delete, select

from app.models.server import Server, Channel


async def create_server(session, name, owner_id):
    server = Server(name=name, owner_id = owner_id)
    session.add(server)
    await session.commit()
    await session.refresh(server)
    return server


async def create_channel(session, server_id, name, owner_id):
    channel = Channel(server_id = server_id, name = name, owner_id = owner_id)
    session.add(channel)
    await session.commit()
    await session.refresh(channel)
    return channel


async def get_channels(session, server_id):
    channels = await session.execute(select(Channel).where(Channel.server_id == server_id))
    return channels.scalars().all()


async def delete_channel(session, channel_id):
    stmt = delete(Channel).where(Channel.id == channel_id)
    await session.execute(stmt)
    await session.commit()


async def get_channel(session, channel_id):
    channel = await session.execute(select(Channel).where(Channel.id == channel_id))
    result = channel.scalar_one_or_none()
    return result