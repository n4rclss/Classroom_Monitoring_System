
# /home/ubuntu/servers_code/servers/features/streaming_handler.py
import json
from typing import TYPE_CHECKING, Callable, Awaitable

from database.sqlite_db import ClassroomDatabase
from utils.packets import PacketStreaming

# Type hint for the sender function passed from main.py
if TYPE_CHECKING:
    from main import NotificationSender # Reuse the sender type hint

async def handle_streaming_request(
    db: ClassroomDatabase, 
    sender_func: 'NotificationSender', 
    sender_client_id: str, 
    streaming_packet: PacketStreaming
) -> tuple[str, str]:
  
    target_username = streaming_packet.target_username
    print(f"[*] Handling streaming request from client 	'{sender_client_id}' targeting user 	'{target_username}'.")

    try:
        # 1. Find the target user's current client_id from the database
        target_client_id = db.get_client_id(target_username)

        if not target_client_id:
            print(f"[!] Target user 	'{target_username}' not found or is offline (no active client_id in DB).")
            return "error", f"User '{target_username}' is currently offline or does not exist."
            
        # Ensure initiator is not targeting themselves (optional, but good practice)
        if target_client_id == sender_client_id:
            print(f"[!] User 	'{sender_client_id}' attempted to stream from themselves.")
            return "error", "You cannot initiate a stream from yourself."

        # 2. Prepare the payload to send to the *target* user
        # This payload tells the target client to start streaming and where to send the data
        start_streaming_payload = {
            "type": "start_streaming", # Command for the target client
            "sender_client_id": sender_client_id # ID for the target to send data back to
            # Optionally add sender_username if needed by target client
            # "initiator_username": db.get_username(sender_client_id) 
        }
        start_streaming_payload_json = json.dumps(start_streaming_payload)
        print(f"[*] Prepared 'start_streaming' command for target 	'{target_username}' (ClientID: {target_client_id}): {start_streaming_payload_json}")

        # 3. Send the command to the target user via the sender function
        try:
            await sender_func(target_client_id, start_streaming_payload)
            print(f"[*] Successfully sent 'start_streaming' command to target 	'{target_username}' ({target_client_id}).")
        except Exception as e:
            # Catch errors during the async send operation
            error_msg = f"Failed to send 'start_streaming' command to target user 	'{target_username}' (Client ID: {target_client_id}): {type(e).__name__} - {e}"
            print(f"[!] {error_msg}")
            # Inform the initiator that the request failed
            return "error", f"Failed to send streaming request to '{target_username}'. They might have just disconnected."

        # 4. Return success message to the *initiator*
        success_message = f"Streaming request successfully sent to user '{target_username}'. Waiting for them to accept and send data."
        print(f"[*] {success_message}")
        return "success", success_message

    except Exception as e:
        # Catch unexpected errors in the overall handler logic
        print(f"[!] Critical error in handle_streaming_request for target 	'{target_username}': {type(e).__name__} - {e}")
        import traceback
        traceback.print_exc()
        return "error", f"Internal server error while processing streaming request for '{target_username}'."


