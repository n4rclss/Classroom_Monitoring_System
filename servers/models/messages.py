from pydantic import BaseModel, ConfigDict, Field
from typing import Literal

class BaseMessage(BaseModel):
    model_config = ConfigDict(extra='forbid', strict=True)
    
class RefreshMessage(BaseMessage):
    type: Literal["refresh"] = "refresh"
    room_id: str = Field(min_length=1)

class SendMessageToAll(BaseMessage):
    type: Literal["send_message_to_all"] = "send_message_to_all"
    room_id: str = Field(min_length=1)
    message_to_all: str = Field(min_length=1)


class LoginMessage(BaseMessage):
    type: Literal["login"] = "login"
    username: str = Field(min_length=1)
    password: str = Field(min_length=1)
    role: str = Field(min_length=1, pattern=r"^(teacher|student)$")

class JoinRoomMessage(BaseMessage):
    type: Literal["join_room"] = "join_room"
    room_id: str = Field(min_length=1)
    username: str = Field(min_length=1)
    mssv: str = Field(min_length=1)
    student_name: str = Field(min_length=1)
    
class CreateRoomMessage(BaseMessage):
    type: Literal["create_room"] = "create_room"
    room_id: str = Field(min_length=1)
    teacher: str = Field(min_length=1)
    

    

class ChatMessage(BaseMessage):
    type: Literal["chat_message"] = "chat_message"
    sender_id: str = Field(min_length=1)
    receiver_id: str = Field(min_length=1)
    message: str = Field(min_length=1)
    timestamp: str = Field(pattern=r'^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$')
