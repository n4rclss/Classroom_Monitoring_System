# /home/ubuntu/server_modular/servers/features/create_room_handler.py

from database.sqlite_db import ClassroomDatabase
from utils.packets import PacketCreateRoom  # Make sure this packet is defined

async def handle_create_room(db: ClassroomDatabase, client_id: str, create_room_packet: PacketCreateRoom) -> tuple[str, str]:
    """Handles the logic for room creation by a teacher.

    Args:
        db: The ClassroomDatabase instance.
        client_id: The unique client identifier.
        create_room_packet: The validated create room packet.

    Returns:
        A tuple of (status, message).
    """

    try:
        username = create_room_packet.teacher
        room_id = create_room_packet.room_id

        # Step 1: Ensure user is logged in
        if not db.get_active_session(username):
            print(f"[!] Room creation denied for {username} (Client {client_id}): not logged in.")
            return "error", "User is not logged in"

      
        # Step 2: Try creating the room
        if db.create_room(room_id, username):
            print(f"[*] Room '{room_id}' created by teacher '{username}' (Client {client_id})")
            return "success", "Room created successfully"
        else:
            print(f"[!] Failed to create room '{room_id}' by '{username}' (Client {client_id}) — Room may already exist.")
            return "error", "Room already exists or error occurred"

    except Exception as e:
        print(f"[!] Unexpected error in room creation by '{create_room_packet.teacher}' (Client {client_id}): {e}")
        return "error", "Internal server error during room creation"
