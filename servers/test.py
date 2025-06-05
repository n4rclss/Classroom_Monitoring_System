#!/usr/bin/env python3
import asyncio
import json
import argparse
import sys

async def send_login_request(host, port, username, password, role):
    """Connects to the load balancer, sends a login request, and prints the response."""
    try:
        reader, writer = await asyncio.open_connection(host, port)
        peername = writer.get_extra_info("peername")
        print(f"[*] Connected to Load Balancer at {peername}")

        # Prepare login packet (as expected by the server)
        login_payload = {
            "type": "login",
            "username": username,
            "password": password,
            "role": role
        }
        login_data = json.dumps(login_payload).encode("utf-8")

        # Send the data (LB will wrap it)
        print(f"[*] Sending login request: {login_payload}")
        writer.write(login_data)
        await writer.drain()

        # Read the response (LB unwraps it)
        print("[*] Waiting for response...")
        response_data = await reader.read(4096) # Read up to 4KB

        if response_data:
            try:
                response_json = json.loads(response_data.decode("utf-8"))
                print(f"[*] Received response: {response_json}")
            except json.JSONDecodeError:
                print(f"[!] Received non-JSON response: {response_data.decode('utf-8', errors='ignore')}")
            except Exception as e:
                print(f"[!] Error decoding response: {e}")
        else:
            print("[!] No response received from server/LB.")

    except ConnectionRefusedError:
        print(f"[!] Connection refused. Is the Load Balancer running at {host}:{port}?")
    except asyncio.TimeoutError:
        print("[!] Connection timed out.")
    except Exception as e:
        print(f"[!] An error occurred: {e}")
    finally:
        if 'writer' in locals() and writer:
            print("[*] Closing connection.")
            writer.close()
            try:
                await writer.wait_closed()
            except Exception as close_e:
                print(f"[!] Error closing writer: {close_e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Test Client for Load Balancer Login")
    parser.add_argument("--lb-host", type=str, default="127.0.0.1", help="Load Balancer host (default: 127.0.0.1)")
    parser.add_argument("--lb-port", type=int, default=8000, help="Load Balancer port (default: 8000)")
    parser.add_argument("--username", type=str, required=True, help="Username for login")
    parser.add_argument("--password", type=str, required=True, help="Password for login")
    parser.add_argument("--role", type=str, choices=["teacher", "student"], required=True, help="Role for login (teacher or student)")

    args = parser.parse_args()

    asyncio.run(send_login_request(args.lb_host, args.lb_port, args.username, args.password, args.role))

