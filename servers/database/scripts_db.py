from sqlite_db import ClassroomDatabase
import sqlite3
import argparse
import sys
import os
import bcrypt

def initialize_sample_data(db):
    """Initialize database with sample test data"""
    import bcrypt
    sample_users = [
        ("teacher", "t", "teacher"),
        ("stu1", "s", "student"),
        ("stu2", "ss", "student"),
        ("stu3", "sss", "student"),
    ]

    hashed_users = [
        (username, bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt()).decode('utf-8'), role)
        for username, password, role in sample_users
    ]

    with db._get_cursor() as cursor:
        cursor.executemany(
            """
            INSERT OR IGNORE INTO users (username, password, role)
            VALUES (?, ?, ?)
        """,
            hashed_users,
        )
    print("Added sample users: teacher, stu1, stu2, stu3")
    
def clear_all_data(db):
    """Clear all data from the database"""
    with db._get_cursor() as cursor:
        # Order matters due to foreign keys if not using CASCADE (but we are)
        cursor.execute("DELETE FROM room_participants")
        cursor.execute("DELETE FROM rooms")
        cursor.execute("DELETE FROM active_sessions")
        cursor.execute("DELETE FROM users")
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
        # Corrected f-string usage (already correct here)
        print(f"{'Username':<20} {'Role':<10}")
        print("-" * 30)
        for user in users:
            # Corrected f-string usage with single quotes for keys
            print(f"{user['username']:<20} {user['role']:<10}")


def add_user(db, username, password, role):
    """Add a new user to the database"""
    if role not in ("teacher", "student"):
        print("Error: Role must be either 'teacher' or 'student'")
        return

    try:
        # Hash the password using bcrypt
        hashed_pw = bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt())
        with db._get_cursor() as cursor:
            cursor.execute(
                """
                INSERT INTO users (username, password, role)
                VALUES (?, ?, ?)
            """,
                (username, hashed_pw.decode('utf-8'), role),
            )
        print(f"Successfully added user: {username} ({role})")
    except sqlite3.IntegrityError:
        print(f"Error: User '{username}' already exists")
    except sqlite3.Error as e:
        print(f"Database error adding user: {e}")
# ...existing code...


def delete_user(db, username):
    """Delete a user from the database (cascades to rooms/participants)"""
    try:
        with db._get_cursor() as cursor:
            # Ensure user exists before attempting delete for better feedback
            cursor.execute("SELECT 1 FROM users WHERE username = ?", (username,))
            if not cursor.fetchone():
                # Corrected f-string usage with single quotes around variable
                print(f"Error: User '{username}' not found")
                return

            cursor.execute("DELETE FROM users WHERE username = ?", (username,))
            # Cascade should handle related deletions in rooms, room_participants, active_sessions
            print(f"Successfully deleted user: {username} (and related data via cascade)")

    except sqlite3.Error as e:
        print(f"Database error deleting user: {e}")

def list_rooms(db):
    """List all rooms and their participants from the database"""
    with db._get_cursor() as cursor:
        # Get all rooms
        cursor.execute(
            """
            SELECT r.room_id, r.teacher, r.created_at,
                   COUNT(rp.student_username) as student_count
            FROM rooms r
            LEFT JOIN room_participants rp ON r.room_id = rp.room_id
            GROUP BY r.room_id
            ORDER BY r.created_at
        """
        )
        rooms = cursor.fetchall()

        if not rooms:
            print("No rooms found in database")
            return

        print("\nRooms in database:")
        for room in rooms:
            # Corrected f-string usage with single quotes for keys
            print(f"\n--- Room ID: {room['room_id']} ---")
            print(f"    Teacher: {room['teacher']}")
            print(f"    Created: {room['created_at']}")
            print(f"    Student Count: {room['student_count']}")

            # Get student details for this room directly using DB method
            # Corrected f-string usage with single quotes for key
            participants = db.get_room_participants(room['room_id'])

            if participants:
                print("    Participants:")
                for student in participants:
                    # Corrected f-string with single quotes for keys
                    print(
                        f"      - {student['student_name']} "
                        f"({student['student_username']}, MSSV: {student['mssv']})"
                    )
            else:
                print("    Participants: None")

