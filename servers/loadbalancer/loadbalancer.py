
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
        self.lb = lb
        self.loop = asyncio.get_event_loop()

    def on_modified(self, event):
        if event.src_path.endswith(servers_path):
            # Schedule update_servers to run in the main event loop
            asyncio.run_coroutine_threadsafe(self.lb.update_servers(), self.loop)

class LoadBalancer:
    def __init__(self):
        self.backend_servers = [] # List of (host, port) tuples
        self.server_connections = {} # Map: server_index -> (reader, writer, read_task)
        self.healthy_indices = set() # Set of indices of healthy servers
        self.round_robin_pool = None
        self.client_connections = {} # Map: client_id -> (reader, writer)
        self.observer = None
        self.health_check_timeout = 1.0
        self._update_lock = asyncio.Lock() # Lock to prevent concurrent updates

    async def _close_server_connection(self, server_index):
        """Safely close connection and cancel reader task for a server index."""
        if server_index in self.server_connections:
            reader, writer, read_task = self.server_connections.pop(server_index)
            host, port = self.backend_servers[server_index]
            peer_name = f"server {host}:{port} (idx {server_index})"
            print(f"[*] Closing connection to {peer_name}")
            if writer and not writer.is_closing():
                writer.close()
                try:
                    await writer.wait_closed()
                except Exception as e:
                    print(f"[!] Error closing writer for {peer_name}: {e}")
            if read_task and not read_task.done():
                read_task.cancel()
                try:
                    await read_task # Allow task to handle cancellation
                except asyncio.CancelledError:
                    print(f"[*] Reader task for {peer_name} cancelled.")
                except Exception as e:
                    print(f"[!] Error awaiting cancelled reader task for {peer_name}: {e}")
            print(f"[*] Connection to {peer_name} closed successfully.")
        if server_index in self.healthy_indices:
             self.healthy_indices.discard(server_index)

    async def update_servers(self):
        """Reload servers.json, perform health checks, and manage connections."""
        async with self._update_lock:
            print("[*] Update triggered: Reloading servers.json...")
            try:
                with open(servers_path, "r") as f:
                    servers_raw = json.load(f)
                
                # Deduplicate servers
                seen_servers = set()
                current_servers_list = []
                for s in servers_raw:
                    server_tuple = (s["host"], s["port"])
                    if server_tuple not in seen_servers:
                        current_servers_list.append(server_tuple)
                        seen_servers.add(server_tuple)
                
                # --- Health Check Phase ---
                print("[*] Performing health checks...")
                health_tasks = []
                current_healthy_indices = set()
                for i, (host, port) in enumerate(current_servers_list):
                    health_tasks.append(asyncio.create_task(
                        self.check_backend_health(host, port, self.health_check_timeout),
                        name=f"HealthCheck-{i}"
                    ))
                
                health_results = await asyncio.gather(*health_tasks, return_exceptions=True)
                
                for i, result in enumerate(health_results):
                    host, port = current_servers_list[i]
                    if isinstance(result, Exception) or not result:
                        print(f"  - Backend {host}:{port} (idx {i}) - FAILED")
                    else:
                        print(f"  - Backend {host}:{port} (idx {i}) - PASSED")
                        current_healthy_indices.add(i)
                
                # --- Connection Management Phase ---
                old_backend_servers = self.backend_servers
                old_healthy_indices = self.healthy_indices
                
                self.backend_servers = current_servers_list # Update server list *after* health checks
                self.healthy_indices = current_healthy_indices
                
                # Indices based on the *new* self.backend_servers list
                new_indices = set(range(len(self.backend_servers)))
                indices_to_connect = self.healthy_indices - set(self.server_connections.keys())
                indices_to_close = set(self.server_connections.keys()) - self.healthy_indices
                
                close_tasks = []
                for index_to_close in indices_to_close:
                    close_tasks.append(self._close_server_connection(index_to_close))
                if close_tasks:
                    print(f"[*] Closing connections to unhealthy/removed servers: {indices_to_close}")
                    await asyncio.gather(*close_tasks, return_exceptions=True)
                    
                connect_tasks = []
                for index_to_connect in indices_to_connect:
                    if index_to_connect in self.healthy_indices: # Double check health before connecting
                         print(f"[*] New healthy server detected or needs connection: {self.backend_servers[index_to_connect]}")
                         connect_tasks.append(self.connect_to_backend(index_to_connect))
                if connect_tasks:
                    print(f"[*] Establishing connections to new/healthy servers: {indices_to_connect}")
                    await asyncio.gather(*connect_tasks, return_exceptions=True)

                # Update Round Robin Pool
                if self.healthy_indices:
                    # Ensure pool uses indices present in server_connections
                    connected_healthy_indices = sorted(list(self.healthy_indices.intersection(self.server_connections.keys())))
                    if connected_healthy_indices:
                         self.round_robin_pool = itertools.cycle(connected_healthy_indices)
                         print(f"[*] Updated round robin pool with indices: {connected_healthy_indices}")
                    else:
                         self.round_robin_pool = None
                         print("[!] No connected healthy servers available for round robin.")
                else:
                    self.round_robin_pool = None
                    print("[!] No healthy backend servers available.")

            except FileNotFoundError:
                 print(f"[!] {servers_path} not found. No servers loaded.")
                 self.backend_servers = []
                 self.healthy_indices = set()
                 self.round_robin_pool = None
                 # Close any existing connections if file disappears
                 close_tasks = [self._close_server_connection(idx) for idx in list(self.server_connections.keys())]
                 if close_tasks:
                     await asyncio.gather(*close_tasks, return_exceptions=True)
            except json.JSONDecodeError:
                 print(f"[!] Error decoding JSON from {servers_path}. Server list not updated.")
            except Exception as e:
                 print(f"[!] Unexpected error during server update: {e}")

    # ... (check_backend_health remains the same) ...
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

    # Remove perform_health_checks as a separate public method, logic moved into update_servers

    async def connect_to_backend(self, server_index):
        """Establishes and stores connection to a backend server."""
        # Prevent connecting if already connected or index out of bounds
        if server_index >= len(self.backend_servers) or server_index in self.server_connections:
             # print(f"[*] Skipping connection attempt for index {server_index} (already connected or invalid)")
             return False 
             
        host, port = self.backend_servers[server_index]
        peer_name = f"server {host}:{port} (idx {server_index})"
        print(f"[*] Attempting to connect to backend {peer_name}...")
        try:
            reader, writer = await asyncio.wait_for(
                asyncio.open_connection(host, port),
                timeout=self.health_check_timeout * 2 # Slightly longer timeout for connection
            )
            print(f"[*] Successfully connected to backend {peer_name}")
            # Create and store the reader task
            read_task = asyncio.create_task(self.read_from_server(reader, server_index), 
                                          name=f"ServerRead-{server_index}")
            self.server_connections[server_index] = (reader, writer, read_task)
            return True
        except (asyncio.TimeoutError, ConnectionRefusedError, OSError) as e:
            print(f"[!] Failed to connect to backend {peer_name}: {e}")
            # Don't mark as unhealthy here, health check handles that
            return False
        except Exception as e:
            print(f"[!] Unexpected error connecting to backend {peer_name}: {e}")
            return False

    async def read_from_server(self, server_reader, server_index):
        """Reads responses from a server and forwards to clients."""
        # Ensure server_index is valid before proceeding
        if server_index >= len(self.backend_servers):
             print(f"[!] Invalid server_index {server_index} in read_from_server. Stopping task.")
             return 
             
        host, port = self.backend_servers[server_index]
        peer_name = f"server {host}:{port} (idx {server_index})"
        print(f"[*] Starting reader task for {peer_name}")
        try:
            while True:
                # Check if connection still exists before reading
                if server_index not in self.server_connections:
                    print(f"[*] Connection for {peer_name} no longer exists. Stopping reader task.")
                    break
                    
                len_data = await server_reader.readexactly(4)
                total_msg_len = struct.unpack("!I", len_data)[0]
                
                # Basic sanity check for message length
                if total_msg_len > 10 * 1024 * 1024: # e.g., 10MB limit
                    print(f"[!] Excessive message length ({total_msg_len} bytes) received from {peer_name}. Closing connection.")
                    await self._close_server_connection(server_index)
                    break
                    
                message_data = await server_reader.readexactly(total_msg_len)

                client_id_len = struct.unpack("!B", message_data[:1])[0]
                client_id_end = 1 + client_id_len
                client_id = message_data[1:client_id_end].decode("utf-8")
                server_response_data = message_data[client_id_end:]

                client_info = self.client_connections.get(client_id)
                if client_info:
                    _, client_writer = client_info
                    if client_writer and not client_writer.is_closing():
                        try:
                            client_writer.write(server_response_data)
                            await client_writer.drain()
                        except (ConnectionResetError, BrokenPipeError, OSError) as e:
                            print(f"[!] Error writing to client {client_id}: {e}. Closing client connection.")
                            if client_id in self.client_connections:
                                cr, cw = self.client_connections.pop(client_id)
                                if cw and not cw.is_closing():
                                    cw.close()
                                    # Don't await wait_closed here to avoid blocking server reader
                    else:
                         print(f"[!] Client {client_id} writer is closed or missing. Cannot forward response.")
                else:
                    print(f"[!] Received response for unknown or disconnected client ID: {client_id}")

        except (asyncio.IncompleteReadError, ConnectionResetError, BrokenPipeError, OSError) as e:
            print(f"[*] Connection issue with {peer_name}: {e}. Closing connection.")
            # Schedule closing in the main loop to avoid deadlocks/re-entrancy issues
            asyncio.create_task(self._close_server_connection(server_index))
        except asyncio.CancelledError:
             print(f"[*] Reader task for {peer_name} was cancelled.")
             # Connection should be closed by the canceller (_close_server_connection)
        except Exception as e:
            print(f"[!] Unexpected error reading from {peer_name}: {e}. Closing connection.")
            asyncio.create_task(self._close_server_connection(server_index))
        # finally:
            # print(f"[*] Reader task for {peer_name} finished.") 
            # Cleanup is handled by _close_server_connection or cancellation

    # ... (read_from_client remains mostly the same, but check server connection validity) ...
    async def read_from_client(self, client_reader, client_writer, client_id):
        """Reads requests from a client and forwards to backend servers."""
        client_addr = client_writer.get_extra_info("peername", "unknown client")
        peer_name = f"client {client_id} ({client_addr})"
        print(f"[*] Starting reader task for {peer_name}")
        server_writer = None # Initialize server_writer
        server_index = -1
        try:
            while True:
                client_data = await client_reader.read(4096)
                if not client_data:
                    print(f"[*] {peer_name} disconnected")
                    break

                # Select a healthy and connected server
                selected_server = False
                if self.round_robin_pool:
                    attempts = 0
                    # Use list of current healthy indices to avoid issues if set changes during iteration
                    healthy_now = list(self.healthy_indices)
                    max_attempts = len(healthy_now) 
                    
                    while attempts < max_attempts:
                        attempts += 1
                        try:
                             server_index = next(self.round_robin_pool)
                        except StopIteration: # Should not happen with cycle, but safety check
                             print("[!] Round robin pool exhausted unexpectedly.")
                             self.round_robin_pool = itertools.cycle(sorted(list(self.healthy_indices))) if self.healthy_indices else None
                             if not self.round_robin_pool: break
                             server_index = next(self.round_robin_pool)
                             
                        # Check if the selected index is *still* healthy and connected
                        if server_index in self.healthy_indices and server_index in self.server_connections:
                            _, server_writer, _ = self.server_connections[server_index]
                            if server_writer and not server_writer.is_closing():
                                selected_server = True
                                break # Found a valid server
                            else:
                                 print(f"[!] Server index {server_index} selected but writer is closed/missing.")
                                 # Consider removing from healthy_indices or forcing health check?
                        # else: print(f"[*] Skipping index {server_index} (not healthy or not connected)")
                
                if not selected_server:
                    print(f"[!] No healthy and connected servers available for {peer_name}. Disconnecting client.")
                    break # Break client loop if no server available

                # Wrap and send data
                client_id_bytes = client_id.encode("utf-8")
                client_id_len = len(client_id_bytes)
                message_payload = struct.pack("!B", client_id_len) + client_id_bytes + client_data
                wrapped_message = struct.pack("!I", len(message_payload)) + message_payload

                try:
                    server_writer.write(wrapped_message)
                    await server_writer.drain()
                    # print(f"[*] Forwarded data from {peer_name} to server idx {server_index}")
                except (ConnectionResetError, BrokenPipeError, OSError) as e:
                    print(f"[!] Error writing to server idx {server_index}: {e}. Closing server connection.")
                    # Schedule closing in the main loop
                    asyncio.create_task(self._close_server_connection(server_index))
                    # Need to break or retry finding another server? For now, break client loop.
                    print(f"[!] Disconnecting {peer_name} due to server write error.")
                    break 
                except Exception as e:
                     print(f"[!] Unexpected error writing to server idx {server_index}: {e}")
                     asyncio.create_task(self._close_server_connection(server_index))
                     print(f"[!] Disconnecting {peer_name} due to unexpected server write error.")
                     break

        except (asyncio.IncompleteReadError, ConnectionResetError, BrokenPipeError, OSError) as e:
            print(f"[*] Connection issue with {peer_name}: {e}")
        except asyncio.CancelledError:
             print(f"[*] Client reader task for {peer_name} cancelled.")
        except Exception as e:
            print(f"[!] Unexpected error reading from {peer_name}: {e}")
        finally:
            print(f"[*] Stopping reader task for {peer_name}")
            if client_id in self.client_connections:
                _, cw = self.client_connections.pop(client_id)
                if cw and not cw.is_closing():
                    cw.close()
                    # Don't await wait_closed here

    # ... (handle_client_connection remains the same) ...
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

    # Simplify initialize - let update_servers handle initial connections
    async def initialize(self):
        """Initializes the load balancer."""
        print("[*] Initializing Load Balancer...")
        await self.update_servers()  # Load, health check, and connect

        # Start filesystem watcher
        print("[*] Starting filesystem watcher for servers.json")
        event_handler = ServerListHandler(self)
        self.observer = Observer()
        # Watch the directory containing servers.json
        watch_dir = os.path.dirname(servers_path)
        self.observer.schedule(event_handler, path=watch_dir, recursive=False)
        self.observer.start()
        print(f"[*] Watching directory: {watch_dir}")

