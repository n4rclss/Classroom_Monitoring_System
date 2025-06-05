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
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS active_sessions (
                    username TEXT PRIMARY KEY,
                    FOREIGN KEY (username) REFERENCES users(username) ON DELETE CASCADE
                )
            """)

            # Rooms table
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS rooms (
                    room_id TEXT PRIMARY KEY,
                    teacher_username TEXT NOT NULL,
                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (teacher_username) REFERENCES users(username) ON DELETE CASCADE
                )
            """)

            # Room participants (students)
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS room_participants (
                    room_id TEXT NOT NULL,
                    student_username TEXT NOT NULL,
                    student_name TEXT NOT NULL,
                    mssv TEXT NOT NULL,
                    joined_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (room_id, student_username),
                    FOREIGN KEY (room_id) REFERENCES rooms(room_id) ON DELETE CASCADE,
                    FOREIGN KEY (student_username) REFERENCES users(username) ON DELETE CASCADE
                )
            """)

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




    # --- Room Management Methods ---

    def create_room(self, room_id: str, teacher_username: str) -> bool:
        """Create a new room in the database."""
        try:
            with self._get_cursor() as cursor:
                cursor.execute("""
                    INSERT INTO rooms (room_id, teacher_username)
                    VALUES (?, ?)
                """, (room_id, teacher_username))
                return True
        except sqlite3.IntegrityError: # Handles PRIMARY KEY violation (room_id exists)
            return False
        except sqlite3.Error as e:
            print(f"Database error creating room {room_id}: {e}")
            return False

    def room_exists(self, room_id: str) -> bool:
        """Check if a room exists in the database."""
        with self._get_cursor() as cursor:
            cursor.execute("SELECT 1 FROM rooms WHERE room_id = ?", (room_id,))
            return cursor.fetchone() is not None

    def delete_room(self, room_id: str) -> bool:
        """Delete a room and its participants from the database."""
        try:
            with self._get_cursor() as cursor:
                # Cascading delete should handle participants, but explicit check is safer
                cursor.execute("DELETE FROM rooms WHERE room_id = ?", (room_id,))
                return cursor.rowcount > 0
        except sqlite3.Error as e:
            print(f"Database error deleting room {room_id}: {e}")
            return False

    # --- Participant Management Methods ---

    def join_room(self, room_id: str, student_username: str, student_name: str, mssv: str) -> bool:
        """Add a student participant to a room in the database."""
        try:
            with self._get_cursor() as cursor:
                cursor.execute("""
                    INSERT INTO room_participants (room_id, student_username, student_name, mssv)
                    VALUES (?, ?, ?, ?)
                """, (room_id, student_username, student_name, mssv))
                return True
        except sqlite3.IntegrityError: # Handles PRIMARY KEY violation (already joined) or FK violation (bad room/user)
            # Check if already joined
            with self._get_cursor() as cursor_check:
                cursor_check.execute("SELECT 1 FROM room_participants WHERE room_id = ? AND student_username = ?", (room_id, student_username))
                if cursor_check.fetchone():
                    return True # Already joined, treat as success
            print(f"Integrity error joining room {room_id} for user {student_username}. Room or user might not exist, or already joined.")
            return False
        except sqlite3.Error as e:
            print(f"Database error joining room {room_id} for user {student_username}: {e}")
            return False

    def leave_room(self, room_id: str, student_username: str) -> bool:
        """Remove a student participant from a room in the database."""
        try:
            with self._get_cursor() as cursor:
                cursor.execute("DELETE FROM room_participants WHERE room_id = ? AND student_username = ?", (room_id, student_username))
                return cursor.rowcount > 0
        except sqlite3.Error as e:
            print(f"Database error leaving room {room_id} for user {student_username}: {e}")
            return False

    def get_room_participants(self, room_id: str) -> List[Dict]:
        """Get all participants for a specific room from the database."""
        with self._get_cursor() as cursor:
            cursor.execute("""
                SELECT student_username, student_name, mssv 
                FROM room_participants 
                WHERE room_id = ?
            """, (room_id,))
            return [dict(row) for row in cursor.fetchall()]

    def leave_all_rooms(self, username: str) -> bool:
        """Remove a user from all rooms they are participating in."""
        try:
            with self._get_cursor() as cursor:
                # Check if user is teacher or student to know which FK to use
                # Simpler: just delete from participants table
                cursor.execute("DELETE FROM room_participants WHERE student_username = ?", (username,))
                # We might also want to delete rooms created by this user if they are a teacher?
                # cursor.execute("DELETE FROM rooms WHERE teacher_username = ?", (username,))
                return True # Assume success even if no rows were deleted
        except sqlite3.Error as e:
            print(f"Database error removing user {username} from all rooms: {e}")
            return False

