from sqlalchemy import select

from app.models.server import Server, Channel


async def create_server(session, name, owner_id):
    server = Server(name=name, owner_id = owner_id)
    session.add(server)
    await session.commit()
    await session.refresh(server)
    return server


async def create_channel(session, server_id, name):
    channel = Channel(server_id = server_id, name = name)
    session.add(channel)
    await session.commit()
    await session.refresh(channel)
    return channel


async def get_channels(session, server_id):
    channels = await session.execute(select(Channel).where(Channel.server_id == server_id))
    return channels.scalars().all()