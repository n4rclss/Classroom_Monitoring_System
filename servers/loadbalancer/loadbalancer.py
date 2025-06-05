import json
import asyncio
import struct
import itertools
import argparse
import sys
import os
import uuid
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

servers_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "servers.json")

class ServerListHandler(FileSystemEventHandler):
    def __init__(self, lb):
        self.lb = lb  # Reference to the load balancer

    def on_modified(self, event):
        if event.src_path.endswith(servers_path):
            asyncio.create_task(self.lb.update_servers())

class LoadBalancer:
    def __init__(self):
        self.backend_servers = []
        self.healthy_indices = set()
        self.round_robin_pool = None
        self.server_connections = {}
        self.client_connections = {}
        self.observer = None
        self.health_check_timeout = 1.0

    async def update_servers(self):
        """Reload servers.json and update the backend list."""
        try:
            with open(servers_path, "r") as f:
                servers = json.load(f)
            new_servers = [(s["host"], s["port"]) for s in servers]
            
            # Identify newly added servers for logging
            current_servers_set = set(self.backend_servers)
            new_servers_set = set(new_servers)
            added_servers = new_servers_set - current_servers_set
            for host, port in added_servers:
                print(f"[*] New server detected: {host}:{port}")
            
            if new_servers != self.backend_servers:
                print("[*] Updating backend servers list...")
                # Store previous connections before updating
                old_connections = self.server_connections.copy()
                old_healthy_indices = self.healthy_indices.copy()
                
                self.backend_servers = new_servers
                # Reset connections for simplicity, health check will re-establish
                self.server_connections = {}
                self.healthy_indices = set()
                
                await self.perform_health_checks() # This will trigger connection attempts via initialize/connect_to_backend logic
                
                # Attempt to connect to newly detected *and* healthy servers
                connect_tasks = []
                for i, server in enumerate(self.backend_servers):
                    if i in self.healthy_indices and i not in old_connections:
                         # Only try connecting if it's healthy and wasn't connected before
                         connect_tasks.append(asyncio.create_task(
                             self.connect_to_backend(i),
                             name=f"ConnectNew-{i}"
                         ))
                if connect_tasks:
                    await asyncio.gather(*connect_tasks)
        except Exception as e:
            print(f"[!] Failed to update servers: {e}")

    async def check_backend_health(self, host, port, timeout):
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

    async def perform_health_checks(self):
        """Check all servers and update healthy_indices."""
        print("[*] Performing health checks...")
        tasks = []
        new_healthy_indices = set()
        
        for i, (host, port) in enumerate(self.backend_servers):
            tasks.append(asyncio.create_task(
                self.check_backend_health(host, port, self.health_check_timeout),
                name=f"HealthCheck-{i}"
            ))

        results = await asyncio.gather(*tasks)

        for i, is_healthy in enumerate(results):
            host, port = self.backend_servers[i]
            if is_healthy:
                print(f"  - Backend {host}:{port} - PASSED")
                new_healthy_indices.add(i)
            else:
                print(f"  - Backend {host}:{port} - FAILED")

        self.healthy_indices = new_healthy_indices
        if self.healthy_indices:
            self.round_robin_pool = itertools.cycle(sorted(list(self.healthy_indices)))
        else:
            self.round_robin_pool = None
            print("[!] No healthy backend servers available")

    async def connect_to_backend(self, server_index):
        """Establishes and stores connection to a backend server."""
        host, port = self.backend_servers[server_index]
        peer_name = f"server {host}:{port} (idx {server_index})"
        print(f"[*] Attempting to connect to backend {peer_name}...")
        try:
            reader, writer = await asyncio.wait_for(
                asyncio.open_connection(host, port),
                timeout=self.health_check_timeout * 2
            )
            print(f"[*] Successfully connected to backend {peer_name}")
            self.server_connections[server_index] = (reader, writer)
            asyncio.create_task(self.read_from_server(reader, server_index), 
                              name=f"ServerRead-{server_index}")
            return True
        except (asyncio.TimeoutError, ConnectionRefusedError, OSError) as e:
            print(f"[!] Failed to connect to backend {peer_name}: {e}")
            self.server_connections[server_index] = None
            if server_index in self.healthy_indices:
                self.healthy_indices.discard(server_index)
                print(f"[!] Marked {peer_name} as unhealthy due to connection failure.")
            return False
        except Exception as e:
            print(f"[!] Unexpected error connecting to backend {peer_name}: {e}")
            self.server_connections[server_index] = None
            if server_index in self.healthy_indices:
                self.healthy_indices.discard(server_index)
                print(f"[!] Marked {peer_name} as unhealthy due to unexpected error.")
            return False

    async def read_from_server(self, server_reader, server_index):
        """Reads responses from a server and forwards to clients."""
        host, port = self.backend_servers[server_index]
        peer_name = f"server {host}:{port} (idx {server_index})"
        print(f"[*] Starting reader task for {peer_name}")
        try:
            while True:
                len_data = await server_reader.readexactly(4)
                total_msg_len = struct.unpack('!I', len_data)[0]
                message_data = await server_reader.readexactly(total_msg_len)

                client_id_len = struct.unpack('!B', message_data[:1])[0]
                client_id_end = 1 + client_id_len
                client_id = message_data[1:client_id_end].decode('utf-8')
                server_response_data = message_data[client_id_end:]

                client_info = self.client_connections.get(client_id)
                if client_info:
                    _, client_writer = client_info
                    try:
                        client_writer.write(server_response_data)
                        await client_writer.drain()
                    except (ConnectionResetError, BrokenPipeError, OSError) as e:
                        print(f"[!] Error writing to client {client_id}: {e}")
                        if client_id in self.client_connections:
                            _, cw = self.client_connections.pop(client_id)
                            if not cw.is_closing():
                                cw.close()
                else:
                    print(f"[!] Received response for unknown client ID: {client_id}")

        except (asyncio.IncompleteReadError, ConnectionResetError):
            print(f"[*] Connection to {peer_name} closed")
        except Exception as e:
            print(f"[!] Error reading from {peer_name}: {e}")
        finally:
            print(f"[*] Stopping reader task for {peer_name}")
            if server_index in self.server_connections:
                self.server_connections[server_index] = None
            if server_index in self.healthy_indices:
                self.healthy_indices.discard(server_index)
                print(f"[!] Marked {peer_name} as unhealthy")

    async def read_from_client(self, client_reader, client_writer, client_id):
        """Reads requests from a client and forwards to backend servers."""
        client_addr = client_writer.get_extra_info("peername", "unknown client")
        peer_name = f"client {client_id} ({client_addr})"
        print(f"[*] Starting reader task for {peer_name}")
        try:
            while True:
                client_data = await client_reader.read(4096)
                if not client_data:
                    print(f"[*] {peer_name} disconnected")
                    break

                if not self.round_robin_pool:
                    print(f"[!] No healthy servers for {peer_name}")
                    break

                attempts = 0
                max_attempts = len(self.healthy_indices)
                while attempts < max_attempts:
                    attempts += 1
                    server_index = next(self.round_robin_pool)
                    if server_index not in self.healthy_indices:
                        continue

                    server_info = self.server_connections.get(server_index)
                    if server_info:
                        _, server_writer = server_info
                        break
                    else:
                        print(f"[!] No connection for server index {server_index}")
                        continue

                if not server_info:
                    print(f"[!] No available servers for {peer_name}")
                    break

                client_id_bytes = client_id.encode('utf-8')
                client_id_len = len(client_id_bytes)
                message_payload = struct.pack('!B', client_id_len) + client_id_bytes + client_data
                wrapped_message = struct.pack('!I', len(message_payload)) + message_payload

                try:
                    server_writer.write(wrapped_message)
                    await server_writer.drain()
                except (ConnectionResetError, BrokenPipeError, OSError) as e:
                    print(f"[!] Error writing to server: {e}")
                    if server_index in self.server_connections:
                        self.server_connections[server_index] = None
                    if server_index in self.healthy_indices:
                        self.healthy_indices.discard(server_index)
                    break

        except (asyncio.IncompleteReadError, ConnectionResetError):
            print(f"[*] Connection to {peer_name} closed")
        except Exception as e:
            print(f"[!] Error reading from {peer_name}: {e}")
        finally:
            print(f"[*] Stopping reader task for {peer_name}")
            if client_id in self.client_connections:
                _, cw = self.client_connections.pop(client_id)
                if not cw.is_closing():
                    cw.close()

    async def handle_client_connection(self, client_reader, client_writer):
        """Handles new client connections."""
        client_id = str(uuid.uuid4())
        client_addr = client_writer.get_extra_info("peername")
        print(f"[*] Accepted connection from {client_addr}, ID: {client_id}")

        if client_id in self.client_connections:
            print(f"[!] Duplicate client ID: {client_id}")
            client_writer.close()
            return

        self.client_connections[client_id] = (client_reader, client_writer)
        asyncio.create_task(
            self.read_from_client(client_reader, client_writer, client_id),
            name=f"ClientRead-{client_id}"
        )

    async def initialize(self):
        """Initializes the load balancer."""
        await self.update_servers()  # Load initial servers from JSON
        
        # Connect to healthy backends
        connect_tasks = []
        for index in list(self.healthy_indices):
            connect_tasks.append(asyncio.create_task(
                self.connect_to_backend(index),
                name=f"Connect-{index}"
            ))
        
        if connect_tasks:
            await asyncio.gather(*connect_tasks)

        # Start filesystem watcher
        event_handler = ServerListHandler(self)
        self.observer = Observer()
        self.observer.schedule(event_handler, path=".", recursive=False)
        self.observer.start()

async def main():
    parser = argparse.ArgumentParser(description="Async TCP Load Balancer")
    parser.add_argument("--lb", type=str, default="127.0.0.1", help="Load Balancer host")
    parser.add_argument("--port", type=int, default=8000, help="Load Balancer port")
    parser.add_argument("--health-check-timeout", type=float, default=1.0, 
                       help="Health check timeout in seconds")
    args = parser.parse_args()

    lb = LoadBalancer()
    lb.health_check_timeout = args.health_check_timeout
    
    # Create initial servers.json if it doesn't exist
    if not os.path.exists(servers_path):
        with open(servers_path, "w") as f:
            json.dump([], f)

    await lb.initialize()

    try:
        server = await asyncio.start_server(
            lb.handle_client_connection, args.lb, args.port
        )
        addr = server.sockets[0].getsockname()
        print(f"[*] Load Balancer listening on {addr}")
        
        async with server:
            await server.serve_forever()
    except Exception as e:
        print(f"[!] Error: {e}")
    finally:
        if lb.observer:
            lb.observer.stop()
            lb.observer.join()

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n[*] Load Balancer shutting down.")
    except Exception as e:
        print(f"[!] Critical error: {e}", file=sys.stderr)