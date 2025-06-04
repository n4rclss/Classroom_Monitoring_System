import json
from models.messages import ChatMessage
class SessionManager:
    """Manages active connections and room participants"""
    def __init__(self, db):
        self.db = db
        self.connections = {}
        self.room_participants = {}  # {room_id: set(usernames)}
        
    def get_connection(self, username):
        """Retrieve active protocol instance for a user, if exists."""
        return self.db.active_sessions.get(username, None)

    def register_connection(self, protocol):
        self.connections[id(protocol)] = protocol
        print(id(protocol), ' connection registered')

    def register_user(self, username, protocol):
        self.db.active_sessions[username] = protocol
        protocol.username = username  # Set username in protocol

    def remove_connection(self, username):
        if username in self.db.active_sessions:
            # Remove from all rooms
            self.leave_all_rooms(username)
            del self.db.active_sessions[username]

    def join_room(self, username, room_id):
        """Add user to room participants tracking"""
        if room_id not in self.room_participants:
            return "Room does not exist!"
        self.room_participants[room_id].add(username)
        print(f"User {username} joined room {room_id}")

    def leave_room(self, username, room_id):
        """Remove user from room participants"""
        if room_id in self.room_participants:
            self.room_participants[room_id].discard(username)
            if not self.room_participants[room_id]:  # Empty room
                del self.room_participants[room_id]

    def leave_all_rooms(self, username):
        """Remove user from all rooms (for cleanup)"""
        rooms_to_clean = []
        for room_id, participants in self.room_participants.items():
            if username in participants:
                participants.discard(username)
                if not participants:  # Empty room
                    rooms_to_clean.append(room_id)
        
        for room_id in rooms_to_clean:
            del self.room_participants[room_id]

    # def broadcast_room_update(self, room_id):
    #     if room_id not in self.db.rooms:
    #         return
    #     print("room_id :",room_id)
    #     print("self.connections element: ", len(self.connections))
    #     print("self.db.rooms: ",self.db.rooms[room_id])
        
    #     participants = {
    #         "teacher": self.db.rooms[room_id]["teacher"],
    #         "students": list(self.db.rooms[room_id]["students"])
    #     }
    #     message = json.dumps({
    #         "type": "room_update",
    #         "participants": participants
    #     })
    #     self.broadcast_message(room_id, message)

    # def broadcast_message(self, room_id: str, message: str):
    #     """Fixed broadcast that actually works"""
    #     print("Broadcasting to room:", room_id)
    #     print("Room participants:", self.room_participants.get(room_id, set()))
        
    #     if room_id not in self.room_participants:
    #         print(f"Room {room_id} has no participants.")
    #         return 
        
    #     for participant in self.room_participants[room_id]:
    #         if participant in self.db.active_sessions:
    #             try:
    #                 self.db.active_sessions[participant].sendLine(message.encode())
    #                 print(f"Message sent to {participant}")
    #             except Exception as e:
    #                 print(f"Failed to send message to {participant}: {e}")
    #         else:
    #             print(f"Participant {participant} not in active sessions")
