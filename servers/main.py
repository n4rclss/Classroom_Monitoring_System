#!/usr/bin/env python3
import asyncio
import struct
import json
import argparse
import sys
import os
from pydantic import ValidationError
from database.sqlite_db import ClassroomDatabase
from utils.packets import PacketLogin
from utils.logger import setup_logger
from features.login_handler import handle_login # Import the modularized handler

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
db = None

# --- Packet Handling ---

def create_response_packet(client_id: str, status: str, message: str) -> bytes:
    """Creates the wrapped response packet to send back to the load balancer."""
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

async def handle_connection(reader, writer):
    """Handles a single connection from the load balancer."""
    peername = writer.get_extra_info("peername")
    print(f"[*] Accepted connection from {peername}")

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

            print(f"[*] Received data from LB for client {client_id}: {original_client_data.decode("utf-8", errors="ignore")}")

            # 4. Process the original client data
            response_packet = b""
            try:
                # Attempt to parse as JSON
                request_json = original_client_data.decode("utf-8")
                request_data = json.loads(request_json)
                request_type = request_data.get("type")

                if request_type == "login":
                    try:
                        login_packet = PacketLogin.model_validate(request_data)
                        status, message = await handle_login(db, client_id, login_packet)
                        response_packet = create_response_packet(client_id, status, message)

                    except ValidationError as e:
                        print(f"[!] Invalid login packet structure from client {client_id}: {e}")
                        response_packet = create_response_packet(client_id, "error", f"Invalid login data: {e}")
                
                # TODO: Add handlers for other feature types (e.g., join_room, chat_message)
                # elif request_type == "join_room":
                #     Code thêm ở đây 
                #     
                
                else:
                    print(f"[!] Unknown request type ", request_type, " from client {client_id}")
                    response_packet = create_response_packet(client_id, "error", f"Unknown request type: {request_type}")

            except json.JSONDecodeError:
                print(f"[!] Invalid JSON received from client {client_id}")
                response_packet = create_response_packet(client_id, "error", "Invalid request format (not JSON)")
            except Exception as e:
                print(f"[!] Unexpected error processing request for client {client_id}: {e}")
                response_packet = create_response_packet(client_id, "error", "Internal server error")

            # 6. Send the wrapped response back to the Load Balancer
            if response_packet:
                print(f"[*] Sending response to LB for client {client_id}")
                writer.write(response_packet)
                await writer.drain()

    except asyncio.IncompleteReadError:
        print(f"[*] Connection from {peername} closed (incomplete read).")
    except ConnectionResetError:
        print(f"[*] Connection from {peername} reset.")
    except Exception as e:
        print(f"[!] Unexpected error in connection handler for {peername}: {e}")
    finally:
        print(f"[*] Closing connection from {peername}")
        writer.close()
        try:
            await writer.wait_closed()
        except Exception as e:
            print(f"[!] Error during writer close for {peername}: {e}")

async def main(host, port):
    """Main function to start the server."""
    global db
    # Determine DB path relative to this script's location
    script_dir = os.path.dirname(os.path.abspath(__file__))
    db_path = os.path.join(script_dir, "database", "classroom.db")
    print(f"[*] Using database at: {db_path}")
    db = ClassroomDatabase(db_path=db_path)

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
    json_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "loadbalancer", "servers.json")
    print(f"[*] Registering server with Load Balancer at {host}:{port}")
    print(f"[*] JSON path for registration: {json_path}")
    try:
        with open(json_path, "r+") as f:
            servers = json.load(f)
            if any(s["host"] == host and s["port"] == port for s in servers):
                print(f"[*] Server {host}:{port} is already registered.")
            else:
                servers.append({"host": host, "port": port})
            f.seek(0)
            json.dump(servers, f)
    except FileNotFoundError:
        with open(json_path, "w") as f:
            json.dump([{"host": host, "port": port}], f)
            
if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Classroom Backend Server")
    parser.add_argument("--host", type=str, default="127.0.0.1", help="Host to bind the server to (default: 127.0.0.1)")
    parser.add_argument("--port", type=int, required=True, help="Port to bind the server to")
    args = parser.parse_args()
    host = args.host
    port = args.port
    register_with_load_balancer(host, port)

    try:
        asyncio.run(main(host, port))
    except KeyboardInterrupt:
        print("\n[*] Server shutting down.")
    except Exception as e:
        print(f"[!] Server critical error: {e}", file=sys.stderr)

