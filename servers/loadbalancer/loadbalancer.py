import asyncio
import struct
import itertools
import argparse
import sys
import uuid # For unique client IDs

# --- Argument Parsing --- 
parser = argparse.ArgumentParser(description="Refactored Async TCP Load Balancer")
parser.add_argument("--lb", type=str, default="127.0.0.1", help="Load Balancer host (default: 127.0.0.1)")
parser.add_argument("--port", type=int, default=8000, help="Load Balancer port (default: 8000)")
parser.add_argument("--backend", type=str, nargs="*", required=True, help="Backend servers in host:port format (e.g., \"127.0.0.1:9001\" \"192.168.1.10:8080\")")
parser.add_argument("--health-check-timeout", type=float, default=1.0, help="Timeout for backend health check connection in seconds (default: 1.0)")
args = parser.parse_args()

LB_HOST = args.lb
LB_PORT = args.port
BACKEND_STRINGS = args.backend
HEALTH_CHECK_TIMEOUT = args.health_check_timeout
BACKEND_SERVERS = [] # Parsed (host, port) tuples

# --- Global State ---
healthy_backend_indices = set()
# Map: server_index -> (reader, writer) | None
server_connections = {}
# Map: client_id (str) -> (reader, writer)
client_connections = {}
# Cycle through healthy server indices for round-robin
round_robin_pool = None
# Lock for accessing shared state (optional for asyncio if access patterns are safe)
# lock = asyncio.Lock()

# --- Health Check Functions --- 
async def check_backend_health(host, port, timeout):
    """Performs a quick TCP connection check to the backend server."""
    try:
        reader, writer = await asyncio.wait_for(
            asyncio.open_connection(host, port),
            timeout=timeout
        )
        writer.close()
        await writer.wait_closed()
        return True
    except (asyncio.TimeoutError, ConnectionRefusedError, OSError):
        return False
    except Exception as e:
        print(f"[!] Unexpected error during health check for {host}:{port}: {e}")
        return False

async def perform_initial_health_checks():
    """Performs health checks on all configured backends at startup."""
    global healthy_backend_indices, round_robin_pool, server_connections # Declare modification of globals
    print("[*] Performing initial health checks...")
    tasks = []
    initial_healthy_indices = set()
    for i, (host, port) in enumerate(BACKEND_SERVERS):
        tasks.append(asyncio.create_task(check_backend_health(host, port, HEALTH_CHECK_TIMEOUT), name=f"InitialCheck-{i}"))
        server_connections[i] = None # Initialize map

    results = await asyncio.gather(*tasks)

    for i, is_healthy in enumerate(results):
        host, port = BACKEND_SERVERS[i]
        if is_healthy:
            print(f"  - Backend {host}:{port} - PASSED")
            initial_healthy_indices.add(i)
        else:
            print(f"  - Backend {host}:{port} - FAILED")

    print("[*] Initial health checks complete.")
    if not initial_healthy_indices:
         print("[!] No backend servers passed the initial health check. Load balancer cannot function.", file=sys.stderr)
         return False # Indicate failure

    healthy_backend_indices = initial_healthy_indices
    if healthy_backend_indices:
        round_robin_pool = itertools.cycle(sorted(list(healthy_backend_indices)))
    else:
        round_robin_pool = None # No healthy servers to cycle through

    return True # Indicate success

# --- New Core Logic ---

