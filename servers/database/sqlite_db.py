
import sqlite3
from contextlib import contextmanager
from typing import Dict, List, Optional
import os
import datetime

script_dir = os.path.dirname(os.path.abspath(__file__))
default_db_path = os.path.join(script_dir, 'classroom.db')

class ClassroomDatabase:
    """SQLite database for users, rooms, participants, and active client mappings."""
    def __init__(self, db_path: str = default_db_path):
        self.db_path = db_path
        self._initialize_db()

    @contextmanager
    def _get_cursor(self, commit_on_exit: bool = True):
        """Provides a database cursor within a context manager."""
        # Using WAL mode can improve concurrency for readers and one writer
        conn = sqlite3.connect(self.db_path, timeout=10) # Add timeout
        conn.execute("PRAGMA journal_mode=WAL;") # Enable WAL mode
        conn.row_factory = sqlite3.Row
        try:
            cursor = conn.cursor()
            yield cursor
            if commit_on_exit:
                conn.commit()
        except sqlite3.Error as e:
            print(f"[!] Database Error: {e}")
            if commit_on_exit:
                 conn.rollback()
            raise # Re-raise the exception after logging/rollback
        finally:
            conn.close()
    def fetch_all(self, query: str, params: tuple = ()) -> List[sqlite3.Row]:
        try:
            with self._get_cursor(commit_on_exit=False) as cursor:
                cursor.execute(query, params)
                return cursor.fetchall()
        except sqlite3.Error as e:
            print(f"[!] DB Error during fetch_all: {e}")
            return []


    def fetch_one(self, query: str, params: tuple = ()) -> Optional[sqlite3.Row]:
        """Executes a SELECT query and returns a single row result."""
        try:
            with self._get_cursor(commit_on_exit=False) as cursor:
                cursor.execute(query, params)
                return cursor.fetchone()
        except sqlite3.Error as e:
            print(f"[!] DB Error during fetch_one: {e}")
            return None


    def _initialize_db(self):
        """Initialize all required tables."""
        with self._get_cursor() as cursor:
            # Users table
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS users (
                    username TEXT PRIMARY KEY,
                    password TEXT NOT NULL,
                    role TEXT NOT NULL CHECK (role IN ('teacher', 'student'))
                )
            ''')

            # Active sessions (Deprecated by active_clients, but kept for potential other uses)
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
                    teacher TEXT NOT NULL,
                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (teacher) REFERENCES users(username) ON DELETE CASCADE
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
            
            # Active Clients (Username <-> ClientID mapping)
            cursor.execute("""
                CREATE TABLE IF NOT EXISTS active_clients (
                    username TEXT PRIMARY KEY,
                    client_id TEXT NOT NULL UNIQUE, 
                    last_seen TEXT NOT NULL,
                    FOREIGN KEY (username) REFERENCES users(username) ON DELETE CASCADE
                )
            """)
            # Index for faster client_id lookups
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_active_clients_client_id ON active_clients(client_id);")
            print("[*] Database initialized/verified.")

    # --- User Authentication --- 
    def authenticate(self, username: str, password: str, role: str) -> bool:
        with self._get_cursor(commit_on_exit=False) as cursor:
            # print("Authenticating user:", username, "with role:", role)
            cursor.execute('''
                SELECT 1 FROM users 
                WHERE username = ? AND password = ? AND role = ?
            ''', (username, password, role))
            return cursor.fetchone() is not None

    def get_role(self, username: str) -> Optional[str]:
        with self._get_cursor(commit_on_exit=False) as cursor:
            cursor.execute('SELECT role FROM users WHERE username = ?', (username,))
            result = cursor.fetchone()
            return result['role'] if result else None

    # --- Active Client Mapping (for Multi-Server) --- 

    def register_client(self, username: str, client_id: str) -> bool:
        """Registers or updates the client_id for a given username in the database."""
        now = datetime.datetime.utcnow().isoformat()
        try:
            with self._get_cursor() as cursor:
                # Use INSERT OR REPLACE to handle both new registrations and updates
                # This assumes username is the primary key for active clients.
                # We also need to ensure client_id uniqueness manually if a user logs 
                # in elsewhere before the old entry is cleared.
                
                # First, remove any existing entry for this client_id (if different user)
                cursor.execute("DELETE FROM active_clients WHERE client_id = ? AND username != ?", (client_id, username))
                if cursor.rowcount > 0:
                    print(f"[!] Cleared stale client_id 	'{client_id}' from previous user during registration of '{username}'.")

                # Now, insert or replace the entry for the current user
                cursor.execute('''
                    INSERT OR REPLACE INTO active_clients (username, client_id, last_seen)
                    VALUES (?, ?, ?)
                ''', (username, client_id, now))
                print(f"[*] DB: Registered/Updated client: User 	'{username}' -> ClientID '{client_id}'")
                return True
        except sqlite3.IntegrityError as e:
            # This might happen if the client_id UNIQUE constraint is violated *after* the delete check
            # (e.g., race condition, though less likely with WAL and short transactions)
            # Or if the username FK constraint fails (user doesn't exist)
            print(f"[!] DB Integrity Error registering client 	'{username}' with client_id '{client_id}': {e}")
            return False
        except sqlite3.Error as e:
            print(f"[!] DB Error registering client 	'{username}': {e}")
            return False

    def unregister_client(self, username: str) -> bool:
        """Removes the active client mapping for a username."""
        try:
            with self._get_cursor() as cursor:
                cursor.execute("DELETE FROM active_clients WHERE username = ?", (username,))
                if cursor.rowcount > 0:
                    print(f"[*] DB: Unregistered client for user 	'{username}'.")
                    return True
                else:
                    # Not necessarily an error, might have already been unregistered
                    print(f"[*] DB: No active client found to unregister for user 	'{username}'.")
                    return True # Still considered success
        except sqlite3.Error as e:
            print(f"[!] DB Error unregistering client 	'{username}': {e}")
            return False
            
    def unregister_client_by_id(self, client_id: str) -> bool:
        """Removes the active client mapping for a client_id. Useful on disconnect."""
        try:
            with self._get_cursor() as cursor:
                cursor.execute("DELETE FROM active_clients WHERE client_id = ?", (client_id,))
                if cursor.rowcount > 0:
                    print(f"[*] DB: Unregistered client by ClientID 	'{client_id}'.")
                    return True
                else:
                    # Not an error, might have already been unregistered
                    # print(f"[*] DB: No active client found to unregister for ClientID 	'{client_id}'.")
                    return True # Still considered success
        except sqlite3.Error as e:
            print(f"[!] DB Error unregistering client by ClientID 	'{client_id}': {e}")
            return False

    def get_client_id(self, username: str) -> Optional[str]:
        """Retrieves the active client_id for a username."""
        try:
            with self._get_cursor(commit_on_exit=False) as cursor:
                cursor.execute("SELECT client_id FROM active_clients WHERE username = ?", (username,))
                result = cursor.fetchone()
                # print(f"[*] DB: Lookup client_id for user 	'{username}': {'Found ' + result['client_id'] if result else 'Not Found'}")
                return result['client_id'] if result else None
        except sqlite3.Error as e:
            print(f"[!] DB Error getting client_id for user 	'{username}': {e}")
            return None

    def get_username(self, client_id: str) -> Optional[str]:
        """Retrieves the username for an active client_id."""
        try:
            with self._get_cursor(commit_on_exit=False) as cursor:
                cursor.execute("SELECT username FROM active_clients WHERE client_id = ?", (client_id,))
                result = cursor.fetchone()
                # print(f"[*] DB: Lookup username for client_id 	'{client_id}': {'Found ' + result['username'] if result else 'Not Found'}")
                return result['username'] if result else None
        except sqlite3.Error as e:
            print(f"[!] DB Error getting username for client_id 	'{client_id}': {e}")
            return None

    # --- Room Management Methods --- (Largely unchanged)

    def create_room(self, room_id: str, teacher: str) -> bool:
        try:
            # print("Trying creating room with ID:", room_id, "for teacher:", teacher)
            with self._get_cursor() as cursor:
                cursor.execute("INSERT INTO rooms (room_id, teacher) VALUES (?, ?)", (room_id, teacher))
                return True
        except sqlite3.IntegrityError: # Handles PRIMARY KEY violation (room_id exists) or FK violation
            print(f"[!] DB Integrity error creating room 	'{room_id}' for teacher '{teacher}'. Room may exist or teacher invalid.")
            return False
        except sqlite3.Error as e:
            print(f"[!] DB error creating room 	'{room_id}': {e}")
            return False
            
    def get_room_teacher(self, room_id: str) -> Optional[str]:
        """Gets the username of the teacher for a given room_id."""
        try:
            with self._get_cursor(commit_on_exit=False) as cursor:
                cursor.execute("SELECT teacher FROM rooms WHERE room_id = ?", (room_id,))
                result = cursor.fetchone()
                return result['teacher'] if result else None
        except sqlite3.Error as e:
            print(f"[!] DB Error getting teacher for room 	'{room_id}': {e}")
            return None

    def room_exists(self, room_id: str) -> bool:
        with self._get_cursor(commit_on_exit=False) as cursor:
            cursor.execute("SELECT 1 FROM rooms WHERE room_id = ?", (room_id,))
            return cursor.fetchone() is not None

    def delete_room(self, room_id: str) -> bool:
        try:
            with self._get_cursor() as cursor:
                cursor.execute("DELETE FROM rooms WHERE room_id = ?", (room_id,))
                deleted = cursor.rowcount > 0
                if deleted:
                    print(f"[*] DB: Deleted room 	'{room_id}'.")
                    # Also clear participants and potentially related active_clients if needed
                    cursor.execute("DELETE FROM room_participants WHERE room_id = ?", (room_id,))
                return deleted
        except sqlite3.Error as e:
            print(f"[!] DB error deleting room 	'{room_id}': {e}")
            return False

    # --- Participant Management Methods --- (Largely unchanged)

    def join_room(self, room_id: str, student_username: str, student_name: str, mssv: str) -> bool:
        try:
            with self._get_cursor() as cursor:
                cursor.execute("""
                    INSERT INTO room_participants (room_id, student_username, student_name, mssv)
                    VALUES (?, ?, ?, ?)
                """, (room_id, student_username, student_name, mssv))
                print(f"[*] DB: Student 	'{student_username}' joined room '{room_id}'.")
                return True
        except sqlite3.IntegrityError: 
            # Check if already joined (common case)
            with self._get_cursor(commit_on_exit=False) as cursor_check:
                cursor_check.execute("SELECT 1 FROM room_participants WHERE room_id = ? AND student_username = ?", (room_id, student_username))
                if cursor_check.fetchone():
                    print(f"[*] DB: Student 	'{student_username}' already in room '{room_id}'. Join successful.")
                    return True # Already joined, treat as success
            print(f"[!] DB Integrity error joining room 	'{room_id}' for user '{student_username}'. Room/user invalid or other issue.")
            return False
        except sqlite3.Error as e:
            print(f"[!] DB error joining room 	'{room_id}' for user '{student_username}': {e}")
            return False

    def leave_room(self, room_id: str, student_username: str) -> bool:
        try:
            with self._get_cursor() as cursor:
                cursor.execute("DELETE FROM room_participants WHERE room_id = ? AND student_username = ?", (room_id, student_username))
                deleted = cursor.rowcount > 0
                if deleted:
                     print(f"[*] DB: Student 	'{student_username}' left room '{room_id}'.")
                return deleted
        except sqlite3.Error as e:
            print(f"[!] DB error leaving room 	'{room_id}' for user '{student_username}': {e}")
            return False
    
    def get_students_in_room(self, room_id: str) -> List[str]:
        try:
            with self._get_cursor(commit_on_exit=False) as cursor:
                cursor.execute("SELECT student_username FROM room_participants WHERE room_id = ?", (room_id,))
                students = [row["student_username"] for row in cursor.fetchall()]
                # print(f"[*] DB: Found {len(students)} students in room 	'{room_id}'.")
                return students
        except sqlite3.Error as e:
            print(f"[!] DB Error fetching students from room 	'{room_id}': {e}")
            return []

    def get_room_participants(self, room_id: str) -> List[Dict]:
        try:
            with self._get_cursor(commit_on_exit=False) as cursor:
                cursor.execute("SELECT student_username, student_name, mssv FROM room_participants WHERE room_id = ?", (room_id,))
                participants = [
                    {
                        "username": row["student_username"],
                        "student_name": row["student_name"],
                        "mssv": row["mssv"]
                    }
                    for row in cursor.fetchall()
                ]
                # print(f"[*] DB: Found {len(participants)} participants details for room 	'{room_id}'.")
                return participants
        except sqlite3.Error as e:
             print(f"[!] DB Error fetching participant details for room 	'{room_id}': {e}")
             return []

    def leave_all_rooms(self, username: str) -> bool:
        try:
            with self._get_cursor() as cursor:
                # Remove student from all participant lists
                cursor.execute("DELETE FROM room_participants WHERE student_username = ?", (username,))
                deleted_count = cursor.rowcount
                print(f"[*] DB: Removed student 	'{username}' from {deleted_count} rooms.")
                # Optionally: Delete rooms where this user was the teacher? 
                # cursor.execute("DELETE FROM rooms WHERE teacher = ?", (username,))
                return True # Assume success even if no rows were deleted
        except sqlite3.Error as e:
            print(f"[!] DB error removing user 	'{username}' from all rooms: {e}")
            return False

    # --- Deprecated Session Methods (kept for reference) ---
    # These are less useful now that we track active clients via client_id
    def add_active_session(self, username: str) -> None:
        with self._get_cursor() as cursor:
            cursor.execute('INSERT OR REPLACE INTO active_sessions (username) VALUES (?)', (username,))

    def remove_active_session(self, username: str) -> None:
        with self._get_cursor() as cursor:
            cursor.execute('DELETE FROM active_sessions WHERE username = ?', (username,))

    def get_active_session(self, username: str) -> bool:
        with self._get_cursor(commit_on_exit=False) as cursor:
            cursor.execute('SELECT 1 FROM active_sessions WHERE username = ?', (username,))
            return cursor.fetchone() is not None



