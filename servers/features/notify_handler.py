
# /home/ubuntu/servers_code/servers/features/notify_handler.py
import json
from typing import TYPE_CHECKING, Callable, Awaitable

# Ensure the path includes the project root if needed, though direct imports should work
# import sys
# import os
# sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from database.sqlite_db import ClassroomDatabase
from utils.packets import PacketNotify

# Type hint for the sender function passed from main.py
if TYPE_CHECKING:
    from main import NotificationSender # Assuming NotificationSender type is defined in main

async def handle_notify(
    db: ClassroomDatabase, 
    sender_func: 'NotificationSender', 
    sender_client_id: str, 
    notify_packet: PacketNotify
) -> tuple[str, str]:
    """
    Handles sending a notification from a user (teacher) to all users (teacher + students)
    associated with the specified room, using database lookups for client IDs.

    Args:
        db: The database instance (provides methods like get_room_teacher, 
            get_students_in_room, get_client_id, get_username).
        sender_func: The async function (bound to a writer) to send notifications via the LB.
        sender_client_id: The client_id of the user initiating the notification.
        notify_packet: The validated notify packet containing room_id and message.

    Returns:
        A tuple of status ('success' or 'error') and a descriptive message for the *sender*.
    """
    room_id = notify_packet.room_id
    notification_message = notify_packet.noti_message

    print(f"[*] Handling notification request for room '{room_id}' from client '{sender_client_id}'")

    try:
        # 1. Get the teacher of the room from the database
        # Uses the synchronous db method
        teacher_username = db.get_room_teacher(room_id)
        if not teacher_username:
            print(f"[!] Room '{room_id}' not found or has no teacher in database during notification.")
            return "error", f"Room '{room_id}' does not exist or is invalid."
        
        # 2. Verify sender is the teacher using database lookup
        # Uses the synchronous db method
        sender_username = db.get_username(sender_client_id)
        if not sender_username:
             # This case should ideally not happen if the client_id is valid and registered
             print(f"[!] Could not find username for sender client_id '{sender_client_id}' in DB.")
             return "error", "Sender identity could not be verified."
             
        if sender_username != teacher_username:
             print(f"[!] Unauthorized notification attempt by non-teacher '{sender_username}' (Client ID: {sender_client_id}) in room '{room_id}'. Expected teacher: '{teacher_username}'.")
             return "error", "Only the teacher can send notifications in this room."
        else:
            print(f"[*] Notification sender '{sender_username}' confirmed as teacher for room '{room_id}'.")

        # 3. Get all student usernames currently listed in the room from the database
        # Uses the synchronous db method
        student_usernames = db.get_students_in_room(room_id)
        print(f"[*] Students found in room '{room_id}': {student_usernames}")

        # 4. Combine all intended recipients (teacher + students)
        recipient_usernames = set(student_usernames)
        
        
        if not recipient_usernames:
            # Should not happen if teacher exists, but check anyway
            print(f"[*] No recipients found for notification in room '{room_id}'.")
            return "success", f"No users currently in room '{room_id}' to notify."

        # 5. Prepare the notification payload
        notify_payload = {
            "type": "notification",
            "room_id": room_id,
            "message": notification_message,
            "sender_username": teacher_username # Include sender info
        }
        notify_payload_json_str = json.dumps(notify_payload)
        print(f"[*] Prepared notification payload for room '{room_id}': {notify_payload_json_str}")

        # 6. Attempt to send the notification to each recipient
        sent_count = 0
        send_errors = []
        offline_users = []
        print(f"[*] Attempting to send notification to recipients in room '{room_id}': {list(recipient_usernames)}")

        for username in recipient_usernames:
            # Look up the target client_id from the database
            # Uses the synchronous db method
            target_client_id = db.get_client_id(username)
            
            if target_client_id:
                try:
                    print(f"[*] Found active client for '{username}': {target_client_id}. Sending notification via sender_func...")
                    # Use the async sender_func passed from main.py
                    await sender_func(target_client_id, notify_payload)
                    sent_count += 1
                    print(f"[*] Successfully initiated send for notification to '{username}' ({target_client_id}).")
                except Exception as e:
                    # Catch errors during the async send operation
                    error_msg = f"Failed to send notification to user '{username}' (Client ID: {target_client_id}): {type(e).__name__} - {e}"
                    print(f"[!] {error_msg}")
                    send_errors.append(error_msg)
                    # Remove from DB if send fails? Or rely on disconnect cleanup?
                    # db.unregister_client(username) # Be careful with this
            else:
                # User is in the room list but has no active client_id in the DB
                print(f"[!] Could not find active client_id for user '{username}' in room '{room_id}' via DB. User is likely offline.")
                offline_users.append(username)

        # 7. Compile and return the final status message to the *sender*
        total_recipients = len(recipient_usernames)
        result_message = f"Notification processed for room '{room_id}'. Attempted send to {sent_count}/{total_recipients - len(offline_users)} online recipients."
        if offline_users:
            result_message += f" ({len(offline_users)} users offline: {', '.join(offline_users)})"
        
        if send_errors:
            error_summary = "; ".join(send_errors)
            print(f"[!] Errors occurred during notification broadcast for room '{room_id}': {error_summary}")
            # Return 'error' to the sender if any individual send failed
            return "error", f"{result_message} Errors: {error_summary}"
        else:
            print(f"[*] {result_message}")
            return "success", result_message

    except Exception as e:
        # Catch unexpected errors in the overall handler logic
        print(f"[!] Critical error in handle_notify for room '{room_id}': {type(e).__name__} - {e}")
        import traceback
        traceback.print_exc()
        return "error", f"Internal server error while processing notification for room '{room_id}'."
