from passlib.hash import pbkdf2_sha256
from jose import jwt
from datetime import datetime, timedelta
from app.core.config import secret_key

def set_hashed_password(password: str):
    hashed_password = pbkdf2_sha256.hash(password)
    return hashed_password

def verify_hashed_password(password: str, hashed_password: str) -> bool:
    return pbkdf2_sha256.verify(password, hashed_password)


ALGORITHM = "HS256"

def create_access_token(data: dict) -> str:
    to_encode = data.copy()
    expire = datetime.utcnow() + timedelta(hours=24)
    to_encode.update({"exp": expire})
    return jwt.encode(to_encode, secret_key, algorithm=ALGORITHM)


def decode_access_token(token: str) -> dict:
    return jwt.decode(token, secret_key, algorithms=[ALGORITHM])

