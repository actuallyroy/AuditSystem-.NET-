#!/usr/bin/env python3
"""
Test script for audit creation and update functionality
Tests the scenario where an audit with the same assignment_id already exists
"""

import requests
import json
import time
import uuid
from datetime import datetime
from typing import Dict, Any
from tools import test_credentials

class AuditCreationTest:
    """Test class for audit creation and update functionality"""
    
    def __init__(self, base_url: str = "http://localhost:8080"):
        self.base_url = base_url
        self.session = requests.Session()
        self.test_data = {}
        
    def login(self, role: str = "manager") -> bool:
        """Login to get authentication token using credentials from test_credentials.py"""
        try:
            creds = test_credentials.get_test_credentials(role)
            login_url = f"{self.base_url}/api/v1/auth/login"
            login_data = {
                "username": creds["username"],
                "password": creds["password"]
            }
            
            response = self.session.post(login_url, json=login_data)
            if response.status_code == 200:
                token_data = response.json()
                token = token_data.get('token')
                if token:
                    self.session.headers.update({
                        'Authorization': f'Bearer {token}',
                        'Content-Type': 'application/json'
                    })
                    print(f"✅ Successfully logged in as {creds['username']}")
                    return True
                else:
                    print("❌ No token received in login response")
                    return False
            else:
                print(f"❌ Login failed with status {response.status_code}: {response.text}")
                return False
        except Exception as e:
            print(f"❌ Login error: {e}")
            return False
    
    def get_test_data(self) -> Dict[str, Any]:
        """Get test data from database using the query tool"""
        try:
            # Use a real user_id from the database (johndoe)
            return {
                "template_id": "b21ca180-9b69-4221-b236-8c316f3b41e3",  # Test Template
                "assignment_id": "550e8400-e29b-41d4-a716-446655440000",  # We'll create this
                "organisation_id": "85e74336-83c0-471a-ac9d-e9d09d7256e4",  # From users table
                "user_id": "2c8ef14b-8038-4841-8a41-131236c55082"  # johndoe
            }
        except Exception as e:
            print(f"❌ Error getting test data: {e}")
            return {}
    
    def create_assignment(self) -> str:
        """Create a test assignment"""
        try:
            test_data = self.get_test_data()
            assignment_url = f"{self.base_url}/api/v1/assignments"
            
            assignment_data = {
                "templateId": test_data["template_id"],
                "assignedToId": test_data["user_id"],
                "organisationId": test_data["organisation_id"],
                "dueDate": (datetime.now().replace(hour=23, minute=59, second=59)).isoformat() + "Z",
                "priority": "high",
                "notes": "Test assignment for audit creation testing",
                "storeInfo": {
                    "storeName": "Test Store",
                    "storeAddress": "123 Test Street, Test City"
                }
            }
            
            response = self.session.post(assignment_url, json=assignment_data)
            if response.status_code == 201:
                assignment = response.json()
                assignment_id = assignment.get('assignmentId')
                print(f"✅ Created assignment with ID: {assignment_id}")
                return assignment_id
            else:
                print(f"❌ Failed to create assignment: {response.status_code} - {response.text}")
                return None
        except Exception as e:
            print(f"❌ Error creating assignment: {e}")
            return None
    
    def create_audit(self, assignment_id: str, store_name: str = "Test Store", responses: Dict = None) -> Dict[str, Any]:
        """Create an audit for the given assignment"""
        try:
            test_data = self.get_test_data()
            audit_url = f"{self.base_url}/api/v1/audits"
            
            audit_data = {
                "templateId": test_data["template_id"],
                "assignmentId": assignment_id,
                "storeName": store_name,
                "storeLocation": "123 Test Street, Test City",
                "status": "in_progress"
            }
            
            if responses:
                audit_data["responses"] = responses
                audit_data["criticalIssues"] = 2
            
            response = self.session.post(audit_url, json=audit_data)
            if response.status_code == 201:
                audit = response.json()
                print(f"✅ Created audit with ID: {audit.get('auditId')}")
                return audit
            else:
                print(f"❌ Failed to create audit: {response.status_code} - {response.text}")
                return {"error": response.status_code, "message": response.text}
        except Exception as e:
            print(f"❌ Error creating audit: {e}")
            return {"error": "exception", "message": str(e)}

    def update_audit(self, audit_id: str, responses: Dict = None) -> Dict[str, Any]:
        """Update an existing audit by ID"""
        try:
            audit_url = f"{self.base_url}/api/v1/audits/{audit_id}"
            update_data = {}
            if responses:
                update_data["responses"] = responses
                update_data["criticalIssues"] = 3
                update_data["status"] = "submitted"
            response = self.session.put(audit_url, json=update_data)
            if response.status_code in (200, 201):
                audit = response.json()
                print(f"✅ Updated audit with ID: {audit.get('auditId')}")
                return audit
            else:
                print(f"❌ Failed to update audit: {response.status_code} - {response.text}")
                return {"error": response.status_code, "message": response.text}
        except Exception as e:
            print(f"❌ Error updating audit: {e}")
            return {"error": "exception", "message": str(e)}

    def delete_audit(self, audit_id: str):
        """Delete an audit by ID"""
        audit_url = f"{self.base_url}/api/v1/audits/{audit_id}"
        response = self.session.delete(audit_url)
        if response.status_code in (200, 204):
            print(f"🗑️ Deleted audit {audit_id}")
        else:
            print(f"❌ Failed to delete audit {audit_id}: {response.status_code} - {response.text}")

    def delete_assignment(self, assignment_id: str):
        """Delete an assignment by ID"""
        assignment_url = f"{self.base_url}/api/v1/assignments/{assignment_id}"
        response = self.session.delete(assignment_url)
        if response.status_code in (200, 204):
            print(f"🗑️ Deleted assignment {assignment_id}")
        else:
            print(f"❌ Failed to delete assignment {assignment_id}: {response.status_code} - {response.text}")

    def get_audit_by_assignment(self, assignment_id: str) -> Dict[str, Any]:
        """Get audit by assignment ID"""
        try:
            # First get all audits and filter by assignment_id
            audit_url = f"{self.base_url}/api/v1/audits"
            response = self.session.get(audit_url)
            
            if response.status_code == 200:
                audits = response.json()
                for audit in audits:
                    if audit.get('assignmentId') == assignment_id:
                        print(f"✅ Found audit with assignment ID: {assignment_id}")
                        return audit
                
                print(f"❌ No audit found with assignment ID: {assignment_id}")
                return None
            else:
                print(f"❌ Failed to get audits: {response.status_code} - {response.text}")
                return None
        except Exception as e:
            print(f"❌ Error getting audit: {e}")
            return None
    
    def run_test(self):
        print("🧪 Starting Audit Creation/Update Test")
        print("=" * 50)
        audit_id = None
        assignment_id = None
        try:
            # Step 1: Login
            if not self.login():
                print("❌ Cannot proceed without login")
                return
            # Step 2: Create an assignment
            assignment_id = self.create_assignment()
            if not assignment_id:
                print("❌ Cannot proceed without assignment")
                return
            # Step 3: Create initial audit
            print("\n📝 Step 1: Creating initial audit...")
            initial_responses = {
                "question1": {"answer": "Yes", "score": 5},
                "question2": {"answer": "No", "score": 0}
            }
            initial_audit = self.create_audit(
                assignment_id=assignment_id,
                store_name="Initial Store",
                responses=initial_responses
            )
            if not initial_audit or initial_audit.get("error"):
                print("❌ Failed to create initial audit")
                return
            audit_id = initial_audit.get("auditId")
            # Step 4: Try to create another audit with same assignment_id (should fail)
            print("\n🚫 Step 2: Attempting to create duplicate audit...")
            duplicate_audit = self.create_audit(
                assignment_id=assignment_id,
                store_name="Duplicate Store",
                responses=initial_responses
            )
            if duplicate_audit.get("error") and duplicate_audit["error"] in (400, 409):
                print("✅ Correctly failed to create duplicate audit (already exists)")
            else:
                print("❌ Duplicate audit creation did not fail as expected")
            # Step 5: Update the existing audit
            print("\n✏️ Step 3: Updating the existing audit...")
            updated_responses = {
                "question1": {"answer": "Yes", "score": 4},
                "question2": {"answer": "Yes", "score": 5},
                "question3": {"answer": "No", "score": 0}
            }
            updated_audit = self.update_audit(
                audit_id=audit_id,
                responses=updated_responses
            )
            if updated_audit and not updated_audit.get("error"):
                print("✅ Successfully updated the audit")
            else:
                print("❌ Failed to update the audit")
        finally:
            # Step 6: Cleanup
            print("\n🧹 Cleaning up...")
            if audit_id:
                self.delete_audit(audit_id)
            if assignment_id:
                self.delete_assignment(assignment_id)
            print("\n🎉 Test completed!")
            print("=" * 50)


def main():
    """Main function"""
    test = AuditCreationTest()
    test.run_test()


if __name__ == "__main__":
    main() 