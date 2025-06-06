#!/usr/bin/env python3
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
# Import all necessary packet types
from utils.packets import (
    PacketLogin, PacketCreateRoom, PacketLogout, PacketJoinRoom, 
    PacketRefresh, PacketNotify, PacketStreaming, PacketScreenData,
    PacketScreenData
)
from utils.logger import setup_logger # Assuming logger setup is desired

# Import all handlers
from features.login_handler import handle_login
from features.create_room_handler import handle_create_room
from features.logout_handler import handle_logout
from features.joinroom_handler import handle_joinroom
from features.refresh_handler import handle_refresh
from features.notify_handler import handle_notify
from features.streaming_handler import handle_streaming_request # Import the correct handler
from features.screen_data_handler import handle_screen_data # Import the correct handler

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

def create_push_packet(target_client_id: str, payload: dict) -> bytes:
    """Creates a wrapped PUSH packet (notification, command, data) to send to a target client via LB."""
    push_payload_bytes = json.dumps(payload).encode("utf-8")

    client_id_bytes = target_client_id.encode("utf-8")
    client_id_len = len(client_id_bytes)
    server_push_wrapper_payload = struct.pack("!B", client_id_len) + client_id_bytes + push_payload_bytes
    total_msg_len = len(server_push_wrapper_payload)
    wrapped_push_packet = struct.pack("!I", total_msg_len) + server_push_wrapper_payload
    return wrapped_push_packet

async def send_push_to_client(writer: asyncio.StreamWriter, target_client_id: str, payload: dict):
    """Sends a push payload (notification, command, data) to a specific client via the Load Balancer."""
    try:
        push_packet = create_push_packet(target_client_id, payload)
        # print(f"[*] Sending push packet ({len(push_packet)} bytes) to LB for routing to client {target_client_id}")
        writer.write(push_packet)
        await writer.drain()
        # print(f"[*] Successfully sent push packet for {target_client_id} to LB.")
    except ConnectionResetError:
        print(f"[!] Connection reset while trying to send push to LB (targeting {target_client_id}). LB might be down.")
        # Attempt to clean up the target client if possible? Difficult here.
        # db.unregister_client_by_id(target_client_id) # Risky without knowing user
        raise
    except Exception as e:
        print(f"[!] Error sending push packet for {target_client_id} to LB: {type(e).__name__} - {e}")
        raise

# Type hint for the sender function to be passed to handlers
PushSender = Callable[[str, dict], Awaitable[None]]

