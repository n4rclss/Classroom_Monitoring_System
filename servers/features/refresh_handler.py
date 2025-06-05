# /home/ubuntu/server_modular/servers/features/refresh_handler.py
from database.sqlite_db import ClassroomDatabase
from utils.packets import PacketRefresh

async def handle_refresh(db: ClassroomDatabase, client_id: str, refresh_packet: PacketRefresh) -> tuple[str, dict]:
    """Handles refreshing the list of participants in a given room.

    Args:
        db: The database instance.
        client_id: The client's unique identifier.
        refresh_packet: The validated refresh packet containing the room_id.

    Returns:
        A tuple of ("success"/"error", data or message)
    """
    try:
        participants = db.get_room_participants(refresh_packet.room_id)
        if participants is None:
            print(f"[!] Refresh FAILED: Room '{refresh_packet.room_id}' not found (Client ID: {client_id})")
            return "error", {"message": f"Room '{refresh_packet.room_id}' not found."}

        print(f"[*] Refreshed room '{refresh_packet.room_id}' for client {client_id}. Found {len(participants)} participants.")
        return "success", {"participants": participants}

    except Exception as e:
        print(f"[!] Unexpected error during refresh for client {client_id}: {e}")
        return "error", {"message": "Internal server error during refresh"}
