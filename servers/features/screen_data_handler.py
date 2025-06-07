# /home/ubuntu/servers_code/servers/features/screen_data_handler.py

from pydantic import ValidationError
from utils.packets import PacketScreenData
from database.sqlite_db import ClassroomDatabase
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from main import NotificationSender


async def handle_screen_data(
    db: ClassroomDatabase,
    sender_func: 'NotificationSender',
    client_id: str,
    screen_data_packet: PacketScreenData
) -> tuple[str, str]:
    """
    Handles a screen_data packet by forwarding it to the teacher in the same room.
    """
    try:
        teacher_client_id = screen_data_packet.sender_client_id
        if not teacher_client_id:
            print(f"[!] No teacher found in the same room as student '{client_id}'")
            return "error", "No teacher found in your room."

        forward_payload = {
            "type": "screen_data",
            "image_data": screen_data_packet.image_data
        }

        print(f"[*] Forwarding screen data from student '{client_id}' to teacher '{teacher_client_id}'")
        await sender_func(teacher_client_id, forward_payload)

        print("[*] Screen data sent successfully.")
        return "success", "Screen data forwarded to teacher."

    except ValidationError as ve:
        print(f"[!] Validation error from {client_id}: {ve}")
        return "error", "Invalid screen data packet."

    except Exception as e:
        print(f"[!] Error forwarding screen data from {client_id}: {type(e).__name__} - {e}")
        return "error", "Internal error while forwarding screen data."
