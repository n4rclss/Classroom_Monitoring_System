from pydantic import BaseModel, ConfigDict, Field
from typing import Literal

class PacketBase(BaseModel):
    model_config = ConfigDict(extra='forbid', strict=True)
    
class PacketRefresh(PacketBase):
    type: Literal["refresh"] = "refresh"
    room_id: str = Field(min_length=1)

class PacketSendToAll(PacketBase):
    type: Literal["send_message_to_all"] = "send_message_to_all"
    room_id: str = Field(min_length=1)
    message_to_all: str = Field(min_length=1)

class PacketLogin(PacketBase):
    type: Literal["login"] = "login"
    username: str = Field(min_length=1)
    password: str = Field(min_length=1)
    role: str = Field(min_length=1, pattern=r"^(teacher|student)$")

class PacketJoinRoom(PacketBase):
    type: Literal["join_room"] = "join_room"
    room_id: str = Field(min_length=1)
    username: str = Field(min_length=1)
    mssv: str = Field(min_length=1)
    student_name: str = Field(min_length=1)
    
class PacketCreateRoom(PacketBase):
    type: Literal["create_room"] = "create_room"
    room_id: str = Field(min_length=1)
    teacher: str = Field(min_length=1)
