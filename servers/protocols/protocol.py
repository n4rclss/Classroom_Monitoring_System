# server/protocols/class_protocol.py
from twisted.internet.protocol import Factory
from twisted.protocols.basic import LineReceiver
from utils.parser import parse_message
import logging
import json

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

    def connectionLost(self, reason):
        peer = self.transport.getPeer()
        logging.info(f"Disconnecting from {peer.host}:{peer.port}")
        self.session_mgr.remove_connection(self.username)
        logging.info(f"Connection lost: {peer} - Reason: {reason}")

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

    def handle_login(self, message):
        if not self.db.authenticate(message.username, message.password, message.role):
            # Disconnect if authentication fails
            # logging.warning(f"Failed login attempt for user: {message.username}")
            # self.transport.loseConnection()
            raise ValueError("Invalid credentials")
        self.session_mgr.register_user(message.username, self)
        self.send_success("login")

    def handle_join(self, data):
        if self.db.join_room(self.username, data["room_id"]):
            self.current_room = data["room_id"]
            self.session_mgr.broadcast_room_update(data["room_id"])
            self.send_success("join")
        else:
            self.send_error("Join failed")

    def handle_chat(self, data):
        self.session_mgr.broadcast_message(
            self.current_room,
            json.dumps({
                "type": "chat",
                "sender": self.username,
                "content": data["content"]
            })
        )

    def handle_join_class(self, message):
        if not self.db.join_room(message.username, message.class_id):
            raise ValueError("Invalid class ID")
        self.current_room = message.class_id
        self.send_success("join_class")

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
