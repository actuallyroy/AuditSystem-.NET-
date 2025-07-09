import requests
import json
import time
import logging
from datetime import datetime

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)

logger = logging.getLogger(__name__)

# API Configuration
BASE_URL = "https://test.scorptech.co/api/v1"
JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIyYzhlZjE0Yi04MDM4LTQ4NDEtOGE0MS0xMzEyMzZjNTUwODIiLCJ1bmlxdWVfbmFtZSI6ImpvaG5kb2UiLCJnaXZlbl9uYW1lIjoiSm9obiIsImZhbWlseV9uYW1lIjoiRG9lIiwicm9sZSI6ImF1ZGl0b3IiLCJlbWFpbCI6ImpvaG5kb2VAZXhhbXBsZS5jb20iLCJvcmdhbmlzYXRpb25faWQiOiI4NWU3NDMzNi04M2MwLTQ3MWEtYWM5ZC1lOWQwOWQ3MjU2ZTQiLCJuYmYiOjE3NTIwMzY3NDEsImV4cCI6MTc1MjA2NTU0MSwiaWF0IjoxNzUyMDM2NzQxLCJpc3MiOiJBdWRpdFN5c3RlbSIsImF1ZCI6IkF1ZGl0U3lzdGVtQ2xpZW50cyJ9.cXq6JMS2CMdmUo78XBblXuCa0plL4eb0PY7LuV6Zk50"

# Headers for API requests
headers = {
    "Authorization": f"Bearer {JWT_TOKEN}",
    "Content-Type": "application/json"
}

def test_auditor_endpoints():
    """Test endpoints that auditors can access"""
    logger.info("=== Testing Auditor-Accessible Endpoints ===")
    
    # Test 1: Get user's own notifications
    try:
        logger.info("Testing: Get user notifications")
        response = requests.get(f"{BASE_URL}/notifications", headers=headers)
        if response.status_code == 200:
            data = response.json()
            logger.info(f"Success: Retrieved {len(data.get('notifications', []))} notifications")
        else:
            logger.error(f"Failed: {response.status_code} - {response.text}")
    except Exception as e:
        logger.error(f"Error: {e}")
    
    # Test 2: Get unread count
    try:
        logger.info("Testing: Get unread count")
        response = requests.get(f"{BASE_URL}/notifications/unread-count", headers=headers)
        if response.status_code == 200:
            data = response.json()
            logger.info(f"Success: {data.get('unreadCount', 0)} unread notifications")
        else:
            logger.error(f"Failed: {response.status_code} - {response.text}")
    except Exception as e:
        logger.error(f"Error: {e}")
    
    # Test 3: Get assignments (auditors can view their assignments)
    try:
        logger.info("Testing: Get assignments")
        response = requests.get(f"{BASE_URL}/assignments", headers=headers)
        if response.status_code == 200:
            data = response.json()
            logger.info(f"Success: Retrieved {len(data)} assignments")
        else:
            logger.error(f"Failed: {response.status_code} - {response.text}")
    except Exception as e:
        logger.error(f"Error: {e}")
    
    # Test 4: Get audits (auditors can view audits)
    try:
        logger.info("Testing: Get audits")
        response = requests.get(f"{BASE_URL}/audits", headers=headers)
        if response.status_code == 200:
            data = response.json()
            logger.info(f"Success: Retrieved {len(data)} audits")
        else:
            logger.error(f"Failed: {response.status_code} - {response.text}")
    except Exception as e:
        logger.error(f"Error: {e}")

def test_mark_notifications_read():
    """Test marking notifications as read to trigger potential notifications"""
    logger.info("=== Testing Notification Management ===")
    
    # First get notifications
    try:
        response = requests.get(f"{BASE_URL}/notifications", headers=headers)
        if response.status_code == 200:
            data = response.json()
            notifications = data.get('notifications', [])
            
            if notifications:
                # Mark first notification as read
                first_notification = notifications[0]
                notification_id = first_notification.get('notificationId')
                
                logger.info(f"Marking notification {notification_id} as read")
                response = requests.put(f"{BASE_URL}/notifications/{notification_id}/read", headers=headers)
                if response.status_code == 200:
                    logger.info("Success: Notification marked as read")
                else:
                    logger.error(f"Failed to mark as read: {response.status_code} - {response.text}")
            else:
                logger.info("No notifications to mark as read")
        else:
            logger.error(f"Failed to get notifications: {response.status_code}")
    except Exception as e:
        logger.error(f"Error: {e}")

def test_websocket_connection():
    """Test WebSocket connection with better error handling"""
    import websockets
    import asyncio
    
    logger.info("=== Testing WebSocket Connection ===")
    
    async def test_connection():
        try:
            logger.info("Attempting WebSocket connection...")
            async with websockets.connect("wss://test.scorptech.co/hubs/notifications?access_token=" + JWT_TOKEN) as websocket:
                logger.info("WebSocket connected successfully")
                
                # Send handshake
                handshake = {"protocol": "json", "version": 1}
                await websocket.send(json.dumps(handshake))
                logger.info("Handshake sent")
                
                # Wait for response
                try:
                    response = await asyncio.wait_for(websocket.recv(), timeout=5.0)
                    logger.info(f"Received response: {response}")
                    
                    # Try to parse response
                    try:
                        parsed = json.loads(response)
                        if "error" in parsed:
                            logger.error(f"Handshake error: {parsed['error']}")
                        else:
                            logger.info("Handshake successful")
                    except:
                        logger.info("Non-JSON response received")
                        
                except asyncio.TimeoutError:
                    logger.error("No response received within 5 seconds")
                    
        except Exception as e:
            logger.error(f"WebSocket connection failed: {e}")
    
    # Run the async test
    asyncio.run(test_connection())

def main():
    """Main function"""
    logger.info("=== Auditor Permission Test ===")
    logger.info("Testing endpoints accessible to auditor role")
    
    # Test basic endpoints
    test_auditor_endpoints()
    
    # Test notification management
    test_mark_notifications_read()
    
    # Test WebSocket connection
    test_websocket_connection()
    
    logger.info("=== Test Complete ===")

if __name__ == "__main__":
    main() 