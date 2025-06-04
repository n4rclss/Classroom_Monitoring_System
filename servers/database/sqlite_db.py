import sqlite3
from contextlib import contextmanager
from typing import Dict, List, Optional
import os

script_dir = os.path.dirname(os.path.abspath(__file__))
default_db_path = os.path.join(script_dir, 'classroom.db')

class ClassroomDatabase:
    """SQLite database for user authentication and session management only."""
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
        """Initialize only the tables needed for users and sessions."""
        with self._get_cursor() as cursor:
            # Users table
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS users (
                    username TEXT PRIMARY KEY,
                    password TEXT NOT NULL,
                    role TEXT NOT NULL CHECK (role IN ('teacher', 'student'))
                )
            ''')

            # Active sessions
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS active_sessions (
                    username TEXT PRIMARY KEY,
                    FOREIGN KEY (username) REFERENCES users(username)
                )
            ''')
            # Removed rooms, room_participants, and chat_history tables

    def authenticate(self, username: str, password: str, role: str) -> bool:
        """Authenticate user against the database."""
        with self._get_cursor() as cursor:
            cursor.execute('''
                SELECT 1 FROM users 
                WHERE username = ? AND password = ? AND role = ?
            ''', (username, password, role))
            return cursor.fetchone() is not None

    def get_role(self, username: str) -> Optional[str]:
        """Get user role from the database."""
        with self._get_cursor() as cursor:
            cursor.execute('SELECT role FROM users WHERE username = ?', (username,))
            result = cursor.fetchone()
            return result['role'] if result else None

    def add_active_session(self, username: str) -> None:
        """Add or replace an active session in the database."""
        with self._get_cursor() as cursor:
            cursor.execute('''
                INSERT OR REPLACE INTO active_sessions (username)
                VALUES (?)
            ''', (username,))

    def remove_active_session(self, username: str) -> None:
        """Remove an active session from the database."""
        with self._get_cursor() as cursor:
            cursor.execute('DELETE FROM active_sessions WHERE username = ?', (username,))

    def get_active_session(self, username: str) -> bool:
        """Check if a user has an active session in the database."""
        with self._get_cursor() as cursor:
            cursor.execute('SELECT 1 FROM active_sessions WHERE username = ?', (username,))
            return cursor.fetchone() is not None


