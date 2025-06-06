# /home/ubuntu/server_modular/servers/features/logout_handler.py
from database.sqlite_db import ClassroomDatabase
from utils.packets import PacketLogout

async def handle_logout(db: ClassroomDatabase, client_id: str, logout_packet: PacketLogout) -> tuple[str, str]:
    """Handles teacher logout:
    - Verifies active session
    - Deletes their room
    - Removes from active sessions

    Args:
        db: The database instance.
        client_id: The client's unique identifier.
        logout_packet: The validated logout packet data.

    Returns:
        A tuple containing the status ("success" or "error") and a message.
    """
    username = logout_packet.teacher
    room_id = logout_packet.room_id

    try:
        # Check if user is logged in
        if not db.get_active_session(username):
            print(f"[!] Logout FAILED: No active session for {username} (Client ID: {client_id})")
            return "error", f"User '{username}' is not logged in."

        # Attempt to delete the room
        if db.delete_room(room_id):
            print(f"[*] Room '{room_id}' deleted successfully during logout of '{username}'")
        else:
            print(f"[!] Warning: Failed to delete room '{room_id}' (maybe already deleted?)")

        # Remove active session
        db.remove_active_session(username)
        print(f"[*] Logout SUCCESS for user {username} (Client ID: {client_id})")
        return "success", f"User '{username}' logged out and room '{room_id}' deleted."

    except Exception as e:
        print(f"[!] Unexpected error during logout for client {client_id}: {e}")
        return "error", "Internal server error during logout"
