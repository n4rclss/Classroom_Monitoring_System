# /home/ubuntu/server_modular/servers/features/login_handler.py
import json
from pydantic import ValidationError
from database.sqlite_db import ClassroomDatabase
from utils.packets import PacketLogin

async def handle_login(db: ClassroomDatabase, client_id: str, login_packet: PacketLogin) -> tuple[str, str]:
    """Handles the login authentication logic.

    Args:
        db: The database instance.
        client_id: The client's unique identifier.
        login_packet: The validated login packet data.

    Returns:
        A tuple containing the status ("success" or "error") and a message.
    """
    try:
        is_authenticated = db.authenticate(
            login_packet.username,
            login_packet.password,
            login_packet.role
        )

        if is_authenticated:
            print(f"[*] Login SUCCESS for user {login_packet.username} (Client ID: {client_id})")
            return "success", "Login successful"
        else:
            print(f"[*] Login FAILED for user {login_packet.username} (Client ID: {client_id})")
            return "error", "Invalid credentials or role"

    except Exception as e:
        print(f"[!] Unexpected error during authentication for client {client_id}: {e}")
        return "error", "Internal server error during authentication"