async def read_from_server(server_reader, server_index):
    """Reads responses from a server, parses them, and forwards to the correct client."""
    server_host, server_port = BACKEND_SERVERS[server_index]
    peer_name = f"server {server_host}:{server_port} (idx {server_index})"
    print(f"[*] Starting reader task for {peer_name}")
    try:
        while True:
            # 1. Read the total message length (4 bytes)
            len_data = await server_reader.readexactly(4)
            total_msg_len = struct.unpack('!I', len_data)[0]

            # 2. Read the rest of the message (ClientIDLen + ClientID + ServerResponseData)
            message_data = await server_reader.readexactly(total_msg_len)

            # 3. Parse the message
            client_id_len = struct.unpack('!B', message_data[:1])[0]
            client_id_end = 1 + client_id_len
            client_id = message_data[1:client_id_end].decode('utf-8')
            server_response_data = message_data[client_id_end:]

            # 4. Find the client writer
            # async with lock: # If modifying shared state across tasks
            client_info = client_connections.get(client_id)

            if client_info:
                _client_reader, client_writer = client_info
                client_peer = client_writer.get_extra_info("peername", "unknown client")
                print(f"[*] Relaying response from {peer_name} to client {client_id} ({client_peer})")
                try:
                    # Send only the actual server response data
                    client_writer.write(server_response_data)
                    await client_writer.drain()
                except (ConnectionResetError, BrokenPipeError, OSError) as e:
                    print(f"[!] Error writing to client {client_id} ({client_peer}): {e}. Removing client.")
                    # async with lock:
                    if client_id in client_connections:
                        _cr, cw = client_connections.pop(client_id)
                        if not cw.is_closing():
                            cw.close()
                            # await cw.wait_closed() # Don't await here, might block reader task
            else:
                print(f"[!] Received response from {peer_name} for unknown/disconnected client ID: {client_id}. Discarding.")

    except asyncio.IncompleteReadError:
        print(f"[*] Server {peer_name} closed connection (incomplete read).")
    except ConnectionResetError:
        print(f"[*] Server {peer_name} reset connection.")
    except asyncio.CancelledError:
        print(f"[*] Reader task for {peer_name} cancelled.")
        raise # Re-raise cancellation
    except Exception as e:
        print(f"[!] Unexpected error reading from {peer_name}: {e}")
    finally:
        print(f"[*] Stopping reader task for {peer_name}")
        # Mark server as potentially unhealthy and remove connection
        # async with lock:
        if server_index in server_connections:
            server_connections[server_index] = None
        if server_index in healthy_backend_indices:
            healthy_backend_indices.discard(server_index)
            print(f"[!] Marked server {peer_name} as unhealthy due to read error.")
            # Update round_robin_pool if necessary (can be complex, maybe handle in main loop/health check)


