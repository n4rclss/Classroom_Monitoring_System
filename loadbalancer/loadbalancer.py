import asyncio
import itertools
import argparse
import time
import sys

parser = argparse.ArgumentParser(description="Async TCP Load Balancer with Enhanced Health Checks and Corrected Round Robin")
parser.add_argument("--lb", type=str, default="127.0.0.1", help="Load Balancer host (default: 127.0.0.1)")
parser.add_argument("--port", type=int, default=8000, help="Load Balancer port (default: 8000)")
parser.add_argument("--backend", type=str, nargs="*", required=True, help="Backend servers in host:port format (e.g., \"127.0.0.1:9001\" \"192.168.1.10:8080\")")
parser.add_argument("--health-check-timeout", type=float, default=1.0, help="Timeout for backend health check connection in seconds (default: 1.0)")
args = parser.parse_args()

LB_HOST = args.lb
LB_PORT = args.port
BACKEND_STRINGS = args.backend
HEALTH_CHECK_TIMEOUT = args.health_check_timeout

# --- Parse and Validate Backend Servers ---
BACKEND_SERVERS = []
for backend_str in BACKEND_STRINGS:
    try:
        host, port_str = backend_str.rsplit(":", 1)
        port = int(port_str)
        if port <= 0 or port > 65535:
            raise ValueError("Port number out of range")
        if not host:
             raise ValueError("Host cannot be empty")
        BACKEND_SERVERS.append((host, port))
    except ValueError as e:
        print(f"[!] Invalid backend server format: \"{backend_str}\". Expected host:port. Error: {e}", file=sys.stderr)
        sys.exit(1)

if not BACKEND_SERVERS:
    print("[!] No valid backend servers configured. Exiting.", file=sys.stderr)
    sys.exit(1)

# --- Global State for Health and Round Robin ---
healthy_backend_indices = set()
round_robin_pool = None

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
    global healthy_backend_indices, round_robin_pool # Declare modification of globals
    print("[*] Performing initial health checks...")
    tasks = []
    for i, (host, port) in enumerate(BACKEND_SERVERS):
        tasks.append(asyncio.create_task(check_backend_health(host, port, HEALTH_CHECK_TIMEOUT), name=f"InitialCheck-{i}"))

    results = await asyncio.gather(*tasks)
    initial_healthy_indices = set()

    for i, is_healthy in enumerate(results):
        host, port = BACKEND_SERVERS[i]
        if is_healthy:
            print(f"  - Backend {host}:{port} - PASSED")
            initial_healthy_indices.add(i)
        else:
            print(f"  - Backend {host}:{port} - FAILED")

    print("[*] Initial health checks complete.")
    if not initial_healthy_indices:
         print("[!] No backend servers passed the initial health check. Load balancer will not start.", file=sys.stderr)
         return False

    healthy_backend_indices = initial_healthy_indices
    # Initialize the global round robin pool *after* determining healthy servers
    round_robin_pool = itertools.cycle(sorted(list(healthy_backend_indices))) # Cycle through healthy indices
    return True

async def relay_data(reader, writer, peer_name="peer"):
    """Relays data from reader to writer until EOF or error."""
    try:
        while True:
            data = await reader.read(4096)
            if not data:
                break
            writer.write(data)
            await writer.drain()
    except ConnectionResetError:
        print(f"[*] Connection reset by {peer_name}. Stopping relay.")
    except asyncio.CancelledError:
        pass
    except asyncio.IncompleteReadError:
        print(f"[*] Incomplete read from {peer_name} (connection likely closed abruptly). Stopping relay.")
    except OSError as e:
        print(f"[!] OS Error during data relay with {peer_name}: {e}. Stopping relay.")
    except Exception as e:
        print(f"[!] Unexpected error during data relay with {peer_name}: {e}")
    finally:
        if not writer.is_closing():
            try:
                writer.close()
                await writer.wait_closed()
            except Exception as close_e:
                print(f"[!] Error closing writer for {peer_name}: {close_e}")

