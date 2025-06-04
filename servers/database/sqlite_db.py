import sqlite3
from contextlib import contextmanager
from datetime import datetime
from typing import Dict, List, Optional
import os

script_dir = os.path.dirname(os.path.abspath(__file__))
default_db_path = os.path.join(script_dir, 'classroom.db')

class ClassroomDatabase:
    def __init__(self, db_path: str = default_db_path):
        self.db_path = db_path
        self._initialize_db()

    @contextmanager
    def _get_cursor(self):
        conn = sqlite3.connect(self.db_path)
        conn.row_factory = sqlite3.Row
        try:
            cursor = conn.cursor()
            yield cursor
            conn.commit()
        except Exception as e:
            conn.rollback()
            raise e
        finally:
            conn.close()

    def _initialize_db(self):
        with self._get_cursor() as cursor:
            # Users table
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS users (
                    username TEXT PRIMARY KEY,
                    password TEXT NOT NULL,
                    role TEXT NOT NULL CHECK (role IN ('teacher', 'student'))
                )
            ''')

            # Rooms table
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS rooms (
                    room_id TEXT PRIMARY KEY,
                    teacher TEXT NOT NULL,
                    FOREIGN KEY (teacher) REFERENCES users(username))
            ''')

            # Room participants (students)
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS room_participants (
                    room_id TEXT,
                    username TEXT,
                    student_name TEXT NOT NULL,
                    mssv TEXT NOT NULL,
                    PRIMARY KEY (room_id, username),
                    FOREIGN KEY (room_id) REFERENCES rooms(room_id),
                    FOREIGN KEY (username) REFERENCES users(username))
            ''')

            # Active sessions
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS active_sessions (
                    username TEXT PRIMARY KEY,
                    FOREIGN KEY (username) REFERENCES users(username))
            ''')

            # Chat history
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS chat_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    sender_id TEXT NOT NULL,
                    receiver_id TEXT NOT NULL,
                    message TEXT NOT NULL,
                    timestamp TEXT NOT NULL,
                    FOREIGN KEY (sender_id) REFERENCES users(username),
                    FOREIGN KEY (receiver_id) REFERENCES users(username))
            ''')

    def authenticate(self, username: str, password: str, role: str) -> bool:
        with self._get_cursor() as cursor:
            cursor.execute('''
                SELECT 1 FROM users 
                WHERE username = ? AND password = ? AND role = ?
            ''', (username, password, role))
            return cursor.fetchone() is not None

    def get_role(self, username: str) -> Optional[str]:
        with self._get_cursor() as cursor:
            cursor.execute('SELECT role FROM users WHERE username = ?', (username,))
            result = cursor.fetchone()
            return result['role'] if result else None

    def create_room(self, room_id: str, teacher: str) -> bool:
        try:
            with self._get_cursor() as cursor:
                cursor.execute('''
                    INSERT INTO rooms (room_id, teacher)
                    VALUES (?, ?)
                ''', (room_id, teacher))
                return True
        except sqlite3.IntegrityError:
            # Room ID likely already exists
            return False

    def room_exists(self, room_id: str) -> bool:
        """Check if a room with the given ID exists in the database."""
        with self._get_cursor() as cursor:
            cursor.execute('SELECT 1 FROM rooms WHERE room_id = ?', (room_id,))
            return cursor.fetchone() is not None

    def join_room(self, username: str, room_id: str, student_name: str, mssv: str) -> bool:
        # First, ensure the room exists
        if not self.room_exists(room_id):
            return False # Or raise an exception
        try:
            with self._get_cursor() as cursor:
                cursor.execute('''
                    INSERT INTO room_participants (room_id, username, student_name, mssv)
                    VALUES (?, ?, ?, ?)
                ''', (room_id, username, student_name, mssv))
                return True
        except sqlite3.IntegrityError:
            # User might already be in the room
            return False

    def get_room_participants(self, room_id: str) -> List[Dict]:
        with self._get_cursor() as cursor:
            cursor.execute('''
                SELECT username, student_name, mssv 
                FROM room_participants 
                WHERE room_id = ?
            ''', (room_id,))
            return [dict(row) for row in cursor.fetchall()]

    def add_active_session(self, username: str) -> None:
        with self._get_cursor() as cursor:
            cursor.execute('''
                INSERT OR REPLACE INTO active_sessions (username)
                VALUES (?)
            ''', (username,))

    def remove_active_session(self, username: str) -> None:
        with self._get_cursor() as cursor:
            cursor.execute('DELETE FROM active_sessions WHERE username = ?', (username,))

    def get_active_session(self, username: str) -> bool:
        with self._get_cursor() as cursor:
            cursor.execute('SELECT 1 FROM active_sessions WHERE username = ?', (username,))
            return cursor.fetchone() is not None

    def add_chat_message(self, message: Dict) -> None:
        with self._get_cursor() as cursor:
            cursor.execute('''
                INSERT INTO chat_history (sender_id, receiver_id, message, timestamp)
                VALUES (?, ?, ?, ?)
            ''', (message['sender_id'], message['receiver_id'], 
                 message['message'], datetime.utcnow().isoformat() + 'Z'))

