#!/usr/bin/env python3
import asyncio
import itertools
import argparse
import time

parser = argparse.ArgumentParser(description="Async TCP Load Balancer with Health Checks")
parser.add_argument("--lb", type=str, default="127.0.0.1", help="Load Balancer host (default: 127.0.0.1)")
parser.add_argument("--port", type=int, default=8000, help="Load Balancer port (default: 8000)")
parser.add_argument("--backend", type=str, nargs='*', default=["127.0.0.1"], help="Backend servers IP (default: 127.0.0.1)")
parser.add_argument("--health-check-timeout", type=float, default=1.0, help="Timeout for backend health check connection in seconds (default: 1.0)")
args = parser.parse_args()

LB_HOST = args.lb
LB_PORT = args.port
BACKEND_IPS = args.backend
HEALTH_CHECK_TIMEOUT = args.health_check_timeout

# List of backend server addresses (host, port)
BACKEND_SERVERS = [
    (ip, 9001 + i) for i, ip in enumerate(BACKEND_IPS)
]

# Use itertools.cycle for simple Round Robin selection attempt
backend_server_pool = itertools.cycle(range(len(BACKEND_SERVERS)))

async def check_backend_health(host, port, timeout):
    """Performs a quick TCP connection check to the backend server."""
    try:
        # Try to open a connection with a timeout
        reader, writer = await asyncio.wait_for(
            asyncio.open_connection(host, port),
            timeout=timeout
        )
        # If connection succeeds, close it immediately
        writer.close()
        await writer.wait_closed()
        # print(f"[*] Health check SUCCESS for {host}:{port}")
        return True
    except (asyncio.TimeoutError, ConnectionRefusedError, OSError) as e:
        # print(f"[!] Health check FAILED for {host}:{port} - {e}")
        return False
    except Exception as e:
        print(f"[!] Unexpected error during health check for {host}:{port}: {e}")
        return False

async def relay_data(reader, writer, peer_name="peer"):
    """Relays data from reader to writer until EOF or error."""
    try:
        while True:
            data = await reader.read(4096)
            if not data:
                # print(f"[*] Connection closed by {peer_name}. Stopping relay.")
                break
            writer.write(data)
            await writer.drain()
    except ConnectionResetError:
        print(f"[*] Connection reset by {peer_name}. Stopping relay.")
    except asyncio.CancelledError:
        # print(f"[*] Relay task cancelled for {peer_name}.")
        pass # Don't log cancellation noise during cleanup
    except asyncio.IncompleteReadError:
        print(f"[*] Incomplete read from {peer_name} (connection likely closed abruptly). Stopping relay.")
    except OSError as e:
        # Catch other potential socket errors during relay
        print(f"[!] OS Error during data relay with {peer_name}: {e}. Stopping relay.")
    except Exception as e:
        print(f"[!] Unexpected error during data relay with {peer_name}: {e}")
    finally:
        if not writer.is_closing():
            try:
                writer.close()
                await writer.wait_closed()
                # print(f"[*] Closed writer connection to {peer_name}.")
            except Exception as close_e:
                print(f"[!] Error closing writer for {peer_name}: {close_e}")

async def handle_client_connection(client_reader, client_writer):
    """Handles a new client connection, selects a HEALTHY backend, and relays data."""
    client_addr = client_writer.get_extra_info("peername")
    print(f"[*] Accepted connection from {client_addr}")

    selected_backend_server = None
    backend_reader = None
    backend_writer = None
    attempts = 0
    max_attempts = len(BACKEND_SERVERS) # Try each server once per client connection attempt

    try:
        # --- Select a HEALTHY Backend Server (Round Robin with Health Check) ---
        while attempts < max_attempts:
            attempts += 1
            server_index = next(backend_server_pool)
            potential_backend = BACKEND_SERVERS[server_index]
            host, port = potential_backend

            print(f"[*] Attempting health check for backend {host}:{port} (Attempt {attempts}/{max_attempts})")
            is_healthy = await check_backend_health(host, port, HEALTH_CHECK_TIMEOUT)

            if is_healthy:
                selected_backend_server = potential_backend
                print(f"[*] Backend {host}:{port} is healthy. Forwarding {client_addr}.")
                break # Found a healthy server
            else:
                print(f"[!] Backend {host}:{port} failed health check.")

        if not selected_backend_server:
            print(f"[!] No healthy backend servers found after checking all {max_attempts} servers for client {client_addr}. Closing connection.")
            # Inform the client maybe? For now, just close.
            client_writer.close()
            await client_writer.wait_closed()
            return

        # --- Connect to the selected HEALTHY Backend Server ---
        # We already know it passed the health check moments ago, but connection might still fail.
        try:
            backend_reader, backend_writer = await asyncio.wait_for(
                asyncio.open_connection(
                    host=selected_backend_server[0],
                    port=selected_backend_server[1]
                ),
                timeout=HEALTH_CHECK_TIMEOUT * 2 # Allow slightly longer for full connection
            )
            backend_addr = backend_writer.get_extra_info("peername")
            print(f"[*] Successfully connected to backend {backend_addr} for client {client_addr}")
        except (asyncio.TimeoutError, ConnectionRefusedError, OSError) as conn_err:
             print(f"[!] Failed to establish full connection to healthy backend {selected_backend_server} for client {client_addr}: {conn_err}. Closing client connection.")
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

        # Wait for either relay task to complete (e.g., connection closed, error)
        done, pending = await asyncio.wait(
            [client_to_backend_task, backend_to_client_task],
            return_when=asyncio.FIRST_COMPLETED
        )

        # print(f"[*] Relay finished for {client_addr} <-> {backend_addr}. Cleaning up pending tasks.")

        # Cancel the remaining task
        for task in pending:
            task.cancel()
            try:
                await task # Allow cancellation to propagate
            except asyncio.CancelledError:
                pass

    except Exception as e:
        # Catch unexpected errors in the handling logic itself
        print(f"[!] Unexpected error handling client {client_addr}: {e}")
    finally:
        # print(f"[*] Cleaning up connections for client {client_addr}.")
        # Ensure both client and backend writers are closed if they exist
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
                # Use selected_backend_server here as backend_addr might not be set if connection failed
                print(f"[!] Error closing backend writer for {selected_backend_server}: {close_e}")

async def main():
    """Main function to start the load balancer server."""
    if not BACKEND_SERVERS:
        print("[!] No backend servers configured. Exiting.")
        return

    try:
        server = await asyncio.start_server(
            handle_client_connection, LB_HOST, LB_PORT
        )
    except OSError as e:
        print(f"[!] Error starting load balancer on {LB_HOST}:{LB_PORT}: {e}")
        print("[!] Check if the port is already in use or if you have permissions.")
        return

    addr = server.sockets[0].getsockname()
    print(f"[*] TCP Load Balancer with Health Checks listening on {addr}")
    print(f"[*] Configured backends: {BACKEND_SERVERS}")
    print(f"[*] Health check timeout: {HEALTH_CHECK_TIMEOUT}s")

    async with server:
        await server.serve_forever()

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n[*] Load Balancer shutting down gracefully.")
    except Exception as e:
        print(f"[!] Load balancer encountered critical error: {e}")

