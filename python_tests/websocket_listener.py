import asyncio
import websockets
from datetime import datetime
import sys
import os
import json

LOG_FILE = "websocket_messages.log"

def write_log(message):
    """Write message to both console and log file."""
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    formatted_message = f"{timestamp} - {message}\n"

    # Print to console
    print(formatted_message.strip())

    # Append to log file
    try:
        with open(LOG_FILE, "a", encoding="utf-8") as f:
            f.write(formatted_message)
            f.flush()
    except Exception as e:
        print(f"{timestamp} - Error writing to log file: {e}")

# SignalR hub URL with JWT token
SIGNALR_URL = (
    "wss://test.scorptech.co/hubs/notifications"
    "?access_token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIyYzhlZjE0Yi04MDM4LTQ4NDEtOGE0MS0xMzEyMzZjNTUwODIiLCJ1bmlxdWVfbmFtZSI6ImpvaG5kb2UiLCJnaXZlbl9uYW1lIjoiSm9obiIsImZhbWlseV9uYW1lIjoiRG9lIiwicm9sZSI6ImF1ZGl0b3IiLCJlbWFpbCI6ImpvaG5kb2VAZXhhbXBsZS5jb20iLCJvcmdhbmlzYXRpb25faWQiOiI4NWU3NDMzNi04M2MwLTQ3MWEtYWM5ZC1lOWQwOWQ3MjU2ZTQiLCJuYmYiOjE3NTIwNDIwMzUsImV4cCI6MTc1MjA3MDgzNSwiaWF0IjoxNzUyMDQyMDM1LCJpc3MiOiJBdWRpdFN5c3RlbSIsImF1ZCI6IkF1ZGl0U3lzdGVtQ2xpZW50cyJ9.SDdQvAXVJ9Uokvz9TzcnqhXtCavfzp-HJ76d3M0PCw0"
)

# Handshake frame you need to send to start the SignalR protocol
HANDSHAKE = '{"protocol":"json","version":1}\u001e'

# User ID from the JWT token
USER_ID = "2c8ef14b-8038-4841-8a41-131236c55082"

async def listen_forever():
    """Keep trying to connect, and listen for raw messages."""
    reconnect_delay = 2  # seconds
    while True:
        write_log(f"Attempting connection to SignalR hub: {SIGNALR_URL}")
        try:
            async with websockets.connect(SIGNALR_URL) as ws:
                write_log("WebSocket connection established")
                
                # Send the initial handshake
                write_log(f"Sending handshake frame: {HANDSHAKE!r}")
                await ws.send(HANDSHAKE)
                write_log("Handshake sent")

                # Wait for handshake response
                handshake_response = await asyncio.wait_for(ws.recv(), timeout=5.0)
                write_log(f"Handshake response: {handshake_response}")

                # Subscribe to user notifications
                subscribe_message = {
                    "type": 1,
                    "target": "SubscribeToUser",
                    "arguments": [USER_ID]
                }
                subscribe_frame = json.dumps(subscribe_message) + '\u001e'
                write_log(f"Sending subscribe message: {subscribe_frame}")
                await ws.send(subscribe_frame)
                write_log("Subscribe message sent")

                message_count = 0
                while True:
                    try:
                        raw = await asyncio.wait_for(ws.recv(), timeout=30.0)
                        message_count += 1
                        write_log(f"=== MESSAGE #{message_count} ===")
                        write_log(f"Raw message: {raw}")
                        write_log(f"=== END MESSAGE #{message_count} ===\n")
                    except asyncio.TimeoutError:
                        write_log("No message for 30s — still listening...")
                    except websockets.ConnectionClosed:
                        write_log("Connection closed by server")
                        break

        except Exception as e:
            write_log(f"Connection error: {e}")

        write_log(f"Reconnecting in {reconnect_delay} seconds...")
        await asyncio.sleep(reconnect_delay)
        # Optional: increase delay gradually up to a max
        reconnect_delay = min(reconnect_delay * 1.5, 30)

async def main():
    write_log("Starting persistent WebSocket listener for SignalR notifications")
    write_log("Press Ctrl+C to exit")
    write_log(f"Logging to: {os.path.abspath(LOG_FILE)}")
    try:
        await listen_forever()
    except KeyboardInterrupt:
        write_log("Interrupted by user — shutting down")

if __name__ == "__main__":
    asyncio.run(main())
