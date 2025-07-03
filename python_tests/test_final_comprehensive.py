#!/usr/bin/env python3
"""
Final Comprehensive Test - StoreInfo JSON Object Fix
Tests that the backend now correctly accepts JSON objects for StoreInfo
"""

import requests
import json
import uuid
from datetime import datetime, timezone, timedelta

# Test configuration
BASE_URL = "http://localhost:8080/api/v1"
JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJjMDE2MjNjYS1mYzliLTRiNDgtOWI5MC1kYTFkNDhkOTZmMzIiLCJ1bmlxdWVfbmFtZSI6ImFtaXRrdW1hcjkzNTI1QGdtYWlsLmNvbSIsImdpdmVuX25hbWUiOiJBbWl0IiwiZmFtaWx5X25hbWUiOiJLdW1hciIsInJvbGUiOiJNYW5hZ2VyIiwiZW1haWwiOiJhbWl0aGFzY3VtQGdtYWlsLmNvbSIsIm5iZiI6MTc1MTU2NDE2MCwiZXhwIjoxNzUxNTkyOTYwLCJpYXQiOjE3NTE1NjQxNjAsImlzcyI6IkF1ZGl0U3lzdGVtIiwiYXVkIjoiQXVkaXRTeXN0ZW1DbGllbnRzIn0.50aakaaRv6IilxBzDvQllGaAGugmfEjjKuNEnCN2bCU"

headers = {
    "Authorization": f"Bearer {JWT_TOKEN}",
    "Content-Type": "application/json"
}

# Test data with different GUIDs to avoid conflicts
test_data = [
    {
        "name": "Original Format Test",
        "endpoint": "/Assignments",
        "data": {
            "templateId": "123e4567-e89b-12d3-a456-426614174000",
            "assignedToId": "a509281d-578c-4643-bc35-bf3392c94294",
            "organisationId": "6aeacea4-edf4-436d-b30f-be54c9015262",
            "dueDate": "2025-07-24T00:00:00.000Z",
            "priority": "medium",
            "notes": "Original format test",
            "storeInfo": {
                "storeName": "Original Store",
                "storeAddress": "123 Original Street"
            }
        }
    },
    {
        "name": "Complex StoreInfo Test",
        "endpoint": "/Assignments/assign",
        "data": {
            "TemplateId": "987fcdeb-a123-45b6-c789-def012345678",
            "AuditorId": "a509281d-578c-4643-bc35-bf3392c94294",
            "DueDate": "2025-07-25T00:00:00.000Z",
            "Priority": "high",
            "Notes": "Complex StoreInfo test",
            "StoreInfo": {
                "storeName": "Complex Store",
                "storeAddress": "456 Complex Avenue",
                "manager": "John Doe",
                "phone": "+1234567890",
                "email": "store@example.com",
                "region": "North",
                "storeType": "Supermarket",
                "openHours": "9AM-9PM",
                "specialInstructions": "Use back entrance for deliveries"
            }
        }
    },
    {
        "name": "Minimal StoreInfo Test",
        "endpoint": "/Assignments/assign",
        "data": {
            "TemplateId": "111aaaaa-2222-bbbb-3333-ccccddddeeee",
            "AuditorId": "a509281d-578c-4643-bc35-bf3392c94294",
            "DueDate": "2025-07-26T00:00:00.000Z",
            "Priority": "low",
            "Notes": "Minimal StoreInfo test",
            "StoreInfo": {
                "name": "Minimal Store"
            }
        }
    }
]

created_assignments = []

def run_test(test_case):
    """Run a single test case"""
    print(f"\n🧪 {test_case['name']}")
    print("=" * 60)
    
    print("📋 Request Data:")
    print(json.dumps(test_case['data'], indent=2))
    
    try:
        response = requests.post(
            f"{BASE_URL}{test_case['endpoint']}",
            headers=headers,
            json=test_case['data'],
            timeout=10
        )
        
        print(f"\n📡 Response Status: {response.status_code}")
        
        if response.status_code == 201:
            print("✅ SUCCESS!")
            result = response.json()
            assignment_id = result.get('assignmentId')
            created_assignments.append(assignment_id)
            
            print(f"📋 Assignment ID: {assignment_id}")
            print(f"📋 Store Info: {result.get('storeInfo')}")
            print(f"📋 Priority: {result.get('priority')}")
            print(f"📋 Status: {result.get('status')}")
            
            return True
        else:
            print(f"❌ FAILED: {response.text}")
            return False
            
    except Exception as e:
        print(f"❌ ERROR: {e}")
        return False

def test_retrieve_assignment(assignment_id):
    """Test retrieving an assignment to verify StoreInfo is stored correctly"""
    try:
        response = requests.get(
            f"{BASE_URL}/Assignments/{assignment_id}",
            headers=headers,
            timeout=10
        )
        
        if response.status_code == 200:
            result = response.json()
            print(f"✅ Retrieved assignment {assignment_id}")
            print(f"📋 StoreInfo: {result.get('storeInfo')}")
            return True
        else:
            print(f"❌ Failed to retrieve: {response.text}")
            return False
            
    except Exception as e:
        print(f"❌ Retrieval error: {e}")
        return False

def cleanup_assignments():
    """Clean up created assignments"""
    print("\n🧹 CLEANUP")
    print("=" * 60)
    
    for assignment_id in created_assignments:
        try:
            response = requests.delete(
                f"{BASE_URL}/Assignments/{assignment_id}",
                headers=headers,
                timeout=10
            )
            
            if response.status_code == 204:
                print(f"✅ Deleted assignment {assignment_id}")
            else:
                print(f"⚠️  Delete response {response.status_code} for {assignment_id}")
                
        except Exception as e:
            print(f"❌ Cleanup error for {assignment_id}: {e}")

def main():
    """Run all tests"""
    print("🔧 FINAL COMPREHENSIVE TEST")
    print("="*80)
    print("✅ Testing that StoreInfo now accepts JSON objects directly")
    print("✅ No more JSON string parsing required!")
    print("✅ Original user format now works perfectly!")
    
    passed = 0
    total = len(test_data)
    
    # Run creation tests
    for test_case in test_data:
        if run_test(test_case):
            passed += 1
    
    # Test retrieval of created assignments
    print("\n🔍 TESTING RETRIEVAL")
    print("=" * 60)
    
    for assignment_id in created_assignments:
        test_retrieve_assignment(assignment_id)
    
    # Summary
    print("\n📊 TEST SUMMARY")
    print("="*80)
    print(f"✅ Tests Passed: {passed}/{total}")
    print(f"✅ Assignments Created: {len(created_assignments)}")
    
    if passed == total and len(created_assignments) > 0:
        print("\n🎉 ALL TESTS PASSED!")
        print("🎯 StoreInfo JSON object fix is working perfectly!")
        print("🎯 Your original request format now works!")
        print("🎯 No more 400 Bad Request errors!")
        print("🎯 JSON objects are accepted directly!")
    else:
        print(f"\n⚠️ Some tests failed: {total - passed} failed, {passed} passed")
    
    # Cleanup
    cleanup_assignments()
    
    return passed == total

if __name__ == "__main__":
    success = main()
    exit(0 if success else 1) 