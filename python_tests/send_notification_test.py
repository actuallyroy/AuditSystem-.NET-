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

def send_system_alert():
    """Send a system alert notification to test WebSocket connection"""
    notification_data = {
        "title": "WebSocket Test Notification",
        "message": f"This is a test notification sent at {datetime.now().isoformat()} to verify WebSocket connection",
        "priority": "medium"
    }
    
    try:
        logger.info("Sending system alert notification...")
        response = requests.post(
            f"{BASE_URL}/notifications/system-alert",
            headers=headers,
            json=notification_data
        )
        
        if response.status_code == 200:
            logger.info("System alert notification sent successfully")
            logger.info(f"Response: {response.json()}")
            return True
        else:
            logger.error(f"Failed to send system alert notification: {response.status_code}")
            logger.error(f"Response: {response.text}")
            return False
            
    except Exception as e:
        logger.error(f"Error sending system alert notification: {e}")
        return False

def send_assignment_notification():
    """Create an assignment to trigger assignment notification"""
    assignment_data = {
        "title": "WebSocket Test Assignment",
        "description": f"This is a test assignment created at {datetime.now().isoformat()} to verify WebSocket notifications",
        "assignedUserId": "2c8ef14b-8038-4841-8a41-131236c55082",  # john doe's user ID
        "dueDate": "2024-12-31T23:59:59Z",
        "priority": "medium",
        "status": "pending"
    }
    
    try:
        logger.info("Creating assignment to trigger notification...")
        response = requests.post(
            f"{BASE_URL}/assignments",
            headers=headers,
            json=assignment_data
        )
        
        if response.status_code == 201:
            logger.info("Assignment created successfully")
            logger.info(f"Response: {response.json()}")
            return True
        else:
            logger.error(f"Failed to create assignment: {response.status_code}")
            logger.error(f"Response: {response.text}")
            return False
            
    except Exception as e:
        logger.error(f"Error creating assignment: {e}")
        return False

def send_multiple_notifications():
    """Send multiple system alert notifications with different priorities"""
    notifications = [
        {
            "title": "Test Notification 1",
            "message": "First test notification for WebSocket verification",
            "priority": "low"
        },
        {
            "title": "Test Notification 2", 
            "message": "Second test notification for WebSocket verification",
            "priority": "medium"
        },
        {
            "title": "Test Notification 3",
            "message": "Third test notification for WebSocket verification", 
            "priority": "high"
        }
    ]
    
    success_count = 0
    
    for i, notification in enumerate(notifications, 1):
        try:
            logger.info(f"Sending notification {i}...")
            response = requests.post(
                f"{BASE_URL}/notifications/system-alert",
                headers=headers,
                json=notification
            )
            
            if response.status_code == 200:
                logger.info(f"Notification {i} sent successfully")
                success_count += 1
            else:
                logger.error(f"Failed to send notification {i}: {response.status_code}")
                logger.error(f"Response: {response.text}")
                
        except Exception as e:
            logger.error(f"Error sending notification {i}: {e}")
    
    logger.info(f"Sent {success_count}/{len(notifications)} notifications successfully")
    return success_count

def main():
    """Main function to run notification tests"""
    logger.info("=== WebSocket Notification Test ===")
    logger.info("This script will send notifications to test the WebSocket connection")
    logger.info("Make sure the websocket_listener.py is running in another terminal")
    
    # Wait a moment for user to start the listener
    logger.info("Waiting 3 seconds before sending notifications...")
    time.sleep(3)
    
    # Test 1: Send system alert notification
    logger.info("\n--- Test 1: System Alert Notification ---")
    send_system_alert()
    
    # Wait between tests
    time.sleep(2)
    
    # Test 2: Send multiple notifications
    logger.info("\n--- Test 2: Multiple Notifications ---")
    send_multiple_notifications()
    
    # Wait between tests
    time.sleep(2)
    
    # Test 3: Create assignment to trigger assignment notification
    logger.info("\n--- Test 3: Assignment Notification ---")
    send_assignment_notification()
    
    logger.info("\n=== Test Complete ===")
    logger.info("Check the websocket_messages.log file to see if notifications were received")

if __name__ == "__main__":
    main() 