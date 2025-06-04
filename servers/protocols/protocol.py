# servers_merged/servers/protocols/protocol.py
from twisted.internet.protocol import Factory
from twisted.protocols.basic import LineReceiver
from utils.parser import parse_message
import logging
import json
from datetime import datetime
from pydantic import ValidationError

class ClassProtocol(LineReceiver):
    delimiter = b'\n'

    def __init__(self, db, session_mgr):
        self.db = db # SQLite DB for auth/sessions
        self.session_mgr = session_mgr # Manages sessions (DB) and rooms/messages (memory)
        self.username = None
        self.current_room = None

    def connectionMade(self):
        # Register connection temporarily in session manager
        self.session_mgr.register_connection(self)
        logging.info(f"New connection established: {self.transport.getPeer()}")

    def lineReceived(self, line: bytes):
        try:
            message = parse_message(line)
            handler = getattr(self, f'handle_{message.type}', None)

            if not handler:
                raise ValueError(f"Unsupported message type: {message.type}")

            # Check login status for actions requiring authentication
            if message.type not in ["login"] and not self.username:
                 raise ValueError("User must be logged in for this action.")

            handler(message)

        except ValidationError as ve:
            logging.error(f"Validation error processing line: {line.decode(errors='ignore')} - {ve}")
            self.send_error(f"Invalid message format: {ve}")
        except ValueError as ve:
            logging.error(f"Value error processing line: {line.decode(errors='ignore')} - {ve}")
            self.send_error(str(ve))
        except Exception as e:
            logging.exception(f"Unexpected protocol error processing line: {line.decode(errors='ignore')}")
            self.send_error(f"An unexpected server error occurred: {e}")

    # --- Handler Methods ---

    def handle_login(self, message):
        # Uses DB for authentication
        if not self.db.authenticate(message.username, message.password, message.role):
            raise ValueError("Invalid credentials")

        # Check if user already has an active session via SessionManager (which checks DB)
        # Note: The updated SessionManager.get_connection checks DB and returns protocol instance if found in memory
        existing_conn = self.session_mgr.get_connection(message.username)
        if existing_conn and existing_conn != self:
             # Handle already logged-in user (e.g., disconnect old session or deny new one)
             # For now, just log a warning. A more robust solution might be needed.
             logging.warning(f"User {message.username} attempted to log in again while already active.")
             # Optional: Disconnect the old connection
             # old_protocol = self.session_mgr.connections.get(message.username)
             # if old_protocol:
             #     old_protocol.transport.loseConnection()
             #     logging.info(f"Disconnected previous session for user {message.username}.")
             # raise ValueError("User already logged in elsewhere.") # Or deny login

        # Register user session in DB and SessionManager memory
        self.session_mgr.register_user(message.username, self)
        self.username = message.username # Set username AFTER successful registration
        logging.info(f"User {self.username} logged in successfully from {self.transport.getPeer()}")
        self.send_success("login successful")

    def handle_create_room(self, message):
        # Uses SessionManager in-memory method
        # Ensure only teachers can create rooms (check role via DB)
        user_role = self.db.get_role(self.username)
        if user_role != 'teacher':
            raise ValueError("Only teachers can create rooms.")
        if message.teacher != self.username:
             raise ValueError("Teacher username in message does not match logged-in user.")

        if not self.session_mgr.create_room_in_memory(message.room_id, message.teacher):
            raise ValueError(f"Room creation failed. Room ID '{message.room_id}' might already exist.")

        self.send_success(f"Room '{message.room_id}' created successfully")

    def handle_join_room(self, message):
        # Uses SessionManager in-memory methods
        # Ensure only students can join rooms (check role via DB)
        user_role = self.db.get_role(self.username)
        if user_role != 'student':
            raise ValueError("Only students can join rooms.")
        if message.username != self.username:
             raise ValueError("Username in message does not match logged-in user.")

        # Check if room exists in memory
        if not self.session_mgr.room_exists_in_memory(message.room_id):
             raise ValueError(f"Room '{message.room_id}' does not exist.")

        # Attempt to join the room in memory
        join_success = self.session_mgr.join_room_in_memory(
            self.username, message.room_id, message.student_name, message.mssv
        )

        if join_success:
            self.current_room = message.room_id # Update protocol's current room
            logging.info(f"User {self.username} successfully joined in-memory room {message.room_id}")
            self.send_success(f"Joined room '{message.room_id}' successfully!")
        else:
            # This path might be less likely now with the pre-check, but handle defensively
            logging.warning(f"In-memory join failed for user {self.username} in room {message.room_id}. Might already be a participant or room disappeared.")
            # Check if already in room again just in case
            if self.session_mgr.room_exists_in_memory(message.room_id) and self.username in self.session_mgr.rooms[message.room_id]["students"]:
                 self.send_success(f"Already in room '{message.room_id}'.") # Treat as success if already joined
                 if self.current_room != message.room_id:
                     self.current_room = message.room_id
            else:
                 self.send_error("Failed to join room. Please try again.")

    def handle_refresh(self, message):
        # Uses SessionManager in-memory method
        if not self.session_mgr.room_exists_in_memory(message.room_id):
            raise ValueError(f"Room '{message.room_id}' does not exist.")

        # Get participant details (list of dicts with name, mssv)
        participants_details = self.session_mgr.get_room_participants_in_memory(message.room_id)

        # Send the list of participant details
        self.sendLine(json.dumps({
            "status": "success",
            "message": "Participants fetched successfully",
            "participants": participants_details # Send list of student details
        }).encode() + self.delimiter)

    def handle_send_message_to_all(self, message):
        # Uses SessionManager in-memory methods
        # Ensure only teacher can broadcast (check role via DB)
        user_role = self.db.get_role(self.username)
        if user_role != 'teacher':
            raise ValueError("Only teachers can send messages to all.")

        room_id = message.room_id
        content = message.message_to_all

        if not self.session_mgr.room_exists_in_memory(room_id):
            raise ValueError(f"Room '{room_id}' does not exist.")

        # Prepare broadcast message data
        broadcast_data = {
            "type": "broadcast_message",
            "sender": self.username,
            "room_id": room_id,
            "content": content,
            "timestamp": datetime.utcnow().isoformat() + 'Z'
        }
        message_json = json.dumps(broadcast_data)

        # Use SessionManager to broadcast
        self.session_mgr.broadcast_message(room_id, message_json, self.username)

        # Send confirmation back to teacher
        self.send_success("Message broadcast to room participants")

    def handle_chat_message(self, message):
        # Uses SessionManager in-memory methods for storage and delivery
        if message.sender_id != self.username:
             raise ValueError("Sender ID mismatch.")

        # Add timestamp if not present (SessionManager might also do this)
        if not hasattr(message, 'timestamp') or not message.timestamp:
             message.timestamp = datetime.utcnow().isoformat() + 'Z'

        # Store message in memory via SessionManager
        # Note: The current session_mgr adds to a global list. Adapt if per-room history is needed.
        self.session_mgr.add_chat_message_in_memory(message.model_dump())

        # Send message via SessionManager
        success = self.session_mgr.send_direct_message(
            message.sender_id,
            message.receiver_id,
            message.model_dump_json() # Send the Pydantic model as JSON string
        )

        if not success:
             # Don't raise error, just inform sender
             self.send_error(f"User '{message.receiver_id}' is not online or message could not be sent.")
        # else: # Optionally send success back to sender
             # self.send_success("Chat message sent.")

    def connectionLost(self, reason):
        peer = self.transport.getPeer()
        user_info = self.username or "unidentified user"
        logging.info(f"Disconnecting {user_info} from {peer.host}:{peer.port}. Reason: {reason.getErrorMessage()}")

        # Clean up user session (DB) and in-memory state using SessionManager
        if self.username:
            # remove_connection handles DB session removal and leaving all in-memory rooms
            self.session_mgr.remove_connection(self.username)
            logging.info(f"User {self.username} session and room participation cleaned up.")
        else:
             # If login never completed, remove the temporary protocol connection object
             self.session_mgr.remove_connection_object(self)

        logging.info(f"Connection lost processing complete for {user_info}.")

    # --- Helper Methods ---

    def send_success(self, message: str):
        response = {"status": "success", "message": message}
        self.sendLine(json.dumps(response).encode() + self.delimiter)

    def send_error(self, message: str):
        response = {"status": "error", "message": message}
        self.sendLine(json.dumps(response).encode() + self.delimiter)

# --- Factory --- #

class ClassProtocolFactory(Factory):
    protocol = ClassProtocol

    def __init__(self, db, session_mgr):
        self.db = db
        self.session_mgr = session_mgr
        logging.info("ClassProtocolFactory initialized with DB and SessionManager.")

    def buildProtocol(self, addr):
        logging.info(f"Building protocol for address: {addr}")
        # Inject dependencies into each new protocol instance
        proto = self.protocol(self.db, self.session_mgr)
        return proto

