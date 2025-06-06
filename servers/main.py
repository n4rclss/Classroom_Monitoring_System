
import asyncio
import struct
import json
import argparse
import sys
import os
from typing import Dict, Optional, Callable, Awaitable
from pydantic import ValidationError

# Ensure the path includes the project root for imports
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from database.sqlite_db import ClassroomDatabase
from utils.packets import PacketNotify, PacketLogin, PacketCreateRoom, PacketLogout, PacketJoinRoom, PacketRefresh
from utils.logger import setup_logger # Assuming logger setup is desired
from features.login_handler import handle_login
from features.create_room_handler import handle_create_room
from features.logout_handler import handle_logout
from features.joinroom_handler import handle_joinroom
from features.refresh_handler import handle_refresh
from features.notify_handler import handle_notify

# Global database instance
db: Optional[ClassroomDatabase] = None

# --- Packet Handling ---

def create_response_packet(client_id: str, status: str, message: str) -> bytes:
    """Creates a wrapped RESPONSE packet to send back to the originating client via LB."""
    response_payload = json.dumps({
        "status": status,
        "message": message
    }).encode("utf-8")

    client_id_bytes = client_id.encode("utf-8")
    client_id_len = len(client_id_bytes)
    server_response_wrapper_payload = struct.pack("!B", client_id_len) + client_id_bytes + response_payload
    total_msg_len = len(server_response_wrapper_payload)
    wrapped_response = struct.pack("!I", total_msg_len) + server_response_wrapper_payload
    return wrapped_response

def create_notification_packet(target_client_id: str, payload: dict) -> bytes:
    """Creates a wrapped NOTIFICATION packet to PUSH to a target client via LB."""
    notification_payload_bytes = json.dumps(payload).encode("utf-8")

    client_id_bytes = target_client_id.encode("utf-8")
    client_id_len = len(client_id_bytes)
    server_push_wrapper_payload = struct.pack("!B", client_id_len) + client_id_bytes + notification_payload_bytes
    total_msg_len = len(server_push_wrapper_payload)
    wrapped_notification = struct.pack("!I", total_msg_len) + server_push_wrapper_payload
    return wrapped_notification

async def send_notification_to_client(writer: asyncio.StreamWriter, target_client_id: str, payload: dict):
    """Sends a notification payload to a specific client via the Load Balancer."""
    try:
        notification_packet = create_notification_packet(target_client_id, payload)
        # print(f"[*] Sending notification packet ({len(notification_packet)} bytes) to LB for routing to client {target_client_id}")
        writer.write(notification_packet)
        await writer.drain()
        # print(f"[*] Successfully sent notification packet for {target_client_id} to LB.")
    except ConnectionResetError:
        print(f"[!] Connection reset while trying to send notification to LB (targeting {target_client_id}). LB might be down.")
        raise
    except Exception as e:
        print(f"[!] Error sending notification packet for {target_client_id} to LB: {type(e).__name__} - {e}")
        raise

# Type hint for the sender function to be passed to handlers
NotificationSender = Callable[[str, dict], Awaitable[None]]

