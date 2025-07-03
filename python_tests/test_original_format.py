#!/usr/bin/env python3
"""
Test Original Format - Verify the exact format from the user's original request works
"""

import requests
import json

# Test configuration
BASE_URL = "http://localhost:8080/api/v1"
JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJjMDE2MjNjYS1mYzliLTRiNDgtOWI5MC1kYTFkNDhkOTZmMzIiLCJ1bmlxdWVfbmFtZSI6ImFtaXRrdW1hcjkzNTI1QGdtYWlsLmNvbSIsImdpdmVuX25hbWUiOiJBbWl0IiwiZmFtaWx5X25hbWUiOiJLdW1hciIsInJvbGUiOiJNYW5hZ2VyIiwiZW1haWwiOiJhbWl0aGFzY3VtQGdtYWlsLmNvbSIsIm5iZiI6MTc1MTU2NDE2MCwiZXhwIjoxNzUxNTkyOTYwLCJpYXQiOjE3NTE1NjQxNjAsImlzcyI6IkF1ZGl0U3lzdGVtIiwiYXVkIjoiQXVkaXRTeXN0ZW1DbGllbnRzIn0.50aakaaRv6IilxBzDvQllGaAGugmfEjjKuNEnCN2bCU"

headers = {
    "Authorization": f"Bearer {JWT_TOKEN}",
    "Content-Type": "application/json"
}

def test_original_format():
    """Test the EXACT format from the user's original request"""
    print("🎯 TESTING ORIGINAL FORMAT")
    print("=" * 60)
    
    # This is EXACTLY what the user originally sent
    original_data = {
        "templateId": "123e4567-e89b-12d3-a456-426614174000",
        "assignedToId": "a509281d-578c-4643-bc35-bf3392c94294",
        "organisationId": "6aeacea4-edf4-436d-b30f-be54c9015262",
        "dueDate": "2025-07-24T00:00:00.000Z",
        "priority": "medium",
        "notes": "dfgbdsfgsdfgg",
        "storeInfo": {
            "storeName": "modikhana",
            "storeAddress": "afsadfsadfasdf"
        }
    }
    
    print("📋 Request Data:")
    print(json.dumps(original_data, indent=2))
    
    try:
        response = requests.post(
            f"{BASE_URL}/Assignments",
            headers=headers,
            json=original_data,
            timeout=10
        )
        
        print(f"\n📡 Response Status: {response.status_code}")
        
        if response.status_code == 201:
            print("✅ SUCCESS! Your original format now works!")
            result = response.json()
            print(f"📋 Assignment ID: {result.get('assignmentId')}")
            print(f"📋 Store Info: {result.get('storeInfo')}")
            print(f"📋 Priority: {result.get('priority')}")
            print(f"📋 Status: {result.get('status')}")
            
            # Cleanup
            assignment_id = result.get('assignmentId')
            if assignment_id:
                requests.delete(f"{BASE_URL}/Assignments/{assignment_id}", headers=headers)
                print(f"🧹 Cleaned up assignment {assignment_id}")
            
            return True
        else:
            print(f"❌ Failed: {response.text}")
            return False
            
    except Exception as e:
        print(f"❌ Error: {e}")
        return False

if __name__ == "__main__":
    success = test_original_format()
    
    if success:
        print("\n🎉 CONGRATULATIONS!")
        print("Your original request format now works perfectly!")
        print("The backend has been fixed to accept JSON objects for StoreInfo!")
    else:
        print("\n⚠️ Still some issues to resolve")
    
    exit(0 if success else 1)