def test_room_operations(db):
    """Perform a sequence of room operations for testing."""
    print("\n--- Testing Room Operations ---")

    # 0. Ensure sample users exist
    initialize_sample_data(db)

    # 1. Create a room
    room_id = "test_room_101"
    teacher = "teacher"
    # Corrected f-string usage with escaped quotes
    print(f"\nAttempting to create room \'{room_id}\' by \'{teacher}\'...")
    if db.create_room(room_id, teacher):
        print("Room created successfully.")
    else:
        print("Room creation failed (maybe already exists?).")

    # 2. List rooms after creation
    list_rooms(db)

    # 3. Join students to the room
    # Corrected f-string usage with escaped quotes
    print(f"\nAttempting to join students to room \'{room_id}\'...")
    join_results = []
    join_results.append(db.join_room(room_id, "stu1", "Student One", "MSSV001"))
    join_results.append(db.join_room(room_id, "stu2", "Student Two", "MSSV002"))
    join_results.append(db.join_room(room_id, "stu1", "Student One", "MSSV001")) # Try joining again
    print(f"Join results (stu1, stu2, stu1 again): {join_results}")

    # 4. List rooms after joining
    list_rooms(db)

    # 5. Student leaves room
    # Corrected f-string usage with escaped quotes
    print(f"\nAttempting to make \'stu1\' leave room \'{room_id}\'...")
    if db.leave_room(room_id, "stu1"):
        print("stu1 left successfully.")
    else:
        print("Failed to remove stu1 (maybe not in room?).")

    # 6. List rooms after leaving
    list_rooms(db)

    # 7. Delete a user (stu2) - should cascade remove participation
    # Corrected f-string usage with escaped quotes
    print(f"\nAttempting to delete user \'stu2\'...")
    delete_user(db, "stu2")

    # 8. List rooms after user deletion
    list_rooms(db)

    # 9. Delete the room - should cascade remove remaining participants (if any)
    # Corrected f-string usage with escaped quotes
    print(f"\nAttempting to delete room \'{room_id}\'...")
    if db.delete_room(room_id):
        print("Room deleted successfully.")
    else:
        print("Failed to delete room (maybe already deleted?).")

    # 10. List rooms after room deletion
    list_rooms(db)

    print("\n--- Room Operations Test Complete ---")

def main():
    parser = argparse.ArgumentParser(
        description="Classroom Database Management Utility",
        formatter_class=argparse.RawTextHelpFormatter,
    )
    script_dir = os.path.dirname(os.path.abspath(__file__))
    default_db_path = os.path.join(script_dir, "classroom.db")

    # --- Command Definitions ---
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--init", action="store_true", help="Initialize with sample data")
    group.add_argument("--clear", action="store_true", help="Clear all data from database")
    group.add_argument("--list-users", action="store_true", help="List all users")
    group.add_argument("--list-rooms", action="store_true", help="List all rooms and participants")
    group.add_argument(
        "--add-user",
        nargs=3,
        metavar=("USERNAME", "PASSWORD", "ROLE"),
        help="Add a new user (role must be teacher or student)",
    )
    group.add_argument(
        "--delete-user", metavar="USERNAME", help="Delete a user from the database"
    )
    group.add_argument(
        "--test-rooms", action="store_true", help="Run a sequence of room operations tests"
    )

    # --- Optional Arguments ---
    parser.add_argument(
        "--db-path",
        default=default_db_path,
        type=str,
        help=f"Path to SQLite database file (default: {default_db_path})",
    )

    args = parser.parse_args()
    print(f"Using database at: {args.db_path}")
    
    db_dir = os.path.dirname(args.db_path)
    if db_dir and not os.path.exists(db_dir):
        try:
            os.makedirs(db_dir)
            print(f"Created directory for database: {db_dir}")
        except OSError as e:
            print(f"Error creating directory {db_dir}: {e}", file=sys.stderr)
            sys.exit(1)

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
        elif args.test_rooms:
            test_room_operations(db)

    except Exception as e:
        print(f"An error occurred: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()

