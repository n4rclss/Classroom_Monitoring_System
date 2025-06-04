# server/protocols/class_protocol.py
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
            logging.error(f"Protocol error: {str(e)}")
            self.send_error(str(e))
            
    def handle_create_room(self, message):
        if not self.db.create_room(message.room_id,message.teacher):
            raise ValueError("Room creation failed")
        self.send_success(f"Room {message.room_id} created successfully")
        return "Room created successfully"
        

    def handle_login(self, message):
        if not self.db.authenticate(message.username, message.password, message.role):
            # Disconnect if authentication fails
            # logging.warning(f"Failed login attempt for user: {message.username}")
            # self.transport.loseConnection()
            raise ValueError("Invalid credentials")
        self.session_mgr.register_user(message.username, self)
        print(f"User {message.username} logged in successfully")
        self.send_success("login")

    def handle_join_room(self, data):
        """Fixed join room handler with proper synchronization"""
        join_result = self.db.join_room(self.username, data.room_id, data.student_name, data.mssv)
        print("handling join room")
        print("Join room result: ",join_result)
        if "successfully" in join_result:  # Check if join was successful
            self.current_room = data.room_id
            
            # CRITICAL: Sync with SessionManager
            self.session_mgr.join_room(self.username, data.room_id)
            
            # Broadcast room update to all participants
           # self.session_mgr.broadcast_room_update(data.room_id)
            
            self.send_success(f"Joined room {data.room_id} successfully!")
           
        else:
            self.send_error("Join failed: " + join_result)
            
    def handle_refresh(self, message):
        """Handle refresh request for room participants"""
        if not message.room_id or message.room_id not in self.db.rooms:
            self.send_error("Invalid room ID")
            return
        
        students = self.db.rooms[message.room_id]["students"]

        participants = []
        for username, info in students.items():
            participants.append({
                "username": username,
                "student_name": info["student_name"],
                "mssv": info["mssv"]
            })
            
        print("Handle refresh for room:", message.room_id)
        print("participants are: ",participants)
        
        self.sendLine(json.dumps({
            "status": "success",
            "message": "Participants fetched successfully",
            "participants": participants
        }).encode())

    
    def handle_send_message_to_all(self, message):
        room_id = message.room_id
        content = message.message_to_all

        if room_id not in self.db.rooms:
            self.send_error("Room does not exist")
            return

        # Get all students in the room
        students = self.db.rooms[room_id]["students"]

        # Message to send to all
        data = {
            "type" : "send_message_to_all",
            "sender": self.username,
            "content": content,
        }
        encoded_data = json.dumps(data).encode()

        # Send to all connected users in the room
        for username in students:
            conn = self.session_mgr.get_connection(username)
            if conn:
                conn.sendLine(encoded_data)

        print(f"Teacher sends to all successfully: {content}")
        self.send_success("Message sent to all participants")

        
        

    def connectionLost(self, reason):
        """Improved cleanup"""
        peer = self.transport.getPeer()
        logging.info(f"Disconnecting {self.username} from {peer.host}:{peer.port}")
        
        # Clean up user session and room memberships
        if self.username:
            self.session_mgr.remove_connection(self.username)
            
            # Remove from database room tracking
            if self.current_room and self.current_room in self.db.rooms:
                self.db.rooms[self.current_room]["students"].discard(self.username)
        
        logging.info(f"Connection lost: {peer} - Reason: {reason}")


    def handle_chat_message(self, message):
        self.session_mgr.broadcast_message(
            message.receiver_id,
            message.model_dump_json()
        )
    

    def send_success(self, action: str):
        self.sendLine(f'{{"status": "success", "message": "{action}"}}'.encode())

    def send_error(self, message: str):
        self.sendLine(f'{{"status": "error", "message": "{message}"}}'.encode())

class ClassProtocolFactory(Factory):
    def __init__(self, db, session_mgr):
        self.db = db
        self.session_mgr = session_mgr

    def buildProtocol(self, addr):
        return ClassProtocol(self.db, self.session_mgr)
