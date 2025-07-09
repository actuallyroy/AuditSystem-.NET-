#!/usr/bin/env python3
"""
Test script for assignment notification functionality
Tests that notifications are sent when assignments are created
"""

import os
import sys
import json
import time
import requests
from datetime import datetime, timedelta

# Add the current directory to the path so we can import from python_tests
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from notification_test_suite import get_auth_token, API_BASE_URL

def test_assignment_notification():
    """Test that notifications are sent when assignments are created"""
    
    print("🧪 Testing Assignment Notification Functionality")
    print("=" * 50)
    
    # Get authentication token
    token = get_auth_token()
    if not token:
        print("❌ Failed to get authentication token")
        return False
    
    headers = {
        'Authorization': f'Bearer {token}',
        'Content-Type': 'application/json'
    }
    
    try:
        # Step 1: Get available templates
        print("📋 Getting available templates...")
        templates_response = requests.get(f"{API_BASE_URL}/templates", headers=headers)
        if templates_response.status_code != 200:
            print(f"❌ Failed to get templates: {templates_response.status_code}")
            return False
        
        templates = templates_response.json()
        if not templates:
            print("❌ No templates available for testing")
            return False
        
        template = templates[0]  # Use the first available template
        print(f"✅ Using template: {template['name']}")
        
        # Step 2: Get available users (auditors)
        print("👥 Getting available users...")
        users_response = requests.get(f"{API_BASE_URL}/users", headers=headers)
        if users_response.status_code != 200:
            print(f"❌ Failed to get users: {users_response.status_code}")
            return False
        
        users = users_response.json()
        auditors = [user for user in users if user.get('role', '').lower() == 'auditor']
        if not auditors:
            print("❌ No auditors available for testing")
            return False
        
        auditor = auditors[0]  # Use the first available auditor
        print(f"✅ Using auditor: {auditor['firstName']} {auditor['lastName']}")
        
        # Step 3: Create an assignment
        print("📝 Creating assignment...")
        assignment_data = {
            "templateId": template['templateId'],
            "assignedToId": auditor['userId'],
            "dueDate": (datetime.now() + timedelta(days=7)).isoformat(),
            "priority": "high",
            "notes": "Test assignment for notification testing",
            "storeInfo": {
                "store_name": "Test Store",
                "address": "123 Test Street, Test City"
            }
        }
        
        assignment_response = requests.post(
            f"{API_BASE_URL}/assignments", 
            headers=headers, 
            json=assignment_data
        )
        
        if assignment_response.status_code != 201:
            print(f"❌ Failed to create assignment: {assignment_response.status_code}")
            print(f"Response: {assignment_response.text}")
            return False
        
        assignment = assignment_response.json()
        print(f"✅ Assignment created: {assignment['assignmentId']}")
        
        # Step 4: Wait a moment for notification processing
        print("⏳ Waiting for notification processing...")
        time.sleep(2)
        
        # Step 5: Check if notification was created
        print("🔔 Checking for notifications...")
        notifications_response = requests.get(f"{API_BASE_URL}/notifications", headers=headers)
        if notifications_response.status_code != 200:
            print(f"❌ Failed to get notifications: {notifications_response.status_code}")
            return False
        
        notifications = notifications_response.json()
        
        # Look for assignment notification
        assignment_notifications = [
            n for n in notifications 
            if n.get('type') == 'audit_assigned' and 
            n.get('title') == 'New Audit Assignment'
        ]
        
        if assignment_notifications:
            notification = assignment_notifications[0]
            print(f"✅ Assignment notification found!")
            print(f"   Title: {notification['title']}")
            print(f"   Message: {notification['message']}")
            print(f"   Type: {notification['type']}")
            print(f"   Priority: {notification['priority']}")
            print(f"   Created: {notification['createdAt']}")
            return True
        else:
            print("❌ No assignment notification found")
            print(f"Available notifications: {len(notifications)}")
            for n in notifications[:3]:  # Show first 3 notifications
                print(f"   - {n.get('title', 'No title')} ({n.get('type', 'No type')})")
            return False
            
    except Exception as e:
        print(f"❌ Test failed with exception: {e}")
        return False

def test_assign_template_to_auditor():
    """Test the alternative assignment method"""
    
    print("\n🧪 Testing Assign Template to Auditor Method")
    print("=" * 50)
    
    # Get authentication token
    token = get_auth_token()
    if not token:
        print("❌ Failed to get authentication token")
        return False
    
    headers = {
        'Authorization': f'Bearer {token}',
        'Content-Type': 'application/json'
    }
    
    try:
        # Get templates and users (reuse from above)
        templates_response = requests.get(f"{API_BASE_URL}/templates", headers=headers)
        users_response = requests.get(f"{API_BASE_URL}/users", headers=headers)
        
        if templates_response.status_code != 200 or users_response.status_code != 200:
            print("❌ Failed to get templates or users")
            return False
        
        templates = templates_response.json()
        users = users_response.json()
        auditors = [user for user in users if user.get('role', '').lower() == 'auditor']
        
        if not templates or not auditors:
            print("❌ No templates or auditors available")
            return False
        
        template = templates[0]
        auditor = auditors[0]
        
        # Create assignment using the assign endpoint
        assignment_data = {
            "templateId": template['templateId'],
            "auditorId": auditor['userId'],
            "dueDate": (datetime.now() + timedelta(days=5)).isoformat(),
            "priority": "medium",
            "notes": "Test assignment via assign endpoint",
            "storeInfo": {
                "store_name": "Test Store 2",
                "address": "456 Test Avenue, Test City"
            }
        }
        
        assignment_response = requests.post(
            f"{API_BASE_URL}/assignments/assign", 
            headers=headers, 
            json=assignment_data
        )
        
        if assignment_response.status_code != 201:
            print(f"❌ Failed to assign template: {assignment_response.status_code}")
            print(f"Response: {assignment_response.text}")
            return False
        
        assignment = assignment_response.json()
        print(f"✅ Template assigned: {assignment['assignmentId']}")
        
        # Wait and check for notification
        time.sleep(2)
        
        notifications_response = requests.get(f"{API_BASE_URL}/notifications", headers=headers)
        if notifications_response.status_code != 200:
            print(f"❌ Failed to get notifications: {notifications_response.status_code}")
            return False
        
        notifications = notifications_response.json()
        assignment_notifications = [
            n for n in notifications 
            if n.get('type') == 'audit_assigned' and 
            n.get('title') == 'New Audit Assignment'
        ]
        
        if assignment_notifications:
            notification = assignment_notifications[0]
            print(f"✅ Assignment notification found!")
            print(f"   Title: {notification['title']}")
            print(f"   Message: {notification['message']}")
            return True
        else:
            print("❌ No assignment notification found")
            return False
            
    except Exception as e:
        print(f"❌ Test failed with exception: {e}")
        return False

def main():
    """Run all assignment notification tests"""
    print("🚀 Starting Assignment Notification Tests")
    print("=" * 60)
    
    # Test 1: Create assignment
    test1_result = test_assignment_notification()
    
    # Test 2: Assign template to auditor
    test2_result = test_assign_template_to_auditor()
    
    # Summary
    print("\n📊 Test Results Summary")
    print("=" * 30)
    print(f"Create Assignment Test: {'✅ PASSED' if test1_result else '❌ FAILED'}")
    print(f"Assign Template Test: {'✅ PASSED' if test2_result else '❌ FAILED'}")
    
    if test1_result and test2_result:
        print("\n🎉 All assignment notification tests passed!")
        return True
    else:
        print("\n💥 Some tests failed. Check the logs above.")
        return False

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1) 