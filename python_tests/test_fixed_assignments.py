#!/usr/bin/env python3
"""
Test Fixed Assignment System - StoreInfo JSON Object Support
Verifies that the backend now correctly accepts JSON objects for StoreInfo
"""

import requests
import json
from datetime import datetime, timezone, timedelta

# Test configuration
BASE_URL = "http://localhost:8080/api/v1"
JWT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJjMDE2MjNjYS1mYzliLTRiNDgtOWI5MC1kYTFkNDhkOTZmMzIiLCJ1bmlxdWVfbmFtZSI6ImFtaXRrdW1hcjkzNTI1QGdtYWlsLmNvbSIsImdpdmVuX25hbWUiOiJBbWl0IiwiZmFtaWx5X25hbWUiOiJLdW1hciIsInJvbGUiOiJNYW5hZ2VyIiwiZW1haWwiOiJhbWl0aGFzY3VtQGdtYWlsLmNvbSIsIm5iZiI6MTc1MTU2NDE2MCwiZXhwIjoxNzUxNTkyOTYwLCJpYXQiOjE3NTE1NjQxNjAsImlzcyI6IkF1ZGl0U3lzdGVtIiwiYXVkIjoiQXVkaXRTeXN0ZW1DbGllbnRzIn0.50aakaaRv6IilxBzDvQllGaAGugmfEjjKuNEnCN2bCU"

headers = {
    "Authorization": f"Bearer {JWT_TOKEN}",
    "Content-Type": "application/json"
}

# Test data
template_id = "123e4567-e89b-12d3-a456-426614174000"
auditor_id = "a509281d-578c-4643-bc35-bf3392c94294"
created_assignments = []

# Database constraints: priority must be 'low', 'medium', 'high' (lowercase)
# Database constraints: status must be 'pending', 'cancelled', 'expired', 'fulfilled' (lowercase)

def test_endpoint(method, endpoint, data=None, description=""):
    """Helper function to test endpoints"""
    url = f"{BASE_URL}{endpoint}"
    
    try:
        if method == "GET":
            response = requests.get(url, headers=headers, timeout=10)
        elif method == "POST":
            response = requests.post(url, headers=headers, json=data, timeout=10)
        elif method == "PUT":
            response = requests.put(url, headers=headers, json=data, timeout=10)
        elif method == "DELETE":
            response = requests.delete(url, headers=headers, timeout=10)
        else:
            print(f"❌ Unknown method: {method}")
            return None
            
        print(f"🧪 {description}")
        print(f"   Method: {method} {endpoint}")
        print(f"   Status: {response.status_code}")
        
        if response.status_code in [200, 201]:
            print(f"   ✅ Success")
            return response.json() if response.content else None
        else:
            print(f"   ❌ Failed: {response.text}")
            return None
            
    except Exception as e:
        print(f"❌ Request failed: {e}")
        return None

def test_original_json_object_format():
    """Test 1: Original JSON Object Format (YOUR ORIGINAL REQUEST)"""
    print("\n" + "="*60)
    print("🎯 TEST 1: ORIGINAL JSON OBJECT FORMAT")
    print("="*60)
    
    # This is exactly what you originally sent (with corrected priority)
    original_assignment_data = {
        "templateId": template_id,  # camelCase (should work if backend is flexible)
        "assignedToId": auditor_id,
        "organisationId": "6aeacea4-edf4-436d-b30f-be54c9015262",
        "dueDate": "2025-07-24T00:00:00.000Z",
        "priority": "medium",  # lowercase as required by database
        "notes": "dfgbdsfgsdfgg",
        "storeInfo": {  # JSON object (what you originally sent)
            "storeName": "modikhana",
            "storeAddress": "afsadfsadfasdf"
        }
    }
    
    result = test_endpoint("POST", "/Assignments", original_assignment_data, 
                          "Testing original JSON object format")
    
    if result:
        created_assignments.append(result.get('assignmentId'))
        print(f"   📋 Assignment ID: {result.get('assignmentId')}")
        print(f"   📋 Store Info: {result.get('storeInfo', 'N/A')}")
        return True
    return False

def test_pascal_case_json_object():
    """Test 2: Pascal Case with JSON Object"""
    print("\n" + "="*60)
    print("🎯 TEST 2: PASCAL CASE WITH JSON OBJECT")
    print("="*60)
    
    pascal_assignment_data = {
        "TemplateId": template_id,  # PascalCase
        "AssignedToId": auditor_id,
        "DueDate": "2025-07-24T00:00:00.000Z",
        "Priority": "high",  # lowercase as required by database
        "Notes": "Test with Pascal case fields",
        "StoreInfo": {  # JSON object
            "storeName": "Test Store",
            "storeAddress": "123 Test Street",
            "storeType": "Retail",
            "region": "North"
        }
    }
    
    result = test_endpoint("POST", "/Assignments", pascal_assignment_data,
                          "Testing Pascal case with JSON object")
    
    if result:
        created_assignments.append(result.get('assignmentId'))
        print(f"   📋 Assignment ID: {result.get('assignmentId')}")
        print(f"   📋 Store Info: {result.get('storeInfo', 'N/A')}")
        return True
    return False

