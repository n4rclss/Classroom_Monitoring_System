import json
from typing import TYPE_CHECKING, Callable, Awaitable
from database.sqlite_db import ClassroomDatabase
from utils.packets import PacketRequestApp, PacketReturnApp

'''
request payload
{
    "type": "request_app",
    "target_username": "student_username"
}
'''

async def handle_app_request(
    db: ClassroomDatabase, 
    sender_func,
    sender_client_id: str, 
    packet_request_app: PacketRequestApp
) -> tuple[str, str]:
  
    target_username = packet_request_app.target_username
    print(f"[*] Handling running application request from client '{sender_client_id}' targeting user '{target_username}'.")

    try:
        target_client_id = db.get_client_id(target_username)

        if not target_client_id:
            print(f"[!] Target user '{target_username}' not found or is offline (no active client_id in DB).")
            return "error", f"User '{target_username}' is currently offline or does not exist."

        if target_client_id == sender_client_id:
            print(f"[!] User 	'{sender_client_id}' attempted to send request to themselves.")
            return "error", "You cannot send to yourself."

        request_app_payload = {
            "type": "request_app", # Command for the target client
            "sender_client_id": sender_client_id # ID for the target to send data back to
        }
        
        request_app_payload_json = json.dumps(request_app_payload)
        print(f"[*] Prepared 'request_app' command for target 	'{target_username}' (ClientID: {target_client_id}): {request_app_payload_json}")

        try:
            await sender_func(target_client_id, request_app_payload)
            print(f"[*] Successfully sent 'request_app' command to target 	'{target_username}' ({target_client_id}).")
        except Exception as e:
            error_msg = f"Failed to send 'request_app' command to target user '{target_username}' (Client ID: {target_client_id}): {type(e).__name__} - {e}"
            print(f"[!] {error_msg}")
            # Inform the initiator that the request failed
            return "error", f"Failed to send 'request_app' to {target_username}"

        success_message = f"Running application request successfully sent to user '{target_username}'. Waiting for them to accept and send data."
        print(f"[*] {success_message}")
        return "success", success_message

    except Exception as e:
        print(f"[!] Critical error while handling for request_app to '{target_username}': {type(e).__name__} - {e}")
        import traceback
        traceback.print_exc()
        return "error", f"Internal server error while processing request for '{target_username}'."

'''
retrun packet
{
    "type": "return_app",
    "target_client_id": "id of teacher",
    "app_data": ["
        {
            "process_name": "Zalo",
            "main_window_title": "Zalo",
        },
        {
            "process_name": "Google Chrome",
            "main_window_title": "Google Chrome",
        }"]
}
'''

async def handle_app_return(
    db: ClassroomDatabase,
    sender_func,
    client_id: str,
    packet_return_app: PacketReturnApp
) -> tuple[str, str]:
    try: 
        target_client_id = packet_return_app.sender_client_id

        if target_client_id == client_id:
            print(f"[!] User '{client_id}' attempted to send request to themselves.")
            return "error", "You cannot send to yourself."
        
        try:
            app_data = [app.model_dump() for app in packet_return_app.app_data]
        except Exception as dump_error:
            raise dump_error

        payload = {
            "type": "return_app",
            "app_data": app_data
        }


        print(f"[*] Handling return application data from client '{target_client_id}'.")

        try:
            await sender_func(target_client_id, payload)
            print(f"[*] Successfully sent application data back to client '{target_client_id}'.")
        
        except Exception as e:
            print(f"[!] Failed to send application data back to client '{target_client_id}': {type(e).__name__} - {e}")
            raise e  
        
        print(f"[*] Application data successfully sent to client '{target_client_id}': {payload}")
        return "success", f"Application data successfully sent to client '{target_client_id}'."
    except Exception as e:
        print(f"[!] Error handling return_app from client '{packet_return_app.sender_client_id}': {type(e).__name__} - {e}")
        return "error", f"Internal error while processing application data from client '{packet_return_app.sender_client_id}'."
