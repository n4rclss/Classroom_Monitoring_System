from pydantic import BaseModel, ConfigDict, Field
from typing import Literal

class BaseMessage(BaseModel):
    model_config = ConfigDict(extra='forbid', strict=True)

class LoginMessage(BaseMessage):
    type: Literal["login"] = "login"
    username: str = Field(min_length=1)
    password: str = Field(min_length=1)

class JoinClassMessage(BaseMessage):
    type: Literal["join_class"] = "join_class"
    class_id: str = Field(min_length=1)

class ChatMessage(BaseMessage):
    type: Literal["chat_message"] = "chat_message"
    sender_id: str = Field(min_length=1)
    receiver_id: str = Field(min_length=1)
    message: str = Field(min_length=1)
    timestamp: str = Field(pattern=r'^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$')
