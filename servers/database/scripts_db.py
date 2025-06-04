from sqlite_db import ClassroomDatabase
import sqlite3
import argparse
import sys
import os

def initialize_sample_data(db):
    """Initialize database with sample test data"""
    sample_users = [
        ('teacher', 't', 'teacher'),
        ('stu1', 's', 'student'),
        ('stu2', 'ss', 'student')
    ]
    
    with db._get_cursor() as cursor:
        cursor.executemany('''
            INSERT OR IGNORE INTO users (username, password, role)
            VALUES (?, ?, ?)
        ''', sample_users)
    print("Added sample users: teacher, stu1, stu2")

def clear_all_data(db):
    """Clear all data from the database"""
    with db._get_cursor() as cursor:
        cursor.execute("DELETE FROM users")
        cursor.execute("DELETE FROM rooms")
        cursor.execute("DELETE FROM room_participants")
        cursor.execute("DELETE FROM active_sessions")
        cursor.execute("DELETE FROM chat_history")
    print("Cleared all database tables")

def list_users(db):
    """List all users in the database"""
    with db._get_cursor() as cursor:
        cursor.execute("SELECT username, role FROM users ORDER BY role, username")
        users = cursor.fetchall()
        
        if not users:
            print("No users found in database")
            return
            
        print("\nUsers in database:")
        print(f"{'Username':<20} {'Role':<10}")
        print("-" * 25)
        for user in users:
            print(f"{user['username']:<20} {user['role']:<10}")

def add_user(db, username, password, role):
    """Add a new user to the database"""
    if role not in ('teacher', 'student'):
        print("Error: Role must be either 'teacher' or 'student'")
        return
        
    try:
        with db._get_cursor() as cursor:
            cursor.execute('''
                INSERT INTO users (username, password, role)
                VALUES (?, ?, ?)
            ''', (username, password, role))
        print(f"Successfully added user: {username} ({role})")
    except sqlite3.IntegrityError:
        print(f"Error: User '{username}' already exists")

def delete_user(db, username):
    """Delete a user from the database"""
    try:
        with db._get_cursor() as cursor:
            cursor.execute("DELETE FROM users WHERE username = ?", (username,))
            if cursor.rowcount == 0:
                print(f"Error: User '{username}' not found")
            else:
                print(f"Successfully deleted user: {username}")
    except sqlite3.Error as e:
        print(f"Error deleting user: {e}")

def list_rooms(db):
    """List all rooms and their participants"""
    with db._get_cursor() as cursor:
        # Get all rooms
        cursor.execute("""
            SELECT r.room_id, r.teacher, 
                   COUNT(rp.username) as student_count
            FROM rooms r
            LEFT JOIN room_participants rp ON r.room_id = rp.room_id
            GROUP BY r.room_id
            ORDER BY r.room_id
        """)
        rooms = cursor.fetchall()
        
        if not rooms:
            print("No rooms found in database")
            return
            
        print("\nRooms in database:")
        for room in rooms:
            print(f"\nRoom ID: {room['room_id']}")
            print(f"Teacher: {room['teacher']}")
            print(f"Students: {room['student_count']}")
            
            # Get student details for this room
            cursor.execute("""
                SELECT username, student_name, mssv
                FROM room_participants
                WHERE room_id = ?
                ORDER BY student_name
            """, (room['room_id'],))
            students = cursor.fetchall()
            
            if students:
                print("\nStudents:")
                for student in students:
                    print(f"  - {student['student_name']} ({student['username']}, MSSV: {student['mssv']})")

def main():
    parser = argparse.ArgumentParser(
        description='Classroom Database Management Utility',
        formatter_class=argparse.RawTextHelpFormatter
    )
    script_dir = os.path.dirname(os.path.abspath(__file__))
    default_db_path = os.path.join(script_dir, 'classroom.db')
    
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('--init', action='store_true', help='Initialize with sample data')
    group.add_argument('--clear', action='store_true', help='Clear all data from database')
    group.add_argument('--list-users', action='store_true', help='List all users')
    group.add_argument('--list-rooms', action='store_true', help='List all rooms and participants')
    group.add_argument('--add-user', nargs=3, metavar=('USERNAME', 'PASSWORD', 'ROLE'), 
                      help='Add a new user (role must be teacher or student)')
    group.add_argument('--delete-user', metavar='USERNAME', 
                      help='Delete a user from the database')
    
    # Optional
    parser.add_argument('--db-path', default=default_db_path, type=str,
                       help='Path to SQLite database file (default: classroom.db)')
    
    args = parser.parse_args()
    print(f"Using database at: {args.db_path}")
    db = ClassroomDatabase(args.db_path)
    
    try:
        if args.init:
            initialize_sample_data(db)
        elif args.clear:
            clear_all_data(db)
        elif args.list_users:
            list_users(db)
        elif args.list_rooms:
            list_rooms(db)
        elif args.add_user:
            username, password, role = args.add_user
            add_user(db, username, password, role)
        elif args.delete_user:
            delete_user(db, args.delete_user)
            
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == '__main__':
    main()