# ... (main function remains the same) ...
async def main():
    parser = argparse.ArgumentParser(description="Async TCP Load Balancer")
    parser.add_argument("--lb", type=str, default="127.0.0.1", help="Load Balancer host")
    parser.add_argument("--port", type=int, default=8000, help="Load Balancer port")
    parser.add_argument("--health-check-timeout", type=float, default=1.0, 
                       help="Health check timeout in seconds")
    args = parser.parse_args()

    lb = LoadBalancer()
    lb.health_check_timeout = args.health_check_timeout
    
    # Ensure servers.json directory exists
    servers_dir = os.path.dirname(servers_path)
    if not os.path.exists(servers_dir):
        os.makedirs(servers_dir)
        print(f"[*] Created directory: {servers_dir}")
        
    # Create initial servers.json if it doesn't exist
    if not os.path.exists(servers_path):
        with open(servers_path, "w") as f:
            json.dump([], f)
            print(f"[*] Created initial empty servers.json at {servers_path}")

    await lb.initialize()

    server = None
    try:
        server = await asyncio.start_server(
            lb.handle_client_connection, args.lb, args.port
        )
        addr = server.sockets[0].getsockname()
        print(f"[*] Load Balancer listening on {addr}")
        
        async with server:
            await server.serve_forever()
            
    except OSError as e:
         print(f"[!] Error starting LB server on {args.lb}:{args.port}: {e}")
    except Exception as e:
        print(f"[!] Critical error in LB main loop: {e}")
    finally:
        print("[*] Shutting down Load Balancer...")
        if lb.observer:
            lb.observer.stop()
            lb.observer.join()
            print("[*] Filesystem watcher stopped.")
            
        # Close all client connections
        client_close_tasks = []
        for client_id, (cr, cw) in list(lb.client_connections.items()):
             print(f"[*] Closing client connection {client_id}")
             if cw and not cw.is_closing():
                  cw.close()
                  # Don't await here to avoid blocking shutdown
        lb.client_connections.clear()
        
        # Close all server connections
        server_close_tasks = []
        for server_index in list(lb.server_connections.keys()):
             server_close_tasks.append(lb._close_server_connection(server_index))
        if server_close_tasks:
             print("[*] Waiting for server connections to close...")
             await asyncio.gather(*server_close_tasks, return_exceptions=True)
             print("[*] All server connections closed.")
             
        if server and server.is_serving():
             server.close()
             await server.wait_closed()
             print("[*] LB server socket closed.")
        print("[*] Load Balancer shutdown complete.")

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n[*] KeyboardInterrupt received.")
    except Exception as e:
        print(f"[!] Critical error running LB: {e}", file=sys.stderr)
