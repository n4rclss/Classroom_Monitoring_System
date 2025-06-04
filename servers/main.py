from twisted.internet import reactor
from database.sqlite_db import ClassroomDatabase  # Changed import
from services.session_manager import SessionManager
from protocols.protocol import ClassProtocolFactory
from utils.logger import setup_logger
import argparse
import os

script_dir = os.path.dirname(os.path.abspath(__file__))

def main():
    parser = argparse.ArgumentParser(description='Classroom Monitoring System Server')
    parser.add_argument('-ip', '--ip_address', type=str, default='127.0.0.1',
                       help='IP address to bind the server to (default: 127.0.0.1)')
    parser.add_argument('-p', '--port', type=int, default=9001,
                       help='Port to run the server on (default: 9001)')
    parser.add_argument('--db-path', type=str, default='classroom.db',
                       help='Path to SQLite database file')
    
    args = parser.parse_args()
    
    db_path = args.db_path
    if not os.path.isabs(db_path):
        db_path = os.path.join(script_dir, "database", db_path)
    print(f"Using database at: {db_path}")
    
    setup_logger(args.ip_address, args.port)
    db = ClassroomDatabase(db_path)  
    
    session_manager = SessionManager(db)
    factory = ClassProtocolFactory(db, session_manager)

    reactor.listenTCP(args.port, factory, interface=args.ip_address)
    print(f"Server started on {args.ip_address}:{args.port}")
    reactor.run()

if __name__ == "__main__":
    main()