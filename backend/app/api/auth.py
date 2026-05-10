from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.ext.asyncio import AsyncSession
from app.core.database import get_session
from app.schemas.user import UserData
from app.services import security, user_service

router = APIRouter(prefix="/auth", tags=["auth"])

@router.post("/register")
async def register(data: UserData, session: AsyncSession = Depends(get_session)):
    existing_user = await user_service.get_user_by_username(session, data.username)
    if existing_user:
        raise HTTPException(status_code=400, detail="Username already taken")
    
    hashed_password = security.set_hashed_password(data.password)
    await user_service.create_user(session=session, username=data.username, hashed_password=hashed_password)
    return {"message": "done"}



@router.post("/login")
async def login(data: UserData, session: AsyncSession = Depends(get_session)):
    user = await user_service.get_user_by_username(session, data.username)
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    if not security.verify_hashed_password(data.password, user.hashed_password):
        raise HTTPException(status_code=401, detail="Invalid password")

    
    