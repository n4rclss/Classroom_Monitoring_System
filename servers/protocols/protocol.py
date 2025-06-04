from twisted.internet.protocol import Factory
from twisted.protocols.basic import LineReceiver
from utils.parser import parse_message
import logging
import json
from datetime import datetime


class ClassProtocol(LineReceiver):
    delimiter = b'\n'
    
    def __init__(self, db, session_mgr):
        self.db = db
        self.session_mgr = session_mgr
        self.username = None
        self.current_room = None

    def connectionMade(self):
        self.session_mgr.register_connection(self)
        logging.info(f"New connection established: {self.transport.getPeer()}")

    def lineReceived(self, line: bytes):
        try:
            message = parse_message(line)
      
            handler = getattr(self, f'handle_{message.type}', None)
            
            if not handler:
                raise ValueError("Unsupported message type")
            handler(message)
            
        except Exception as e:
            # Log the full traceback for better debugging
            logging.exception(f"Protocol error processing line: {line.decode(errors='ignore')}")
            self.send_error(str(e))
            
    def handle_create_room(self, message):
        if not self.db.create_room(message.room_id,message.teacher):
            raise ValueError("Room creation failed, possibly room ID already exists.")
        self.send_success(f"Room {message.room_id} created successfully")
        # No return value needed here as send_success handles the response

    def handle_login(self, message):
        if not self.db.authenticate(message.username, message.password, message.role):
            raise ValueError("Invalid credentials")
        
        # Check if user is already logged in elsewhere
        if self.session_mgr.get_connection(message.username):
             # Optionally disconnect the old session or prevent new login
             logging.warning(f"User {message.username} attempted to log in again.")

        self.username = message.username # Set username after successful authentication
        self.session_mgr.register_user(self.username, self)
        logging.info(f"User {self.username} logged in successfully from {self.transport.getPeer()}")
        self.send_success("login")

    def handle_join_room(self, data):
        # Ensure user is logged in before joining a room
        if not self.username:
            raise ValueError("User must be logged in to join a room.")
            
        # Check if room exists before attempting to join
        if not self.db.room_exists(data.room_id):
             raise ValueError(f"Room {data.room_id} does not exist.")

        # Attempt to join the room in the database
        join_success = self.db.join_room(self.username, data.room_id, data.student_name, data.mssv)
        
        if join_success:
            logging.info(f"User {self.username} successfully joined room {data.room_id} in DB.")
            self.current_room = data.room_id
            
            # Register user to the room in the session manager
            self.session_mgr.join_room(self.username, data.room_id)
            logging.info(f"User {self.username} registered to room {data.room_id} in SessionManager.")
            
            # Optionally broadcast room update to other participants (if needed)
            # self.session_mgr.broadcast_room_update(data.room_id)
            
            self.send_success(f"Joined room {data.room_id} successfully!")
           
        else:
            # This case might happen due to constraints (e.g., already in room)
            logging.warning(f"Database join failed for user {self.username} in room {data.room_id}. Might already be a participant.")
            # Check if already in room to provide a more specific message
            participants = self.db.get_room_participants(data.room_id)
            if any(p['username'] == self.username for p in participants):
                 self.send_success(f"Already in room {data.room_id}.") # Treat as success if already joined
                 # Ensure session manager state is consistent
                 if self.current_room != data.room_id:
                     self.current_room = data.room_id
                     self.session_mgr.join_room(self.username, data.room_id) 
            else:
                 self.send_error("Failed to join room. Please try again.")
            
    def handle_refresh(self, message):
        # Ensure room_id is provided
        if not hasattr(message, 'room_id') or not message.room_id:
             raise ValueError("Room ID is required for refresh.")

        participants = self.db.get_room_participants(message.room_id)
        
        # It's okay if participants list is empty, just return it
        self.sendLine(json.dumps({
            "status": "success",
            "message": "Participants fetched successfully",
            "participants": participants # Send empty list if no participants
        }).encode())

    
    def handle_send_message_to_all(self, message):
        if not self.username:
             raise ValueError("User must be logged in to send messages.")

        room_id = message.room_id
        content = message.message_to_all

        # Check if the room exists using the database method
        if not self.db.room_exists(room_id):
            self.send_error(f"Room {room_id} does not exist")
            return

        # Get participants (students) from the database
        participants = self.db.get_room_participants(room_id)
        student_usernames = [p['username'] for p in participants]

        if not student_usernames:
            logging.info(f"No students in room {room_id} to send message to.")
            self.send_success("Message sent (no students in room).") # Or maybe a different message
            return

        # Message to send to all students
        data = {
            "type" : "broadcast_message", # Use a more specific type for client handling
            "sender": self.username, # Should likely be teacher's username
            "room_id": room_id,
            "content": content,
            "timestamp": datetime.utcnow().isoformat() + 'Z'
        }
        encoded_data = json.dumps(data).encode() + self.delimiter # Ensure delimiter is added

        # Send to all connected students in the room via SessionManager
        sent_count = 0
        for username in student_usernames:
            conn = self.session_mgr.get_connection(username)
            if conn:
                try:
                    conn.sendLine(encoded_data) # Use sendLine for consistency
                    sent_count += 1
                except Exception as e:
                     logging.error(f"Failed to send message to {username}: {e}")

        logging.info(f"Teacher {self.username} sent message to {sent_count}/{len(student_usernames)} connected students in room {room_id}: {content}")
        self.send_success("Message sent to all participants")

    def connectionLost(self, reason):
        peer = self.transport.getPeer()
        logging.info(f"Disconnecting {self.username or 'unknown user'} from {peer.host}:{peer.port}")
        
        # Clean up user session and room memberships using SessionManager
        if self.username:
            # Leave the current room in SessionManager
            if self.current_room:
                self.session_mgr.leave_room(self.username, self.current_room)
                logging.info(f"User {self.username} removed from room {self.current_room} in SessionManager.")
            
            self.session_mgr.remove_connection(self.username)
            logging.info(f"User {self.username} session removed.")
        else:
             # If username is None, it means login never completed or failed.
             # Just remove the connection object itself from the initial registration.
             self.session_mgr.remove_connection_object(self)

        logging.info(f"Connection lost: {peer} - Reason: {reason.getErrorMessage()}")


    def handle_chat_message(self, message):
        # Ensure user is logged in
        if not self.username:
             raise ValueError("User must be logged in to chat.")
             
        # Basic validation
        if not all(hasattr(message, attr) for attr in ['sender_id', 'receiver_id', 'message']):
             raise ValueError("Invalid chat message format.")
             
        # Ensure sender_id matches the logged-in user for security
        if message.sender_id != self.username:
             raise ValueError("Sender ID mismatch.")

        # Add timestamp if not present
        if not hasattr(message, 'timestamp'):
             message.timestamp = datetime.utcnow().isoformat() + 'Z'
             
        # Persist message to DB
        try:
            self.db.add_chat_message(message.model_dump()) 
        except Exception as e:
             logging.error(f"Failed to save chat message to DB: {e}")


        # Send message via SessionManager
        success = self.session_mgr.send_direct_message(
            message.sender_id,
            message.receiver_id,
            message.model_dump_json()
        )
        
        if not success:
             self.send_error(f"User {message.receiver_id} is not online.")

    

    def send_success(self, action: str):
        self.sendLine(f'{{"status": "success", "message": "{action}"}}'.encode() + self.delimiter)

    def send_error(self, message: str):
        self.sendLine(f'{{"status": "error", "message": "{message}"}}'.encode() + self.delimiter)

class ClassProtocolFactory(Factory):
    protocol = ClassProtocol 
    
    def __init__(self, db, session_mgr):
        self.db = db
        self.session_mgr = session_mgr
        logging.info("ClassProtocolFactory initialized.")

    def buildProtocol(self, addr):
        logging.info(f"Building protocol for address: {addr}")
        proto = self.protocol(self.db, self.session_mgr)
        return proto