async def read_from_client(client_reader, client_writer, client_id):
    """Reads requests from a client, wraps them, and forwards to a backend server."""
    client_addr = client_writer.get_extra_info("peername", "unknown client")
    peer_name = f"client {client_id} ({client_addr})"
    print(f"[*] Starting reader task for {peer_name}")
    try:
        while True:
            client_data = await client_reader.read(4096) # Read client request data
            if not client_data:
                print(f"[*] {peer_name} disconnected.")
                break

            # 1. Select a healthy backend server connection
            selected_server_index = -1
            selected_server_writer = None
            attempts = 0
            # async with lock: # Access shared state safely
            current_healthy_indices = list(healthy_backend_indices) # Use current list
            max_attempts = len(current_healthy_indices)

            if not round_robin_pool or max_attempts == 0:
                print(f"[!] No healthy servers available for {peer_name}. Cannot forward request.")
                # Maybe send an error back to the client? For now, just break.
                break

            while attempts < max_attempts: # Try all currently healthy ones up to max_attempts times
                attempts += 1
                try:
                    server_index = next(round_robin_pool)
                    if server_index not in healthy_backend_indices:
                        continue # Skip if index became unhealthy since cycle was created

                    server_info = server_connections.get(server_index)
                    if server_info:
                        _server_reader, server_writer = server_info
                        selected_server_index = server_index
                        selected_server_writer = server_writer
                        break # Found a connection
                    else:
                         print(f"[!] Server index {server_index} in cycle but no connection found. Skipping.")

                except StopIteration: # Should not happen with cycle
                    print("[!] Round robin pool iteration stopped unexpectedly.")
                    # async with lock:
                    if healthy_backend_indices:
                         round_robin_pool = itertools.cycle(sorted(list(healthy_backend_indices)))
                    else:
                         round_robin_pool = None
                    break # Exit loop if pool is empty

            if not selected_server_writer:
                print(f"[!] Failed to find a healthy backend connection for {peer_name} after {attempts} attempts.")
                break # Cannot forward

            # 2. Wrap the data with client_id and length prefix
            client_id_bytes = client_id.encode('utf-8')
            client_id_len = len(client_id_bytes)
            if client_id_len > 255:
                 print(f"[!] Client ID too long for {peer_name}. Cannot forward.")
                 continue # Skip this message

            # Message: [TotalMsgLen (4 bytes)][ClientIDLen (1 byte)][ClientID (variable)][ClientData (variable)]
            message_payload = struct.pack('!B', client_id_len) + client_id_bytes + client_data
            total_msg_len = len(message_payload)
            wrapped_message = struct.pack('!I', total_msg_len) + message_payload

            # 3. Forward to the selected server
            server_host, server_port = BACKEND_SERVERS[selected_server_index]
            server_peer_name = f"server {server_host}:{server_port} (idx {selected_server_index})"
            print(f"[*] Forwarding request from {peer_name} to {server_peer_name}")
            try:
                selected_server_writer.write(wrapped_message)
                await selected_server_writer.drain()
            except (ConnectionResetError, BrokenPipeError, OSError) as e:
                print(f"[!] Error writing to {server_peer_name}: {e}. Marking as unhealthy.")
                # Mark server as potentially unhealthy and remove connection
                # async with lock:
                if selected_server_index in server_connections:
                    server_connections[selected_server_index] = None
                if selected_server_index in healthy_backend_indices:
                    healthy_backend_indices.discard(selected_server_index)
                    print(f"[!] Marked {server_peer_name} as unhealthy due to write error.")
                # Need to retry sending the client's message? Or just drop it? Drop for now.
                print(f"[!] Dropping request from {peer_name} due to server write error.")
                break # Stop reading from this client for now

    except asyncio.IncompleteReadError:
        print(f"[*] Client {peer_name} closed connection (incomplete read).")
    except ConnectionResetError:
        print(f"[*] Client {peer_name} reset connection.")
    except asyncio.CancelledError:
        print(f"[*] Reader task for {peer_name} cancelled.")
        # Don't re-raise cancellation here, just clean up
    except Exception as e:
        print(f"[!] Unexpected error reading from {peer_name}: {e}")
    finally:
        print(f"[*] Stopping reader task for {peer_name}")
        # Clean up client connection
        # async with lock:
        if client_id in client_connections:
            _cr, cw = client_connections.pop(client_id)
            if not cw.is_closing():
                cw.close()
                # await cw.wait_closed() # Avoid blocking

async def handle_client_connection(client_reader, client_writer):
    """Handles a new client connection by assigning an ID and starting reader task."""
    client_id = str(uuid.uuid4()) # Generate unique ID
    client_addr = client_writer.get_extra_info("peername")
    print(f"[*] Accepted connection from {client_addr}, assigned ID: {client_id}")

    # async with lock:
    if client_id in client_connections:
        # Extremely unlikely with UUIDv4, but handle anyway
        print(f"[!] Duplicate client ID generated? {client_id}. Closing new connection.")
        client_writer.close()
        return
    client_connections[client_id] = (client_reader, client_writer)

    # Start a task to read from this client
    asyncio.create_task(read_from_client(client_reader, client_writer, client_id), name=f"ClientRead-{client_id}")
    # Note: The read_from_client task handles cleanup on exit/error

