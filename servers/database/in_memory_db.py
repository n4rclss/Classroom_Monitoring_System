# server/database/in_memory_db.py
from datetime import datetime
class ClassroomDatabase:
    """In-memory database for user and room management"""
    def __init__(self):
        self.users = {
            "teacher1": {"password": "teach123", "role": "teacher"},
            "student1": {"password": "stu456", "role": "student"}
        }
        self.rooms = {}
        self.active_sessions = {}
        self.chat_history = []

    def authenticate(self, username, password, role):
        user = self.users.get(username)
        return user and user["password"] == password and user["role"] == role

    def get_role(self, username):
        return self.users.get(username, {}).get("role")

    def create_room(self, room_id, teacher):
        self.rooms[room_id] = {
            "teacher": teacher,
            "students": set(),
            "chat_history": []
        }

    def join_room(self, username, room_id):
        role = self.get_role(username)
        if not role or room_id not in self.rooms:
            return False
        
        if role == "teacher":
            self.rooms[room_id]["teacher"] = username
        else:
            self.rooms[room_id]["students"].add(username)
        return True

    def add_chat_message(self, message: dict):
        """Store chat message with validation"""
        message['timestamp'] = datetime.utcnow().isoformat() + 'Z'
        self.chat_history.append(message)