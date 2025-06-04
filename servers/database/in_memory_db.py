# server/database/in_memory_db.py
from datetime import datetime
class ClassroomDatabase:
    """In-memory database for user and room management"""
    def __init__(self):
        self.users = {
            "teacher": {"password": "t", "role": "teacher"},
            "stu1": {"password": "s", "role": "student"},
            "stu2": {"password": "ss", "role": "student"}
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
        print('------',room_id, teacher)
        self.rooms[room_id] = {
            "teacher": teacher,
            "students": {},
            "chat_history": []
        }
        return (f"Room {room_id} created by {teacher} successfully!")

    def join_room(self, username, room_id, student_name, mssv):
        if room_id not in self.rooms:
            return "Room does not exist!"
        
        self.rooms[room_id]["students"][username] = {
            "student_name": student_name,
            "mssv": mssv
        }
        return ("Joined room successfully!")

    def add_chat_message(self, message: dict):
        """Store chat message with validation"""
        message['timestamp'] = datetime.utcnow().isoformat() + 'Z'
        self.chat_history.append(message)