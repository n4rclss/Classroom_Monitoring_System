#!/usr/bin/env python3
import asyncio
import itertools
import argparse

parser = argparse.ArgumentParser(description="Async TCP Load Balancer")
parser.add_argument("--lb", type=str, default="127.0.0.1", help="Load Balancer host (default:127.0.0.1)")
parser.add_argument("--port", type=int, default=8000, help="Load Balancer port (default:8000)")
parser.add_argument("--backend", type=str, nargs='*', default=["127.0.0.1"], help="Backend servers IP (default:127.0.0.1)")
args = parser.parse_args()

LB_HOST = args.lb  # Host for the load balancer to listen on
LB_PORT = args.port  # Port for the load balancer to listen on
BACKEND_IPS = args.backend  # List of backend server IPs

# List of backend server addresses (host, port)
BACKEND_SERVERS = [
    (ip, 9001 + i) for i, ip in enumerate(BACKEND_IPS)
]  

# Use itertools.cycle for simple Round Robin
backend_server_pool = itertools.cycle(BACKEND_SERVERS)

async def relay_data(reader, writer, peer_name="peer"):
    """Relays data from reader to writer until EOF or error."""
    try:
        while True:
            data = await reader.read(4096)  # Read chunk of data
            if not data:
                print(f"[*] Connection closed by {peer_name}. Stopping relay.")
                break
            writer.write(data)
            await writer.drain()  # Ensure data is sent
    except ConnectionResetError:
        print(f"[*] Connection reset by {peer_name}. Stopping relay.")
    except asyncio.CancelledError:
        print(f"[*] Relay task cancelled for {peer_name}.")
    except Exception as e:
        print(f"[!] Error during data relay from {peer_name}: {e}")
    finally:
        if not writer.is_closing():
            try:
                writer.close()
                await writer.wait_closed()
                print(f"[*] Closed writer connection to {peer_name}.")
            except Exception as close_e:
                print(f"[!] Error closing writer for {peer_name}: {close_e}")

async def handle_client_connection(client_reader, client_writer):
    """Handles a new client connection, selects a backend, and relays data."""
    client_addr = client_writer.get_extra_info("peername")
    print(f"[*] Accepted connection from {client_addr}")

    backend_server = None
    backend_reader = None
    backend_writer = None

    try:
        # --- Select Backend Server (Round Robin) ---
        backend_server = next(backend_server_pool)
        print(f"[*] Forwarding {client_addr} to backend {backend_server}")

        # --- Connect to Backend Server ---
        backend_reader, backend_writer = await asyncio.open_connection(
            host=backend_server[0],
            port=backend_server[1]
        )
        backend_addr = backend_writer.get_extra_info("peername")
        print(f"[*] Connected to backend {backend_addr} for client {client_addr}")

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

        print(f"[*] Relay finished for {client_addr} <-> {backend_addr}. Cleaning up.")

        for task in pending:
            task.cancel()
            try:
                await task
            except asyncio.CancelledError:
                pass

    except ConnectionRefusedError:
        print(f"[!] Connection refused by backend server {backend_server}. Cannot forward {client_addr}.")
    except asyncio.TimeoutError:
        print(f"[!] Timeout connecting to backend server {backend_server}. Cannot forward {client_addr}.")
    except Exception as e:
        print(f"[!] Error handling client {client_addr}: {e}")
    finally:
        print(f"[*] Closing connections for client {client_addr}.")
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
                print(f"[!] Error closing backend writer for {backend_addr}: {close_e}")

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
    print(f"[*] TCP Load Balancer listening on {addr}")
    print(f"[*] Forwarding to backends: {BACKEND_SERVERS}")

    async with server:
        await server.serve_forever()

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("[*] Load Balancer shutting down gracefully.")