import json
from models.messages import ChatMessage

class SessionManager:
    """Manages active connections and room participants"""
    def __init__(self, db):
        self.db = db
        self.connections = {}
        self.room_participants = {}

    def register_connection(self, protocol):
        self.connections[id(protocol)] = protocol


    def register_user(self, username, protocol):
        self.db.active_sessions[username] = protocol

    def remove_connection(self, username):
        if username in self.db.active_sessions:
            del self.db.active_sessions[username]

    def broadcast_room_update(self, room_id):
        participants = {
            "teacher": self.db.rooms[room_id]["teacher"],
            "students": list(self.db.rooms[room_id]["students"])
        }
        message = json.dumps({
            "type": "room_update",
            "participants": participants
        })
        self.broadcast_message(room_id, message)

    def broadcast_message(self, room_id: str, message: ChatMessage):
        if room_id not in self.room_participants:
            return
            
        validated = message.model_dump()
        self.db.add_chat_message(validated)
        
        for participant in self.room_participants[room_id]:
            if participant in self.db.active_sessions:
                self.db.active_sessions[participant].sendLine(
                    message.model_dump_json().encode()
                )
