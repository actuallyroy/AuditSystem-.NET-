import asyncio
import websockets
import json
import logging
import requests
import time
import threading
from datetime import datetime
import sys

# Configure logging to write to both console and file
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('websocket_test.log', mode='w'),
        logging.StreamHandler(sys.stdout)
    ]
)

logger = logging.getLogger(__name__)

# Configuration
SIGNALR_URL = "wss://test.scorptech.co/hubs/notifications?access_token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIyYzhlZjE0Yi04MDM4LTQ4NDEtOGE0MS0xMzEyMzZjNTUwODIiLCJ1bmlxdWVfbmFtZSI6ImpvaG5kb2UiLCJnaXZlbl9uYW1lIjoiSm9obiIsImZhbWlseV9uYW1lIjoiRG9lIiwicm9sZSI6ImF1ZGl0b3IiLCJlbWFpbCI6ImpvaG5kb2VAZXhhbXBsZS5jb20iLCJvcmdhbmlzYXRpb25faWQiOiI4NWU3NDMzNi04M2MwLTQ3MWEtYWM5ZC1lOWQwOWQ3MjU2ZTQiLCJuYmYiOjE3NTIwMzY3NDEsImV4cCI6MTc1MjA2NTU0MSwiaWF0IjoxNzUyMDM2NzQxLCJpc3MiOiJBdWRpdFN5c3RlbSIsImF1ZCI6IkF1ZGl0U3lzdGVtQ2xpZW50cyJ9.cXq6JMS2CMdmUo78XBblXuCa0plL4eb0PY7LuV6Zk50"
API_BASE_URL = "https://test.scorptech.co/api/v1"
JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIyYzhlZjE0Yi04MDM4LTQ4NDEtOGE0MS0xMzEyMzZjNTUwODIiLCJ1bmlxdWVfbmFtZSI6ImpvaG5kb2UiLCJnaXZlbl9uYW1lIjoiSm9obiIsImZhbWlseV9uYW1lIjoiRG9lIiwicm9sZSI6ImF1ZGl0b3IiLCJlbWFpbCI6ImpvaG5kb2VAZXhhbXBsZS5jb20iLCJvcmdhbmlzYXRpb25faWQiOiI4NWU3NDMzNi04M2MwLTQ3MWEtYWM5ZC1lOWQwOWQ3MjU2ZTQiLCJuYmYiOjE3NTIwMzY3NDEsImV4cCI6MTc1MjA2NTU0MSwiaWF0IjoxNzUyMDM2NzQxLCJpc3MiOiJBdWRpdFN5c3RlbSIsImF1ZCI6IkF1ZGl0U3lzdGVtQ2xpZW50cyJ9.cXq6JMS2CMdmUo78XBblXuCa0plL4eb0PY7LuV6Zk50"

# Global variables
websocket_connected = False
messages_received = []
test_completed = False

async def websocket_listener():
    """Connect to SignalR hub via WebSocket and log all messages"""
    global websocket_connected, messages_received
    
    try:
        logger.info(f"Connecting to SignalR hub: {SIGNALR_URL}")
        
        # Connect to the WebSocket
        async with websockets.connect(SIGNALR_URL) as websocket:
            websocket_connected = True
            logger.info("WebSocket connection established successfully")
            
            # Send handshake message
            handshake_message = {
                "protocol": "json",
                "version": 1
            }
            
            logger.info(f"Sending handshake: {json.dumps(handshake_message)}")
            await websocket.send(json.dumps(handshake_message))
            logger.info("Handshake sent successfully")
            
            # Listen for messages
            logger.info("Starting to listen for messages...")
            message_count = 0
            
            while not test_completed:
                try:
                    # Receive message with timeout
                    message = await asyncio.wait_for(websocket.recv(), timeout=10.0)
                    message_count += 1
                    
                    # Store message
                    messages_received.append({
                        'count': message_count,
                        'timestamp': datetime.now().isoformat(),
                        'raw_message': message
                    })
                    
                    # Log the raw message
                    logger.info(f"=== MESSAGE #{message_count} ===")
                    logger.info(f"Raw message: {message}")
                    
                    # Try to parse as JSON for better formatting
                    try:
                        parsed_message = json.loads(message)
                        logger.info(f"Parsed JSON: {json.dumps(parsed_message, indent=2)}")
                        messages_received[-1]['parsed_message'] = parsed_message
                    except json.JSONDecodeError:
                        logger.info("Message is not valid JSON")
                    
                    logger.info(f"=== END MESSAGE #{message_count} ===\n")
                    
                except asyncio.TimeoutError:
                    # Continue listening
                    continue
                except websockets.exceptions.ConnectionClosed:
                    logger.error("WebSocket connection closed")
                    break
                except Exception as e:
                    logger.error(f"Error receiving message: {e}")
                    break
                    
    except websockets.exceptions.InvalidURI:
        logger.error("Invalid WebSocket URI")
    except websockets.exceptions.ConnectionClosedError as e:
        logger.error(f"Connection closed error: {e}")
    except Exception as e:
        logger.error(f"Unexpected error: {e}")
    finally:
        websocket_connected = False