async def handle_connection(reader: asyncio.StreamReader, writer: asyncio.StreamWriter):
    """Handles a single connection from the load balancer."""
    peername = writer.get_extra_info("peername")
    print(f"[*] Accepted connection from {peername}")
    # Track the last known client_id associated with this specific connection for cleanup
    client_id_on_this_connection: Optional[str] = None 

    # Create a sender function bound to the current writer for this connection
    async def sender_func(target_client_id: str, payload: dict):
        # Ensure writer is still valid before sending
        if writer.is_closing():
            print(f"[!] Attempted to send notification via closed writer (targeting {target_client_id}).")
            return # Or raise an exception
        await send_notification_to_client(writer, target_client_id, payload)

    try:
        while True:
            # 1. Read the total message length (prepended by LB)
            len_data = await reader.readexactly(4)
            total_msg_len = struct.unpack("!I", len_data)[0]

            # 2. Read the rest of the wrapped message
            wrapped_message_data = await reader.readexactly(total_msg_len)

            # 3. Parse the wrapped message to get ClientID and original data
            client_id_len = struct.unpack("!B", wrapped_message_data[:1])[0]
            client_id_end = 1 + client_id_len
            client_id = wrapped_message_data[1:client_id_end].decode("utf-8")
            original_client_data = wrapped_message_data[client_id_end:]
            
            # Update the client_id associated with this connection
            client_id_on_this_connection = client_id

            print(f"[*] Received data from LB for client 	'{client_id}': {original_client_data.decode('utf-8', errors='ignore')}")

            # 4. Process the original client data
            response_packet = b"" # For sending RESPONSE back to originating client
            try:
                request_json = original_client_data.decode("utf-8")
                request_data = json.loads(request_json)
                request_type = request_data.get("type")

                # --- Request Handling (Using DB for client mapping) --- #

                if request_type == "login":
                    try:
                        login_packet = PacketLogin.model_validate(request_data)
                        # handle_login performs authentication against DB users table
                        status, message = await handle_login(db, client_id, login_packet)
                        if status == "success":
                            # Register the client mapping in the DB upon successful login
                            # This now uses the database method
                            if not db.register_client(login_packet.username, client_id):
                                # Handle potential DB error during registration
                                print(f"[!] Failed to register client 	'{login_packet.username}' in DB.")
                                # Optionally change status/message, but login itself succeeded
                                # status = "error"
                                # message = "Login successful, but failed to update session state."
                                pass # Continue for now
                        response_packet = create_response_packet(client_id, status, message)
                    except ValidationError as e:
                        print(f"[!] Invalid login packet structure from client {client_id}: {e}")
                        response_packet = create_response_packet(client_id, "error", f"Invalid login data: {e}")

                elif request_type == "create_room":
                    try:
                        create_room_packet = PacketCreateRoom.model_validate(request_data)
                        status, message = await handle_create_room(db, client_id, create_room_packet)
                        response_packet = create_response_packet(client_id, status, message)
                    except ValidationError as e:
                        print(f"[!] Invalid create room packet structure from client {client_id}: {e}")
                        response_packet = create_response_packet(client_id, "error", f"Invalid create room data: {e}")

                elif request_type == "logout":
                    try:
                        print("++++++++LOGOUT++++++++++++++")
                        logout_packet = PacketLogout.model_validate(request_data)
                        status, message = await handle_logout(db, client_id, logout_packet)
                        if status == "success":
                             if not db.unregister_client(logout_packet.teacher):
                                 print(f"[!] Failed to unregister client 	'{logout_packet.teacher}' from DB.")
                        response_packet = create_response_packet(client_id, status, message)
                    except ValidationError as e:
                        print(f"[!] Invalid log out packet structure from client {client_id}: {e}")
                        response_packet = create_response_packet(client_id, "error", f"Invalid logout data: {e}")

                elif request_type == "join_room":
                    try:
                        print("++++++++JOIN_ROOM++++++++++++++")
                        joinroom_packet = PacketJoinRoom.model_validate(request_data)
                        status, message = await handle_joinroom(db, client_id, joinroom_packet)
                        response_packet = create_response_packet(client_id, status, message)
                    except ValidationError as e:
                        print(f"[!] Invalid join room packet structure from client {client_id}: {e}")
                        response_packet = create_response_packet(client_id, "error", f"Invalid join room data: {e}")

                elif request_type == "refresh":
                    try:
                        print("++++++++REFRESH++++++++++++++")
                        refresh_packet = PacketRefresh.model_validate(request_data)
                        status, message = await handle_refresh(db, client_id, refresh_packet)
                        response_packet = create_response_packet(client_id, status, message)
                    except ValidationError as e:
                        print(f"[!] Invalid refresh packet structure from client {client_id}: {e}")
                        response_packet = create_response_packet(client_id, "error", f"Invalid refresh data: {e}")

                elif request_type == "notify":
                    try:
                        print("++++++++NOTIFY++++++++++++++")
                        notify_packet = PacketNotify.model_validate(request_data)
                        # Pass db (for lookups), the sender function, sender client_id, and packet
                        # No longer passing the removed client_manager
                        status, message = await handle_notify(db, sender_func, client_id, notify_packet)
                        # The response goes back to the *sender* of the notify request
                        response_packet = create_response_packet(client_id, status, message)
                    except ValidationError as e:
                        print(f"[!] Invalid notify packet structure from client {client_id}: {e}")
                        response_packet = create_response_packet(client_id, "error", f"Invalid notify data: {e}")

                else:
                    print(f"[!] Unknown request type 	'{request_type}' from client {client_id}")
                    response_packet = create_response_packet(client_id, "error", f"Unknown request type: {request_type}")

            except json.JSONDecodeError:
                print(f"[!] Invalid JSON received from client {client_id}")
                response_packet = create_response_packet(client_id, "error", "Invalid request format (not JSON)")
            except Exception as e:
                # Catch-all for unexpected errors during request processing
                print(f"[!] Unexpected error processing request for client {client_id}: {type(e).__name__} - {e}")
                import traceback
                traceback.print_exc() # Log stack trace for debugging
                response_packet = create_response_packet(client_id, "error", "Internal server error")

            # 6. Send the RESPONSE packet back to the Load Balancer (for the originating client)
            if response_packet:
                # print(f"[*] Sending response ({len(response_packet)} bytes) to LB for client {client_id}")
                if writer.is_closing():
                     print(f"[!] Writer closed before sending response to client {client_id}.")
                else:
                    try:
                        writer.write(response_packet)
                        await writer.drain()
                        # print(f"[*] Response sent to client {client_id}.")
                    except ConnectionResetError:
                         print(f"[!] Connection reset while sending response to client {client_id}.")
                    except Exception as e:
                         print(f"[!] Error sending response to client {client_id}: {type(e).__name__} - {e}")

    except asyncio.IncompleteReadError:
        print(f"[*] Connection from {peername} closed by peer (incomplete read).")
        # Attempt to clean up client mapping based on the last known client_id for this connection
        if client_id_on_this_connection:
            print(f"[*] Attempting DB cleanup for disconnected client_id 	'{client_id_on_this_connection}'.")
            db.unregister_client_by_id(client_id_on_this_connection)

    except ConnectionResetError:
        print(f"[*] Connection from {peername} reset by peer.")
        # Attempt cleanup
        if client_id_on_this_connection:
            print(f"[*] Attempting DB cleanup for reset client_id 	'{client_id_on_this_connection}'.")
            db.unregister_client_by_id(client_id_on_this_connection)

    except asyncio.CancelledError:
        print(f"[*] Connection handler for {peername} cancelled.")
        # Attempt cleanup
        if client_id_on_this_connection:
            print(f"[*] Attempting DB cleanup for cancelled client_id 	'{client_id_on_this_connection}'.")
            db.unregister_client_by_id(client_id_on_this_connection)
            
    except Exception as e:
        # Catch-all for unexpected errors in the connection handler loop itself
        print(f"[!] Unexpected error in connection handler for {peername}: {type(e).__name__} - {e}")
        import traceback
        traceback.print_exc()
        # Attempt cleanup
        if client_id_on_this_connection:
            print(f"[*] Attempting DB cleanup after error for client_id 	'{client_id_on_this_connection}'.")
            db.unregister_client_by_id(client_id_on_this_connection)

    finally:
        print(f"[*] Closing connection from {peername}")
        if writer.can_write_eof():
            try:
                writer.write_eof()
            except Exception as e:
                 print(f"[!] Error sending EOF for {peername}: {e}")
        try:
            writer.close()
            await writer.wait_closed()
        except Exception as e:
            print(f"[!] Error during writer close for {peername}: {e}")