async def connect_to_backend(server_index):
    """Establishes and stores connection to a backend server."""
    global server_connections # Modifying global
    host, port = BACKEND_SERVERS[server_index]
    peer_name = f"server {host}:{port} (idx {server_index})"
    print(f"[*] Attempting to connect to backend {peer_name}...")
    try:
        reader, writer = await asyncio.wait_for(
            asyncio.open_connection(host, port),
            timeout=HEALTH_CHECK_TIMEOUT * 2 # Longer timeout for initial connect
        )
        print(f"[*] Successfully connected to backend {peer_name}")
        # async with lock:
        server_connections[server_index] = (reader, writer)
        # Start the reader task for this server connection
        asyncio.create_task(read_from_server(reader, server_index), name=f"ServerRead-{server_index}")
        return True
    except (asyncio.TimeoutError, ConnectionRefusedError, OSError) as e:
        print(f"[!] Failed to connect to backend {peer_name}: {e}")
        # async with lock:
        server_connections[server_index] = None # Ensure it's marked as disconnected
        if server_index in healthy_backend_indices:
             healthy_backend_indices.discard(server_index) # Mark unhealthy if connection failed
             print(f"[!] Marked {peer_name} as unhealthy due to connection failure.")
        return False
    except Exception as e:
        print(f"[!] Unexpected error connecting to backend {peer_name}: {e}")
        # async with lock:
        server_connections[server_index] = None
        if server_index in healthy_backend_indices:
             healthy_backend_indices.discard(server_index)
             print(f"[!] Marked {peer_name} as unhealthy due to unexpected connection error.")
        return False


async def main():
    """Main function to start the load balancer server."""
    print("[*] Starting Load Balancer...")
    if not await perform_initial_health_checks():
        sys.exit(1) # Exit if no servers are initially healthy

    # --- Connect to initially healthy backends ---
    connect_tasks = []
    # async with lock: # Access healthy_backend_indices safely
    indices_to_connect = list(healthy_backend_indices) # Copy to avoid modification issues

    for index in indices_to_connect:
        connect_tasks.append(asyncio.create_task(connect_to_backend(index), name=f"Connect-{index}"))

    if connect_tasks:
        await asyncio.gather(*connect_tasks) # Wait for initial connections

    # Verify if any connections were successful
    # async with lock:
    active_server_indices = {idx for idx, conn in server_connections.items() if conn is not None}
    if not active_server_indices:
        print("[!] Failed to establish connection to any initially healthy backend server. Exiting.", file=sys.stderr)
        sys.exit(1)

    # Update healthy indices based on successful connections
    # async with lock:
    healthy_backend_indices.intersection_update(active_server_indices)
    if healthy_backend_indices:
        round_robin_pool = itertools.cycle(sorted(list(healthy_backend_indices)))
        print(f"[*] Updated healthy servers for round-robin: {[BACKEND_SERVERS[i] for i in sorted(list(healthy_backend_indices))]}")
    else:
        round_robin_pool = None
        print("[!] No backend servers are currently connected and healthy after initial connection attempts.")
        # Decide whether to exit or keep running hoping for future health checks/reconnects
        # For now, let it run but warn


    # --- Start Client Listener ---
    try:
        server = await asyncio.start_server(
            handle_client_connection, LB_HOST, LB_PORT
        )
    except OSError as e:
        print(f"[!] Error starting load balancer listener on {LB_HOST}:{LB_PORT}: {e}", file=sys.stderr)
        sys.exit(1)

    addr = server.sockets[0].getsockname()
    print(f"[*] Load Balancer listener started on {addr}")
    print("[*] Ready to accept client connections.")

    async with server:
        await server.serve_forever()

# --- Main Execution ---
if __name__ == "__main__":
    # Parse backends first
    BACKEND_SERVERS = []
    for backend_str in BACKEND_STRINGS:
        try:
            host, port_str = backend_str.rsplit(":", 1)
            port = int(port_str)
            if port <= 0 or port > 65535: raise ValueError("Port out of range")
            if not host: raise ValueError("Host empty")
            BACKEND_SERVERS.append((host, port))
        except ValueError as e:
            print(f"[!] Invalid backend: '{backend_str}'. {e}", file=sys.stderr)
            sys.exit(1)
    if not BACKEND_SERVERS:
        print("[!] No valid backends provided.", file=sys.stderr)
        sys.exit(1)

    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n[*] Load Balancer shutting down.")
    except Exception as e:
        print(f"[!] Load balancer critical error: {e}", file=sys.stderr)
