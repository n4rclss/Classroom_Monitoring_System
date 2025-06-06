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
    
class PacketStreaming(PacketBase):
    # Sent by initiator to server to request streaming from target
    type: Literal["streaming"] = "streaming"
    target_username: str = Field(min_length=1) # Renamed from 'username' for clarity
    
class PacketScreenData(PacketBase):
    # Sent by target back to server, intended for initiator
    type: Literal["screen_data"] = "screen_data"
    image_data: str = Field(min_length=1) # Assuming base64 encoded image data
    sender_client_id: str = Field(min_length=1)  # Added to identify the teacher for context
    
class PacketNotify(PacketBase):
    type: Literal["notify"] = "notify"
    room_id: str = Field(min_length=1)
    noti_message: str = Field(min_length=1)
    
class PacketCreateRoom(PacketBase):
    type: Literal["create_room"] = "create_room"
    room_id: str = Field(min_length=1)
    teacher: str = Field(min_length=1)
    
class PacketLogout(PacketBase):
    type: Literal["logout"] = "logout"
    # These fields seem incorrect for logout, usually just username is needed.
    # Keeping them as per the provided file for now.
    room_id: str = Field(min_length=1) 
    teacher: str = Field(min_length=1)
    # It should likely be:
    # username: str = Field(min_length=1)

# Note: We also need packet formats for messages SENT BY the server to clients,
# e.g., for starting the stream. These aren't validated on input here,
# but defined implicitly in the sending logic (main.py/handlers).
# Example server-to-target format (conceptual):
# {
#   "type": "start_streaming",
#   "initiator_client_id": "client_id_of_requester"
# }
# Example server-to-initiator format (conceptual):
# {
#   "type": "screen_data",
#   "image_data": "..." 
# }

