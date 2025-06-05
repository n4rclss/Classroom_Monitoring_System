# /home/ubuntu/server_modular/servers/features/joinroom_handler.py
from database.sqlite_db import ClassroomDatabase
from utils.packets import PacketJoinRoom

async def handle_joinroom(db: ClassroomDatabase, client_id: str, join_packet: PacketJoinRoom) -> tuple[str, str]:
    """Handles a student joining a room using the database's join_room method.

    Args:
        db: The database instance.
        client_id: The client's unique identifier.
        join_packet: The validated join room packet data.

    Returns:
        A tuple containing the status ("success" or "error") and a message.
    """
    try:
        success = db.join_room(
            room_id=join_packet.room_id,
            student_username=join_packet.username,
            student_name=join_packet.student_name,
            mssv=join_packet.mssv
        )

        if success:
            print(f"[*] Student '{join_packet.username}' joined room '{join_packet.room_id}' successfully (Client ID: {client_id})")
            return "success", f"Student '{join_packet.student_name}' joined room '{join_packet.room_id}'."
        else:
            print(f"[!] Join FAILED for student '{join_packet.username}' in room '{join_packet.room_id}' (Client ID: {client_id})")
            return "error", "Failed to join room. Check if the room and user exist."

    except Exception as e:
        print(f"[!] Unexpected error during join_room for client {client_id}: {e}")
        return "error", "Internal server error during join_room"