def send_notifications():
    """Send notifications via REST API"""
    headers = {
        "Authorization": f"Bearer {JWT_TOKEN}",
        "Content-Type": "application/json"
    }
    
    # Wait for WebSocket to connect
    time.sleep(3)
    
    if not websocket_connected:
        logger.error("WebSocket not connected, cannot send notifications")
        return
    
    logger.info("=== Sending Test Notifications ===")
    
    # Test 1: System alert notification
    notification_data = {
        "title": "WebSocket Test Notification",
        "message": f"This is a test notification sent at {datetime.now().isoformat()} to verify WebSocket connection",
        "priority": "medium"
    }
    
    try:
        logger.info("Sending system alert notification...")
        response = requests.post(
            f"{API_BASE_URL}/notifications/system-alert",
            headers=headers,
            json=notification_data
        )
        
        if response.status_code == 200:
            logger.info("System alert notification sent successfully")
        else:
            logger.error(f"Failed to send system alert notification: {response.status_code}")
            logger.error(f"Response: {response.text}")
            
    except Exception as e:
        logger.error(f"Error sending system alert notification: {e}")
    
    # Wait for message to be received
    time.sleep(2)
    
    # Test 2: Assignment notification
    assignment_data = {
        "title": "WebSocket Test Assignment",
        "description": f"This is a test assignment created at {datetime.now().isoformat()} to verify WebSocket notifications",
        "assignedUserId": "2c8ef14b-8038-4841-8a41-131236c55082",
        "dueDate": "2024-12-31T23:59:59Z",
        "priority": "medium",
        "status": "pending"
    }
    
    try:
        logger.info("Creating assignment to trigger notification...")
        response = requests.post(
            f"{API_BASE_URL}/assignments",
            headers=headers,
            json=assignment_data
        )
        
        if response.status_code == 201:
            logger.info("Assignment created successfully")
        else:
            logger.error(f"Failed to create assignment: {response.status_code}")
            logger.error(f"Response: {response.text}")
            
    except Exception as e:
        logger.error(f"Error creating assignment: {e}")
    
    # Wait for message to be received
    time.sleep(2)
    
    # Test 3: Multiple system alerts
    notifications = [
        {"title": "Test 1", "message": "First test notification", "priority": "low"},
        {"title": "Test 2", "message": "Second test notification", "priority": "medium"},
        {"title": "Test 3", "message": "Third test notification", "priority": "high"}
    ]
    
    for i, notification in enumerate(notifications, 1):
        try:
            logger.info(f"Sending notification {i}...")
            response = requests.post(
                f"{API_BASE_URL}/notifications/system-alert",
                headers=headers,
                json=notification
            )
            
            if response.status_code == 200:
                logger.info(f"Notification {i} sent successfully")
            else:
                logger.error(f"Failed to send notification {i}: {response.status_code}")
                logger.error(f"Response: {response.text}")
                
        except Exception as e:
            logger.error(f"Error sending notification {i}: {e}")
        
        time.sleep(1)
    
    # Wait for final messages
    time.sleep(3)
    
    # Mark test as completed
    global test_completed
    test_completed = True
    
    # Print summary
    logger.info("\n=== TEST SUMMARY ===")
    logger.info(f"WebSocket connected: {'YES' if websocket_connected else 'NO'}")
    logger.info(f"Messages received: {len(messages_received)}")
    
    if messages_received:
        logger.info("Received messages:")
        for msg in messages_received:
            logger.info(f"  - Message #{msg['count']} at {msg['timestamp']}")
            if 'parsed_message' in msg:
                logger.info(f"    Type: {msg['parsed_message'].get('type', 'unknown')}")
    else:
        logger.warning("No messages received via WebSocket")

async def main():
    """Main function to run the complete test"""
    logger.info("=== WebSocket Notification Test ===")
    logger.info("This test will:")
    logger.info("1. Connect to SignalR hub via WebSocket")
    logger.info("2. Send notifications via REST API")
    logger.info("3. Verify messages are received via WebSocket")
    
    # Start WebSocket listener
    websocket_task = asyncio.create_task(websocket_listener())
    
    # Start notification sender in a separate thread
    notification_thread = threading.Thread(target=send_notifications)
    notification_thread.start()
    
    # Wait for both to complete
    await websocket_task
    notification_thread.join()
    
    logger.info("=== Test Complete ===")
    logger.info("Check websocket_test.log for detailed logs")

if __name__ == "__main__":
    asyncio.run(main()) 