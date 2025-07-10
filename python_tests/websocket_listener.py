import asyncio
import websockets
from datetime import datetime
import sys
import os
import json
import uuid

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
    "ws://localhost:8080/hubs/notifications"
    "?access_token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIyYzhlZjE0Yi04MDM4LTQ4NDEtOGE0MS0xMzEyMzZjNTUwODIiLCJ1bmlxdWVfbmFtZSI6ImpvaG5kb2UiLCJnaXZlbl9uYW1lIjoiSm9obiIsImZhbWlseV9uYW1lIjoiRG9lIiwicm9sZSI6ImF1ZGl0b3IiLCJlbWFpbCI6ImpvaG5kb2VAZXhhbXBsZS5jb20iLCJvcmdhbmlzYXRpb25faWQiOiI4NWU3NDMzNi04M2MwLTQ3MWEtYWM5ZC1lOWQwOWQ3MjU2ZTQiLCJuYmYiOjE3NTIxMjI1ODUsImV4cCI6MTc1MjE1MTM4NSwiaWF0IjoxNzUyMTIyNTg1LCJpc3MiOiJBdWRpdFN5c3RlbSIsImF1ZCI6IkF1ZGl0U3lzdGVtQ2xpZW50cyJ9.klCluTBp9r8p9G_k7kJ0jYkEPeIHwdjLcVB1JBVnGzs"
)

# Handshake frame you need to send to start the SignalR protocol
HANDSHAKE = '{"protocol":"json","version":1}\u001e'

# User ID from the JWT token
USER_ID = "2c8ef14b-8038-4841-8a41-131236c55082"

async def acknowledge_delivery(ws, notification_id):
    """Acknowledge delivery of a notification"""
    message = {
        "type": 1,
        "target": "AcknowledgeDelivery",
        "arguments": [notification_id],
        "invocationId": str(uuid.uuid4())
    }
    await ws.send(json.dumps(message) + '\u001e')
    write_log(f"Sent delivery acknowledgment for notification: {notification_id}")

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
                        
                        # Try to parse as JSON
                        try:
                            # Remove the record separator character if present
                            clean_message = raw.rstrip('\u001e')
                            if clean_message:
                                parsed = json.loads(clean_message)
                                write_log(f"Parsed JSON: {json.dumps(parsed, indent=2)}")
                                
                                # Check for notification messages
                                if isinstance(parsed, dict):
                                    if parsed.get('type') == 1 and parsed.get('target') == 'ReceiveNotification':
                                        write_log("*** NOTIFICATION RECEIVED ***")
                                        if 'arguments' in parsed and len(parsed['arguments']) > 0:
                                            notification = parsed['arguments'][0]
                                            notification_id = notification.get('notificationId')
                                            write_log(f"Notification ID: {notification_id}")
                                            write_log(f"Title: {notification.get('title')}")
                                            write_log(f"Message: {notification.get('message')}")
                                            write_log(f"Type: {notification.get('type')}")
                                            write_log(f"Priority: {notification.get('priority')}")
                                            
                                            # Automatically acknowledge delivery
                                            if notification_id:
                                                await acknowledge_delivery(ws, notification_id)
                                                write_log(f"*** DELIVERY ACKNOWLEDGED FOR: {notification_id} ***")
                                    elif parsed.get('type') == 1 and parsed.get('target') == 'UnreadCount':
                                        write_log(f"*** UNREAD COUNT UPDATE: {parsed.get('arguments', [0])[0]} ***")
                                    elif parsed.get('type') == 1 and parsed.get('target') == 'Heartbeat':
                                        write_log(f"*** HEARTBEAT: {parsed.get('arguments', [{}])[0].get('timestamp')} ***")
                                    elif parsed.get('type') == 1 and parsed.get('target') == 'DeliveryAcknowledged':
                                        write_log(f"*** DELIVERY ACKNOWLEDGMENT CONFIRMED: {parsed.get('arguments', [{}])[0].get('notificationId')} ***")
                            else:
                                write_log("Empty message received")
                        except json.JSONDecodeError as e:
                            write_log(f"Message is not valid JSON: {e}")
                        except Exception as e:
                            write_log(f"Error parsing message: {e}")
                        
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
