# server/main.py
from twisted.internet import reactor
from database.in_memory_db import ClassroomDatabase
from services.session_manager import SessionManager
from protocols.protocol import ClassProtocolFactory
from utils.logger import setup_logger
import argparse

def main():
    # Setup command-line argument parser
    parser = argparse.ArgumentParser(description='Classroom Monitoring System Server')
    parser.add_argument('-ip', '--ip_address', type=str, default='0.0.0.0',
                        help='IP address to bind the server to (default: 0.0.0.0)')
    parser.add_argument('-p', '--port', type=int, default=8000,
                        help='Port to run the server on (default: 8000)')
    
    args = parser.parse_args()
    
    setup_logger(args.ip_address, args.port)
    # Initialize core components
    db = ClassroomDatabase()
    session_manager = SessionManager(db)
    factory = ClassProtocolFactory(db, session_manager)

    # Start TCP server
    reactor.listenTCP(args.port, factory, interface=args.ip_address)
    print(f"Server started on {args.ip_address}:{args.port}")
    reactor.run()

if __name__ == "__main__":
    main()
