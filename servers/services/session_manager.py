# servers_merged/servers/services/session_manager.py
import json
import logging
from datetime import datetime
from typing import Dict, List, Set, Any

class SessionManager:
    """Manages active connections (via DB) and room participants/messages (in-memory)."""
    def __init__(self, db):
        self.db = db  # The SQLite DB instance (for sessions/auth)
        # In-memory stores for non-persistent data
        self.connections: Dict[str, Any] = {}  # Maps username -> protocol instance
        self.rooms: Dict[str, Dict] = {} # {room_id: {"teacher": str, "students": {username: {"student_name": str, "mssv": str}}, "chat_history": []}}
        self.room_participants: Dict[str, Set[str]] = {} # {room_id: set(usernames)} - Redundant? rooms dict has students
        self.chat_history: list = [] # Global chat history? Or should be per-room?
        self.protocol_connections: Dict[int, Any] = {} # Maps protocol id -> protocol instance (for unregistered connections)

    # --- Session Management (Uses Database) ---

    def get_connection(self, username: str) -> Any:
        """Retrieve active protocol instance for a logged-in user."""
        # Check DB first to see if session *should* exist
        if self.db.get_active_session(username):
            # Return the actual protocol instance from memory
            return self.connections.get(username)
        return None

    def register_connection(self, protocol: Any):
        """Temporarily store protocol instance before login."""
        self.protocol_connections[id(protocol)] = protocol
        logging.debug(f"Protocol connection registered: {id(protocol)}")

    def remove_connection_object(self, protocol: Any):
        """Remove protocol instance if login never happened."""
        if id(protocol) in self.protocol_connections:
            del self.protocol_connections[id(protocol)]
            logging.debug(f"Protocol connection removed: {id(protocol)}")

    def register_user(self, username: str, protocol: Any):
        """Register user session in DB and store protocol instance."""
        self.db.add_active_session(username) # Add to DB
        self.connections[username] = protocol # Store protocol instance mapped by username
        protocol.username = username # Assign username to protocol
        # Remove from temporary storage if present
        if id(protocol) in self.protocol_connections:
            del self.protocol_connections[id(protocol)]
        logging.info(f"User {username} session registered.")

    def remove_connection(self, username: str):
        """Remove user session from DB and in-memory structures."""
        if self.db.get_active_session(username):
            self.db.remove_active_session(username) # Remove from DB
            if username in self.connections:
                del self.connections[username] # Remove protocol instance
            self.leave_all_rooms(username) # Clean up in-memory room participation
            logging.info(f"User {username} session removed.")
        else:
            logging.warning(f"Attempted to remove non-existent session for user {username}")

    # --- Room and Message Management (In-Memory) ---

    def create_room_in_memory(self, room_id: str, teacher: str) -> bool:
        """Create a room in the in-memory store."""
        if room_id in self.rooms:
            logging.warning(f"Attempted to create existing room: {room_id}")
            return False # Room already exists
        self.rooms[room_id] = {
            "teacher": teacher,
            "students": {},
            "chat_history": [] # Per-room chat history
        }
        self.room_participants[room_id] = set() # Initialize participant set
        logging.info(f"In-memory room created: {room_id} by {teacher}")
        return True

    def room_exists_in_memory(self, room_id: str) -> bool:
        """Check if a room exists in the in-memory store."""
        return room_id in self.rooms

    def join_room_in_memory(self, username: str, room_id: str, student_name: str, mssv: str) -> bool:
        """Add a student to an in-memory room."""
        if not self.room_exists_in_memory(room_id):
            logging.error(f"Attempted to join non-existent room: {room_id}")
            return False
        if username in self.rooms[room_id]["students"]:
            logging.warning(f"User {username} already in room {room_id}")
            # Ensure participant set is consistent
            if room_id in self.room_participants:
                 self.room_participants[room_id].add(username)
            return True # Treat as success if already joined

        self.rooms[room_id]["students"][username] = {
            "student_name": student_name,
            "mssv": mssv
        }
        if room_id not in self.room_participants:
             self.room_participants[room_id] = set()
        self.room_participants[room_id].add(username)
        logging.info(f"User {username} joined in-memory room {room_id}")
        return True

    def leave_room(self, username: str, room_id: str):
        """Remove user from an in-memory room."""
        if room_id in self.rooms and username in self.rooms[room_id]["students"]:
            del self.rooms[room_id]["students"][username]
            logging.info(f"User {username} removed from in-memory room details {room_id}")
        if room_id in self.room_participants:
            self.room_participants[room_id].discard(username)
            logging.info(f"User {username} removed from in-memory room participants set {room_id}")
            # Optional: Clean up empty room participant set? (Leave room dict)
            # if not self.room_participants[room_id]:
            #     del self.room_participants[room_id]

    def leave_all_rooms(self, username: str):
        """Remove user from all in-memory rooms."""
        rooms_left = []
        for room_id in list(self.rooms.keys()): # Iterate over keys copy
            if username in self.rooms[room_id]["students"]:
                del self.rooms[room_id]["students"][username]
                rooms_left.append(room_id)
        for room_id in list(self.room_participants.keys()):
            if username in self.room_participants[room_id]:
                self.room_participants[room_id].discard(username)
                if room_id not in rooms_left:
                     rooms_left.append(room_id)
        if rooms_left:
            logging.info(f"User {username} removed from in-memory rooms: {rooms_left}")

    def get_room_participants_in_memory(self, room_id: str) -> List[Dict]:
        """Get participants of an in-memory room."""
        if not self.room_exists_in_memory(room_id):
            return []
        # Return list of student details
        return list(self.rooms[room_id]["students"].values())

    def get_room_participant_usernames(self, room_id: str) -> Set[str]:
         """Get usernames of participants in an in-memory room."""
         return self.room_participants.get(room_id, set())

    def add_chat_message_in_memory(self, message: Dict):
        """Store chat message in the global in-memory list (or per-room if adapted)."""
        # Add to global history for now
        message["timestamp"] = datetime.utcnow().isoformat() + "Z"
        self.chat_history.append(message)
        # TODO: Consider adding to self.rooms[room_id]["chat_history"] instead if needed
        logging.debug(f"Chat message added to in-memory history: {message}")

    def send_direct_message(self, sender_id: str, receiver_id: str, message_json: str) -> bool:
        """Send a direct message to a connected user."""
        receiver_conn = self.get_connection(receiver_id)
        if receiver_conn:
            try:
                # Assuming protocol has sendLine method
                receiver_conn.sendLine(message_json.encode() + b"\n")
                logging.info(f"Direct message sent from {sender_id} to {receiver_id}")
                return True
            except Exception as e:
                logging.error(f"Failed to send direct message to {receiver_id}: {e}")
                return False
        else:
            logging.warning(f"Cannot send direct message: Receiver {receiver_id} not connected.")
            return False

    def broadcast_message(self, room_id: str, message_json: str, sender_id: str):
        """Broadcast message to all connected participants in an in-memory room."""
        if not self.room_exists_in_memory(room_id):
            logging.warning(f"Cannot broadcast: Room {room_id} does not exist in memory.")
            return

        participants = self.get_room_participant_usernames(room_id)
        encoded_message = message_json.encode() + b"\n"
        sent_count = 0
        total_participants = len(participants)

        for username in participants:
            # Optionally skip sending to self if sender is in the room
            # if username == sender_id:
            #     continue
            conn = self.get_connection(username)
            if conn:
                try:
                    conn.sendLine(encoded_message)
                    sent_count += 1
                except Exception as e:
                    logging.error(f"Failed to broadcast to {username} in room {room_id}: {e}")
            else:
                logging.debug(f"User {username} in room {room_id} is not connected for broadcast.")

        logging.info(f"Broadcast message from {sender_id} sent to {sent_count}/{total_participants} connected users in room {room_id}.")