async def handle_connection(reader: asyncio.StreamReader, writer: asyncio.StreamWriter):
    """Handles a single connection from the load balancer."""
    peername = writer.get_extra_info("peername")
    print(f"[*] Accepted connection from {peername}")
    client_id_on_this_connection: Optional[str] = None 

    # Create a sender function bound to the current writer for this connection
    async def sender_func(target_client_id: str, payload: dict):
        if writer.is_closing():
            print(f"[!] Attempted to send push via closed writer (targeting {target_client_id}).")
            return
        await send_push_to_client(writer, target_client_id, payload)

    try:
        while True:
            # 1. Read length prefix
            len_data = await reader.readexactly(4)
            total_msg_len = struct.unpack("!I", len_data)[0]

            # 2. Read the wrapped message
            wrapped_message_data = await reader.readexactly(total_msg_len)

            # 3. Parse wrapper: ClientID + Original Data
            client_id_len = struct.unpack("!B", wrapped_message_data[:1])[0]
            client_id_end = 1 + client_id_len
            client_id = wrapped_message_data[1:client_id_end].decode("utf-8")
            original_client_data = wrapped_message_data[client_id_end:]
            
            client_id_on_this_connection = client_id
            print(f"[*] Received data from LB for client 	'{client_id}': {original_client_data.decode('utf-8', errors='ignore')}")

            # 4. Process the original client data
            response_packet = None # Default: No direct response needed unless specified by handler
            
            try:
                request_json = original_client_data.decode("utf-8")
                request_data = json.loads(request_json)
                request_type = request_data.get("type")

                # --- Request Routing --- #

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
                        # handle_logout might perform DB checks (e.g., leave rooms)
                        status, message = await handle_logout(db, client_id, logout_packet)
                        if status == "success":
                             # Unregister the client mapping from the DB upon successful logout
                             if not db.unregister_client(logout_packet.teacher):
                                 print(f"[!] Failed to unregister client 	'{logout_packet.teacher}' from DB.")
                                 # Logout succeeded, but cleanup failed. Log and continue.
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
                        notify_packet = PacketNotify.model_validate(request_data)
                        # handle_notify sends pushes via sender_func, returns status to original sender
                        status, message = await handle_notify(db, sender_func, client_id, notify_packet)
                        response_packet = create_response_packet(client_id, status, message)
                    except ValidationError as e:
                        response_packet = create_response_packet(client_id, "error", f"Invalid notify data: {e}")



                elif request_type == "streaming": # Request to start streaming
                    try:
                        print("++++++++STREAMING REQUEST++++++++++++++")
                        streaming_packet = PacketStreaming.model_validate(request_data)
                        # handle_streaming_request sends a push to target, returns status to initiator
                        status, message = await handle_streaming_request(db, sender_func, client_id, streaming_packet)
                        response_packet = create_response_packet(client_id, status, message)
                    except ValidationError as e:
                        print(f"[!] Invalid streaming request packet structure from client {client_id}: {e}")
                        response_packet = create_response_packet(client_id, "error", f"Invalid streaming request data: {e}")
                        
                        
                elif request_type == "screen_data":
                    try:
                        print("++++++++SCREEN DATA RECEIVED++++++++++++++")
                        screen_data_packet = PacketScreenData.model_validate(request_data)

                        status, message = await handle_screen_data(db, sender_func, client_id, screen_data_packet)
                        response_packet = create_response_packet(client_id, status, message)

                    except ValidationError as e:
                        print(f"[!] Invalid screen_data packet structure from client {client_id}: {e}")
                        response_packet = create_response_packet(client_id, "error", f"Invalid screen_data data: {e}")
                else:
         
                  
                    print(f"[!] Unknown request type 	'{request_type}' from client {client_id}")
                    response_packet = create_response_packet(client_id, "error", f"Unknown request type: {request_type}")

            except json.JSONDecodeError:
                print(f"[!] Invalid JSON received from client {client_id}")
                response_packet = create_response_packet(client_id, "error", "Invalid request format (not JSON)")
            except Exception as e:
                print(f"[!] Unexpected error processing request for client {client_id}: {type(e).__name__} - {e}")
                import traceback
                traceback.print_exc()
                response_packet = create_response_packet(client_id, "error", "Internal server error")

            # 5. Send RESPONSE packet back (if one was generated)
            if response_packet:
                if writer.is_closing():
                     print(f"[!] Writer closed before sending response to client {client_id}.")
                else:
                    try:
                        # print(f"[*] Sending response ({len(response_packet)} bytes) to LB for client {client_id}")
                        writer.write(response_packet)
                        await writer.drain()
                        # print(f"[*] Response sent to client {client_id}.")
                    except ConnectionResetError:
                         print(f"[!] Connection reset while sending response to client {client_id}.")
                    except Exception as e:
                         print(f"[!] Error sending response to client {client_id}: {type(e).__name__} - {e}")

    # --- Connection Cleanup --- 
    except asyncio.IncompleteReadError:
        print(f"[*] Connection from {peername} closed by peer (incomplete read).")
    except ConnectionResetError:
        print(f"[*] Connection from {peername} reset by peer.")
    except asyncio.CancelledError:
        print(f"[*] Connection handler for {peername} cancelled.")
    except Exception as e:
        print(f"[!] Unexpected error in connection handler loop for {peername}: {type(e).__name__} - {e}")
        import traceback
        traceback.print_exc()
    finally:
        print(f"[*] Closing connection from {peername}.")
        # Attempt DB cleanup using the last known client_id for this connection
        if client_id_on_this_connection:
            print(f"[*] Attempting DB cleanup for client_id 	'{client_id_on_this_connection}' on disconnect.")
            if db: # Ensure db is initialized
                db.unregister_client_by_id(client_id_on_this_connection)
            else:
                 print(f"[!] DB not available for cleanup of client_id 	'{client_id_on_this_connection}'.")
                 
        # Close the writer stream
        if writer.can_write_eof():
            try: writer.write_eof()
            except Exception as e: print(f"[!] Error sending EOF for {peername}: {e}")
        try:
            writer.close()
            await writer.wait_closed()
        except Exception as e: print(f"[!] Error during writer close for {peername}: {e}")

async def main(host, port):
    """Main function to start the server."""
    global db
    script_dir = os.path.dirname(os.path.abspath(__file__))
    db_path = os.path.join(script_dir, "database", "classroom.db")
    print(f"[*] Using database at: {db_path}")
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
    # (Keep existing registration logic)
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
        sys.exit(1)

