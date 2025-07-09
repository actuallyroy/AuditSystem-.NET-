import requests
import json
import jwt
from datetime import datetime

# Configuration
API_BASE_URL = "https://test.scorptech.co/api/v1"
JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIyYzhlZjE0Yi04MDM4LTQ4NDEtOGE0MS0xMzEyMzZjNTUwODIiLCJ1bmlxdWVfbmFtZSI6ImpvaG5kb2UiLCJnaXZlbl9uYW1lIjoiSm9obiIsImZhbWlseV9uYW1lIjoiRG9lIiwicm9sZSI6ImF1ZGl0b3IiLCJlbWFpbCI6ImpvaG5kb2VAZXhhbXBsZS5jb20iLCJvcmdhbmlzYXRpb25faWQiOiI4NWU3NDMzNi04M2MwLTQ3MWEtYWM5ZC1lOWQwOWQ3MjU2ZTQiLCJuYmYiOjE3NTIwMzY3NDEsImV4cCI6MTc1MjA2NTU0MSwiaWF0IjoxNzUyMDM2NzQxLCJpc3MiOiJBdWRpdFN5c3RlbSIsImF1ZCI6IkF1ZGl0U3lzdGVtQ2xpZW50cyJ9.cXq6JMS2CMdmUo78XBblXuCa0plL4eb0PY7LuV6Zk50"

def decode_jwt_token():
    """Decode and display JWT token information"""
    try:
        # Decode without verification to see the payload
        decoded = jwt.decode(JWT_TOKEN, options={"verify_signature": False})
        
        print("=== JWT Token Analysis ===")
        print(f"User ID: {decoded.get('nameid')}")
        print(f"Username: {decoded.get('unique_name')}")
        print(f"Name: {decoded.get('given_name')} {decoded.get('family_name')}")
        print(f"Role: {decoded.get('role')}")
        print(f"Email: {decoded.get('email')}")
        print(f"Organization ID: {decoded.get('organisation_id')}")
        print(f"Issued At: {datetime.fromtimestamp(decoded.get('iat', 0))}")
        print(f"Expires At: {datetime.fromtimestamp(decoded.get('exp', 0))}")
        print(f"Current Time: {datetime.now()}")
        print(f"Token Expired: {datetime.now().timestamp() > decoded.get('exp', 0)}")
        print()
        
        return decoded
    except Exception as e:
        print(f"Error decoding JWT token: {e}")
        return None

def test_basic_api():
    """Test basic API connectivity"""
    headers = {
        "Authorization": f"Bearer {JWT_TOKEN}",
        "Content-Type": "application/json"
    }
    
    print("=== Testing Basic API Endpoints ===")
    
    # Test 1: Health check (no auth required)
    try:
        response = requests.get(f"{API_BASE_URL.replace('/api/v1', '')}/health")
        print(f"Health Check: {response.status_code} - {response.text}")
    except Exception as e:
        print(f"Health Check Error: {e}")
    
    # Test 2: Get user notifications (requires auth)
    try:
        response = requests.get(f"{API_BASE_URL}/notifications", headers=headers)
        print(f"Get Notifications: {response.status_code}")
        if response.status_code != 200:
            print(f"Response: {response.text}")
    except Exception as e:
        print(f"Get Notifications Error: {e}")
    
    # Test 3: Get unread count (requires auth)
    try:
        response = requests.get(f"{API_BASE_URL}/notifications/unread-count", headers=headers)
        print(f"Get Unread Count: {response.status_code}")
        if response.status_code != 200:
            print(f"Response: {response.text}")
    except Exception as e:
        print(f"Get Unread Count Error: {e}")
    
    # Test 4: Test system alert with detailed error
    try:
        notification_data = {
            "title": "Debug Test",
            "message": "Testing permissions",
            "priority": "low"
        }
        response = requests.post(f"{API_BASE_URL}/notifications/system-alert", 
                               headers=headers, json=notification_data)
        print(f"System Alert: {response.status_code}")
        if response.status_code != 200:
            print(f"Response: {response.text}")
            print(f"Headers: {dict(response.headers)}")
    except Exception as e:
        print(f"System Alert Error: {e}")

def main():
    """Main function"""
    print("JWT Token and API Debug Tool")
    print("=" * 50)
    
    # Decode JWT token
    token_info = decode_jwt_token()
    
    # Test API connectivity
    test_basic_api()
    
    print("\n=== Summary ===")
    if token_info:
        if datetime.now().timestamp() > token_info.get('exp', 0):
            print("❌ Token is expired!")
        else:
            print("✅ Token is valid")
        
        role = token_info.get('role', 'unknown')
        if role in ['admin', 'manager']:
            print(f"✅ User has {role} role - should be able to send system alerts")
        else:
            print(f"❌ User has {role} role - may not have permission for system alerts")

if __name__ == "__main__":
    main() 