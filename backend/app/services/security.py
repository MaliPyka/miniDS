from passlib.hash import pbkdf2_sha256

def set_hashed_password(password: str):
    hashed_password = pbkdf2_sha256.hash(password)
    return hashed_password

def verify_hashed_password(password: str, hashed_password: str) -> bool:
    return pbkdf2_sha256.verify(password, hashed_password)