def test_assign_endpoint_with_json_object():
    """Test 3: /assign endpoint with JSON object"""
    print("\n" + "="*60)
    print("🎯 TEST 3: /ASSIGN ENDPOINT WITH JSON OBJECT")
    print("="*60)
    
    assign_data = {
        "TemplateId": template_id,
        "AuditorId": auditor_id,
        "DueDate": "2025-07-24T00:00:00.000Z",
        "Priority": "low",  # lowercase as required by database
        "Notes": "Testing assign endpoint with JSON object",
        "StoreInfo": {  # JSON object
            "storeName": "Assignment Store",
            "storeAddress": "456 Assignment Ave",
            "manager": "John Doe",
            "phone": "+1234567890"
        }
    }
    
    result = test_endpoint("POST", "/Assignments/assign", assign_data,
                          "Testing /assign endpoint with JSON object")
    
    if result:
        created_assignments.append(result.get('assignmentId'))
        print(f"   📋 Assignment ID: {result.get('assignmentId')}")
        print(f"   📋 Store Info: {result.get('storeInfo', 'N/A')}")
        return True
    return False

def test_retrieve_assignments():
    """Test 4: Retrieve created assignments"""
    print("\n" + "="*60)
    print("🎯 TEST 4: RETRIEVE CREATED ASSIGNMENTS")
    print("="*60)
    
    for assignment_id in created_assignments:
        result = test_endpoint("GET", f"/Assignments/{assignment_id}",
                              description=f"Retrieving assignment {assignment_id}")
        if result:
            print(f"   📋 Retrieved: {result.get('assignmentId')}")
            print(f"   📋 Status: {result.get('status')}")
            print(f"   📋 Priority: {result.get('priority')}")
            print(f"   📋 Store Info: {result.get('storeInfo', 'N/A')}")

def test_various_priority_formats():
    """Test 5: Various priority formats"""
    print("\n" + "="*60)
    print("🎯 TEST 5: VARIOUS PRIORITY FORMATS")
    print("="*60)
    
    # Only use valid database constraint values
    priorities = ["low", "medium", "high"]
    
    for priority in priorities:
        test_data = {
            "TemplateId": template_id,
            "AuditorId": auditor_id,
            "DueDate": "2025-07-25T00:00:00.000Z",
            "Priority": priority,  # lowercase as required by database
            "Notes": f"Testing priority: {priority}",
            "StoreInfo": {
                "storeName": f"Store {priority}",
                "priority": priority
            }
        }
        
        result = test_endpoint("POST", "/Assignments/assign", test_data,
                              f"Testing priority: {priority}")
        
        if result:
            created_assignments.append(result.get('assignmentId'))

def cleanup_test_assignments():
    """Test 6: Cleanup created assignments"""
    print("\n" + "="*60)
    print("🧹 CLEANUP: DELETING TEST ASSIGNMENTS")
    print("="*60)
    
    for assignment_id in created_assignments:
        test_endpoint("DELETE", f"/Assignments/{assignment_id}",
                     description=f"Deleting assignment {assignment_id}")

def main():
    """Run all tests"""
    print("🔧 TESTING FIXED ASSIGNMENT SYSTEM")
    print("=" * 80)
    print("Testing that StoreInfo now accepts JSON objects directly")
    print("(No more string parsing required!)")
    
    # Run tests
    tests = [
        test_original_json_object_format,
        test_pascal_case_json_object,
        test_assign_endpoint_with_json_object,
        test_retrieve_assignments,
        test_various_priority_formats,
    ]
    
    passed = 0
    total = len(tests)
    
    for test in tests:
        try:
            if test():
                passed += 1
        except Exception as e:
            print(f"❌ Test failed with exception: {e}")
    
    print("\n" + "="*80)
    print("📊 TEST SUMMARY")
    print("="*80)
    print(f"✅ Tests Passed: {passed}/{total}")
    
    if passed == total:
        print("🎉 ALL TESTS PASSED! StoreInfo JSON object fix is working!")
    else:
        print(f"⚠️ Some tests failed. Check the logs above.")
    
    # Cleanup
    cleanup_test_assignments()
    
    return passed == total

if __name__ == "__main__":
    success = main()
    exit(0 if success else 1) 