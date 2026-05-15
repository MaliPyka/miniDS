from app.core.database import Base
from sqlalchemy.orm import Mapped, mapped_column
from sqlalchemy import String, func, DateTime, ForeignKey
from datetime import datetime


class Message(Base):

    __tablename__ = 'messages'

    id: Mapped[int] = mapped_column(primary_key=True, autoincrement=True)
    channel_id: Mapped[int] = mapped_column(ForeignKey("channels.id", ondelete='CASCADE'))
    author_id: Mapped[int] = mapped_column(ForeignKey("users.id", ondelete='CASCADE'))
    content: Mapped[str] = mapped_column(String, nullable=False)
    created_at: Mapped[datetime] = mapped_column(DateTime, server_default=func.now())