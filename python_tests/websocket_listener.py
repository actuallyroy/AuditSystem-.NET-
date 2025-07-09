import asyncio
import websockets
import json
import logging
from datetime import datetime
import sys

# Configure logging to write to both console and file
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('websocket_messages.log', mode='w'),
        logging.StreamHandler(sys.stdout)
    ]
)

logger = logging.getLogger(__name__)

# SignalR hub URL with JWT token
SIGNALR_URL = "wss://test.scorptech.co/hubs/notifications?access_token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIyYzhlZjE0Yi04MDM4LTQ4NDEtOGE0MS0xMzEyMzZjNTUwODIiLCJ1bmlxdWVfbmFtZSI6ImpvaG5kb2UiLCJnaXZlbl9uYW1lIjoiSm9obiIsImZhbWlseV9uYW1lIjoiRG9lIiwicm9sZSI6ImF1ZGl0b3IiLCJlbWFpbCI6ImpvaG5kb2VAZXhhbXBsZS5jb20iLCJvcmdhbmlzYXRpb25faWQiOiI4NWU3NDMzNi04M2MwLTQ3MWEtYWM5ZC1lOWQwOWQ3MjU2ZTQiLCJuYmYiOjE3NTIwMzY3NDEsImV4cCI6MTc1MjA2NTU0MSwiaWF0IjoxNzUyMDM2NzQxLCJpc3MiOiJBdWRpdFN5c3RlbSIsImF1ZCI6IkF1ZGl0U3lzdGVtQ2xpZW50cyJ9.cXq6JMS2CMdmUo78XBblXuCa0plL4eb0PY7LuV6Zk50"

async def websocket_listener():
    """Connect to SignalR hub via WebSocket and log all messages"""
    try:
        logger.info(f"Connecting to SignalR hub: {SIGNALR_URL}")
        
        # Connect to the WebSocket
        async with websockets.connect(SIGNALR_URL) as websocket:
            logger.info("WebSocket connection established successfully")
            
            # Send handshake message
            handshake_message = '{"protocol": "json", "version": 1}'
            
            logger.info(f"Sending handshake: {handshake_message}")
            await websocket.send(handshake_message)
            logger.info("Handshake sent successfully")
            
            # Listen for messages
            logger.info("Starting to listen for messages...")
            message_count = 0
            
            while True:
                try:
                    # Receive message with timeout
                    message = await asyncio.wait_for(websocket.recv(), timeout=30.0)
                    message_count += 1
                    
                    # Log the raw message
                    logger.info(f"=== MESSAGE #{message_count} ===")
                    logger.info(f"Raw message: {message}")
                    
                    # Try to parse as JSON for better formatting
                    try:
                        parsed_message = json.loads(message)
                        logger.info(f"Parsed JSON: {json.dumps(parsed_message, indent=2)}")
                    except json.JSONDecodeError:
                        logger.info("Message is not valid JSON")
                    
                    logger.info(f"=== END MESSAGE #{message_count} ===\n")
                    
                except asyncio.TimeoutError:
                    logger.info("No message received in 30 seconds, continuing to listen...")
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

async def main():
    """Main function to run the WebSocket listener"""
    logger.info("Starting WebSocket listener for SignalR notifications")
    logger.info("Press Ctrl+C to stop")
    
    try:
        await websocket_listener()
    except KeyboardInterrupt:
        logger.info("Received interrupt signal, shutting down...")
    except Exception as e:
        logger.error(f"Main error: {e}")

if __name__ == "__main__":
    asyncio.run(main()) 