async def main(host, port):
    """Main function to start the server."""
    global db
    script_dir = os.path.dirname(os.path.abspath(__file__))
    db_path = os.path.join(script_dir, "database", "classroom.db")
    print(f"[*] Using database at: {db_path}")
    # Initialize the database instance
    try:
        db = ClassroomDatabase(db_path=db_path)
        print("[*] Database connection established.")
    except Exception as e:
        print(f"[!] CRITICAL: Failed to initialize database connection: {e}", file=sys.stderr)
        sys.exit(1)

    try:
        server = await asyncio.start_server(handle_connection, host, port)
    except OSError as e:
        print(f"[!] Error starting server on {host}:{port}: {e}", file=sys.stderr)
        sys.exit(1)

    addr = server.sockets[0].getsockname()
    print(f"[*] Server listening on {addr}")
    print("[*] Ready to accept connections from Load Balancer.")

    async with server:
        await server.serve_forever()

def register_with_load_balancer(host, port):
    # (Keep existing registration logic - ensure it handles file IO errors)
    json_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "loadbalancer", "servers.json")
    print(f"[*] Registering server {host}:{port} with Load Balancer config: {json_path}")
    try:
        os.makedirs(os.path.dirname(json_path), exist_ok=True)
        servers = []
        if os.path.exists(json_path):
            try:
                with open(json_path, "r") as f:
                    servers = json.load(f)
                    if not isinstance(servers, list):
                        print(f"[!] servers.json content is not a list. Resetting.")
                        servers = []
            except json.JSONDecodeError:
                print(f"[!] servers.json is corrupted. Resetting.")
                servers = []
            except IOError as e:
                 print(f"[!] Error reading servers.json: {e}. Assuming empty list.")
                 servers = []
        
        server_entry = {"host": host, "port": port}
        if not any(s.get("host") == host and s.get("port") == port for s in servers):
            servers.append(server_entry)
            try:
                with open(json_path, "w") as f:
                    json.dump(servers, f, indent=4)
                print(f"[*] Server {host}:{port} registered.")
            except IOError as e:
                 print(f"[!] Error writing updated servers.json: {e}")
        else:
            print(f"[*] Server {host}:{port} is already registered.")
            # Optionally rewrite to ensure format consistency
            try:
                with open(json_path, "w") as f:
                    json.dump(servers, f, indent=4)
            except IOError as e:
                 print(f"[!] Error rewriting servers.json: {e}")

    except Exception as e:
        print(f"[!] Unexpected error during server registration: {type(e).__name__} - {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Classroom Backend Server (Multi-Server Ready)")
    parser.add_argument("--host", type=str, default="127.0.0.1", help="Host to bind the server to (default: 127.0.0.1)")
    parser.add_argument("--port", type=int, required=True, help="Port to bind the server to")
    args = parser.parse_args()
    host = args.host
    port = args.port
    
    # Setup logger if desired
    # setup_logger()
    
    register_with_load_balancer(host, port)

    try:
        asyncio.run(main(host, port))
    except KeyboardInterrupt:
        print("\n[*] Server shutting down gracefully.")
    except Exception as e:
        print(f"[!] Server critical error: {type(e).__name__} - {e}", file=sys.stderr)
        import traceback
        traceback.print_exc()
        sys.exit(1) # Exit on critical error after logging