async def handle_client_connection(client_reader, client_writer):
    """Handles a new client connection, selects a HEALTHY backend using persistent round-robin, and relays data."""
    global round_robin_pool # Access the global cycle object
    client_addr = client_writer.get_extra_info("peername")
    print(f"[*] Accepted connection from {client_addr}")

    selected_backend_server = None
    backend_reader = None
    backend_writer = None
    attempts = 0

    # Check if the pool was initialized (should be if initial checks passed)
    if round_robin_pool is None or not healthy_backend_indices:
         print(f"[!] Load balancer not ready or no healthy servers available for client {client_addr}. Closing connection.")
         client_writer.close()
         await client_writer.wait_closed()
         return

    # Use the current set of healthy servers determined by initial checks
    current_healthy_indices = list(healthy_backend_indices) # Work with a copy for max_attempts
    max_attempts = len(current_healthy_indices)
    if max_attempts == 0:
        print(f"[!] No backend servers marked as healthy for client {client_addr}. Closing connection.")
        client_writer.close()
        await client_writer.wait_closed()
        return

    try:
        # --- Select a HEALTHY Backend Server (Global Round Robin + On-Demand Check) ---
        while attempts < max_attempts:
            attempts += 1
            try:
                # Get the next index from the GLOBAL cycle
                server_index = next(round_robin_pool)
                if server_index not in healthy_backend_indices:
                     print(f"[!] Server index {server_index} from cycle is no longer marked healthy. Skipping.")
                     continue # Try the next one in the cycle

            except StopIteration: # Should not happen with cycle
                 print(f"[!] Error cycling through healthy servers for {client_addr}. Resetting cycle?")
                 # Re-initialize cycle if needed, though this indicates a potential logic issue
                 if healthy_backend_indices:
                     round_robin_pool = itertools.cycle(sorted(list(healthy_backend_indices)))
                 else:
                     break # No healthy servers left
                 continue # Try again with the reset cycle

            potential_backend = BACKEND_SERVERS[server_index]
            host, port = potential_backend

            # Perform on-demand check
            is_healthy_now = await check_backend_health(host, port, HEALTH_CHECK_TIMEOUT)

            if is_healthy_now:
                selected_backend_server = potential_backend
                print(f"[*] Backend {host}:{port} passed on-demand check. Forwarding {client_addr}.")
                break # Found a healthy server
            else:
                print(f"[!] Backend {host}:{port} failed on-demand health check.")

        if not selected_backend_server:
            print(f"[!] No backend servers passed on-demand health check after {max_attempts} attempts for client {client_addr}. Closing connection.")
            client_writer.close()
            await client_writer.wait_closed()
            return

        # --- Connect to the selected HEALTHY Backend Server ---
        try:
            backend_reader, backend_writer = await asyncio.wait_for(
                asyncio.open_connection(host=selected_backend_server[0], port=selected_backend_server[1]),
                timeout=HEALTH_CHECK_TIMEOUT * 2
            )
            backend_addr = backend_writer.get_extra_info("peername")
            print(f"[*] Successfully connected to backend {backend_addr} for client {client_addr}")
        except (asyncio.TimeoutError, ConnectionRefusedError, OSError) as conn_err:
             print(f"[!] Failed to establish full connection to selected backend {selected_backend_server} for client {client_addr}: {conn_err}. Closing client connection.")
             client_writer.close()
             await client_writer.wait_closed()
             return

        # --- Start Bi-directional Data Relay ---
        client_to_backend_task = asyncio.create_task(
            relay_data(client_reader, backend_writer, peer_name=f"client {client_addr}")
        )
        backend_to_client_task = asyncio.create_task(
            relay_data(backend_reader, client_writer, peer_name=f"backend {backend_addr}")
        )

        done, pending = await asyncio.wait(
            [client_to_backend_task, backend_to_client_task],
            return_when=asyncio.FIRST_COMPLETED
        )

        for task in pending:
            task.cancel()
            try: await task
            except asyncio.CancelledError: pass

    except Exception as e:
        print(f"[!] Unexpected error handling client {client_addr}: {e}")
    finally:
        if client_writer and not client_writer.is_closing():
            try:
                client_writer.close()
                await client_writer.wait_closed()
            except Exception as close_e:
                print(f"[!] Error closing client writer for {client_addr}: {close_e}")
        if backend_writer and not backend_writer.is_closing():
            try:
                backend_writer.close()
                await backend_writer.wait_closed()
            except Exception as close_e:
                print(f"[!] Error closing backend writer for {selected_backend_server}: {close_e}")

async def main():
    """Main function to start the load balancer server."""
    if not await perform_initial_health_checks():
        sys.exit(1)

    try:
        server = await asyncio.start_server(
            handle_client_connection, LB_HOST, LB_PORT
        )
    except OSError as e:
        print(f"[!] Error starting load balancer on {LB_HOST}:{LB_PORT}: {e}", file=sys.stderr)
        print("[!] Check if the port is already in use or if you have permissions.", file=sys.stderr)
        sys.exit(1)

    addr = server.sockets[0].getsockname()
    print(f"[*] TCP Load Balancer listening on {addr}")
    print(f"[*] Configured backends: {BACKEND_SERVERS}")
    initially_healthy_servers = [BACKEND_SERVERS[i] for i in sorted(list(healthy_backend_indices))]
    print(f"[*] Initially healthy backends (used for round-robin): {initially_healthy_servers}")
    print(f"[*] Health check timeout: {HEALTH_CHECK_TIMEOUT}s")
    print("[*] Load balancer ready to accept connections.")

    async with server:
        await server.serve_forever()

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n[*] Load Balancer shutting down gracefully.")
    except Exception as e:
        print(f"[!] Load balancer encountered critical error: {e}", file=sys.stderr)
