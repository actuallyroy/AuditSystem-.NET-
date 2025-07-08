#!/usr/bin/env python3
"""
Get a fresh JWT token for testing
"""

import requests
import json
import sys

def get_fresh_token():
    """Get a fresh JWT token for testing."""
    base_url = "http://localhost:8080"
    
    # Test credentials
    credentials = {
        "username": "amitkumar93525@gmail.com",
        "password": "idDcXT.as5tAK2g"
    }
    
    print("🔐 Getting fresh JWT token...")
    
    try:
        # Login to get token
        response = requests.post(f"{base_url}/api/v1/Auth/login", json=credentials)
        
        if response.status_code == 200:
            data = response.json()
            token = data.get('token')
            if token:
                print(f"✅ Token obtained successfully!")
                print(f"🔑 Token: {token}")
                return token
            else:
                print(f"❌ No token in response: {data}")
                return None
        else:
            print(f"❌ Login failed: {response.status_code}")
            print(f"Response: {response.text}")
            return None
            
    except Exception as e:
        print(f"❌ Error getting token: {e}")
        return None

if __name__ == "__main__":
    token = get_fresh_token()
    if token:
        print(f"\n📋 Use this token in your tests:")
        print(f"Authorization: Bearer {token}")
    else:
        print("Failed to get token")
        sys.exit(1) 