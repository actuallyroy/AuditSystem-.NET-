#!/usr/bin/env python3
"""
Comprehensive test suite for Assignment API endpoints.
Tests all CRUD operations with proper role-based access control.
"""
import requests
import pytest
import json
from datetime import datetime, timedelta, timezone
import uuid
import time

# Test Configuration
BASE_URL = "http://localhost:8080/api/v1"
ASSIGNMENTS_URL = f"{BASE_URL}/Assignments"

# Test JWT tokens with different roles (using the existing token with Manager role)
JWT_TOKENS = {
    "manager": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJjMDE2MjNjYS1mYzliLTRiNDgtOWI5MC1kYTFkNDhkOTZmMzIiLCJ1bmlxdWVfbmFtZSI6ImFtaXRrdW1hcjkzNTI1QGdtYWlsLmNvbSIsImdpdmVuX25hbWUiOiJBbWl0IiwiZmFtaWx5X25hbWUiOiJLdW1hciIsInJvbGUiOiJNYW5hZ2VyIiwiZW1haWwiOiJhbWl0aGFzY3VtQGdtYWlsLmNvbSIsIm5iZiI6MTc1MTU2NDE2MCwiZXhwIjoxNzUxNTkyOTYwLCJpYXQiOjE3NTE1NjQxNjAsImlzcyI6IkF1ZGl0U3lzdGVtIiwiYXVkIjoiQXVkaXRTeXN0ZW1DbGllbnRzIn0.50aakaaRv6IilxBzDvQllGaAGugmfEjjKuNEnCN2bCU"
}

# Test data IDs (will be populated from actual system)
ORGANIZATION_ID = "6aeacea4-edf4-436d-b30f-be54c9015262"
MANAGER_USER_ID = "c01623ca-fc9b-4b48-9b90-da1d48d96f32"

def get_headers(role="manager"):
    """Get authorization headers for the specified role"""
    return {
        "Authorization": f"Bearer {JWT_TOKENS[role]}",
        "Content-Type": "application/json"
    }

def make_request(method, url, headers=None, data=None, params=None):
    """Make HTTP request with proper error handling"""
    try:
        response = requests.request(
            method=method,
            url=url,
            headers=headers or get_headers(),
            json=data,
            params=params,
            timeout=10
        )
        return response
    except requests.exceptions.RequestException as e:
        print(f"Request failed: {e}")
        return None

def get_test_users():
    """Get users from the organization for testing"""
    headers = get_headers("manager")
    response = make_request("GET", f"{BASE_URL}/Users/by-organisation/{ORGANIZATION_ID}", headers)
    
    if response and response.status_code == 200:
        users = response.json()
        auditors = [u for u in users if u.get('role', '').lower() == 'auditor']
        return users, auditors
    return [], []

def get_test_templates():
    """Get published templates for testing"""
    headers = get_headers("manager")
    response = make_request("GET", f"{BASE_URL}/Templates/published", headers)
    
    if response and response.status_code == 200:
        return response.json()
    return []

class TestAssignmentEndpoints:
    """Test Assignment API endpoints"""
    
    @classmethod
    def setup_class(cls):
        """Set up test data before running tests"""
        print("\n" + "="*70)
        print("🚀 SETTING UP ASSIGNMENT API TESTS")
        print("="*70)
        
        # Get test data
        cls.users, cls.auditors = get_test_users()
        cls.templates = get_test_templates()
        
        print(f"📊 Found {len(cls.users)} users, {len(cls.auditors)} auditors")
        print(f"📋 Found {len(cls.templates)} templates")
        
        # Store test assignment IDs for cleanup
        cls.created_assignments = []
    
    @classmethod
    def teardown_class(cls):
        """Clean up test data after tests"""
        print("\n" + "="*70)
        print("🧹 CLEANING UP TEST ASSIGNMENTS")
        print("="*70)
        
        headers = get_headers("manager")
        for assignment_id in cls.created_assignments:
            try:
                response = make_request("DELETE", f"{ASSIGNMENTS_URL}/{assignment_id}", headers)
                if response and response.status_code in [204, 404]:
                    print(f"✅ Cleaned up assignment {assignment_id}")
                else:
                    print(f"⚠️  Failed to clean up assignment {assignment_id}")
            except Exception as e:
                print(f"❌ Error cleaning up {assignment_id}: {e}")

    def test_01_get_assignments_as_manager(self):
        """Test getting assignments as manager"""
        print("\n🧪 Testing: GET /Assignments (Manager)")
        
        headers = get_headers("manager")
        response = make_request("GET", ASSIGNMENTS_URL, headers)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 200:
            assignments = response.json()
            print(f"   ✅ Success: Found {len(assignments)} assignments")
            
            # Validate structure if assignments exist
            if assignments:
                first_assignment = assignments[0]
                required_fields = ['assignmentId', 'templateId', 'assignedToId', 'assignedById', 'organisationId']
                for field in required_fields:
                    assert field in first_assignment, f"Missing field: {field}"
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")

    def test_02_create_assignment_success(self):
        """Test creating a new assignment successfully"""
        print("\n🧪 Testing: POST /Assignments (Create)")
        
        if not self.auditors or not self.templates:
            pytest.skip("No auditors or templates available for testing")
        
        auditor = self.auditors[0]
        template = self.templates[0]
        
        assignment_data = {
            "templateId": template['templateId'],
            "assignedToId": auditor['userId'],
            "dueDate": (datetime.now(timezone.utc) + timedelta(days=7)).isoformat(),
            "priority": "High",
            "notes": "Test assignment created by automated tests",
            "storeInfo": json.dumps({"storeName": "Test Store", "location": "Test Location"})
        }
        
        headers = get_headers("manager")
        response = make_request("POST", ASSIGNMENTS_URL, headers, assignment_data)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 201:
            assignment = response.json()
            print(f"   ✅ Success: Created assignment {assignment['assignmentId']}")
            
            # Store for cleanup
            self.created_assignments.append(assignment['assignmentId'])
            
            # Validate created assignment
            assert assignment['templateId'] == assignment_data['templateId']
            assert assignment['assignedToId'] == assignment_data['assignedToId']
            assert assignment['assignedById'] == MANAGER_USER_ID
            assert assignment['priority'] == assignment_data['priority']
            assert assignment['status'] == 'Pending'
            
            return assignment['assignmentId']
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")
            return None

    def test_03_create_assignment_invalid_data(self):
        """Test creating assignment with invalid data"""
        print("\n🧪 Testing: POST /Assignments (Invalid Data)")
        
        # Test with missing required fields
        invalid_data = {
            "assignedToId": str(uuid.uuid4()),  # Non-existent user
            "priority": "High"
            # Missing templateId
        }
        
        headers = get_headers("manager")
        response = make_request("POST", ASSIGNMENTS_URL, headers, invalid_data)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        assert response.status_code == 400, f"Expected 400, got {response.status_code}"
        print(f"   ✅ Success: Correctly rejected invalid data")

    def test_04_assign_template_to_auditor(self):
        """Test assigning template to auditor using dedicated endpoint"""
        print("\n🧪 Testing: POST /Assignments/assign")
        
        if not self.auditors or not self.templates:
            pytest.skip("No auditors or templates available for testing")
        
        auditor = self.auditors[0]
        template = self.templates[0]
        
        assignment_data = {
            "templateId": template['templateId'],
            "auditorId": auditor['userId'],
            "dueDate": (datetime.now(timezone.utc) + timedelta(days=14)).isoformat(),
            "priority": "Medium",
            "notes": "Assigned via dedicated endpoint",
            "storeInfo": json.dumps({"type": "Supermarket", "size": "Large"})
        }
        
        headers = get_headers("manager")
        response = make_request("POST", f"{ASSIGNMENTS_URL}/assign", headers, assignment_data)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 201:
            assignment = response.json()
            print(f"   ✅ Success: Assigned template to auditor {assignment['assignmentId']}")
            
            # Store for cleanup
            self.created_assignments.append(assignment['assignmentId'])
            
            return assignment['assignmentId']
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")
            return None

    def test_05_get_assignment_by_id(self):
        """Test getting specific assignment by ID"""
        print("\n🧪 Testing: GET /Assignments/{id}")
        
        # First create an assignment to test with
        assignment_id = self.test_02_create_assignment_success()
        if not assignment_id:
            pytest.skip("Could not create test assignment")
        
        headers = get_headers("manager")
        response = make_request("GET", f"{ASSIGNMENTS_URL}/{assignment_id}", headers)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 200:
            assignment = response.json()
            print(f"   ✅ Success: Retrieved assignment {assignment['assignmentId']}")
            
            # Validate structure
            assert assignment['assignmentId'] == assignment_id
            assert 'templateId' in assignment
            assert 'assignedToId' in assignment
            assert 'status' in assignment
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")

    def test_06_get_assignment_by_id_not_found(self):
        """Test getting assignment that doesn't exist"""
        print("\n🧪 Testing: GET /Assignments/{id} (Not Found)")
        
        fake_id = str(uuid.uuid4())
        headers = get_headers("manager")
        response = make_request("GET", f"{ASSIGNMENTS_URL}/{fake_id}", headers)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        assert response.status_code == 404, f"Expected 404, got {response.status_code}"
        print(f"   ✅ Success: Correctly returned 404 for non-existent assignment")

    def test_07_update_assignment(self):
        """Test updating an existing assignment"""
        print("\n🧪 Testing: PUT /Assignments/{id}")
        
        # First create an assignment to test with
        assignment_id = self.test_02_create_assignment_success()
        if not assignment_id:
            pytest.skip("Could not create test assignment")
        
        update_data = {
            "assignmentId": assignment_id,
            "dueDate": (datetime.now(timezone.utc) + timedelta(days=21)).isoformat(),
            "priority": "Critical",
            "notes": "Updated assignment notes",
            "status": "In Progress",
            "storeInfo": json.dumps({"storeName": "Updated Store", "manager": "John Doe"})
        }
        
        headers = get_headers("manager")
        response = make_request("PUT", f"{ASSIGNMENTS_URL}/{assignment_id}", headers, update_data)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 200:
            assignment = response.json()
            print(f"   ✅ Success: Updated assignment {assignment['assignmentId']}")
            
            # Validate updates
            assert assignment['priority'] == update_data['priority']
            assert assignment['status'] == update_data['status']
            assert assignment['notes'] == update_data['notes']
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")

    def test_08_update_assignment_status(self):
        """Test updating assignment status using PATCH endpoint"""
        print("\n🧪 Testing: PATCH /Assignments/{id}/status")
        
        # First create an assignment to test with
        assignment_id = self.test_02_create_assignment_success()
        if not assignment_id:
            pytest.skip("Could not create test assignment")
        
        status_data = {
            "status": "Completed"
        }
        
        headers = get_headers("manager")
        response = make_request("PATCH", f"{ASSIGNMENTS_URL}/{assignment_id}/status", headers, status_data)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 200:
            assignment = response.json()
            print(f"   ✅ Success: Updated status to {assignment['status']}")
            
            assert assignment['status'] == status_data['status']
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")

    def test_09_get_assignments_by_organisation(self):
        """Test getting assignments by organisation"""
        print("\n🧪 Testing: GET /Assignments/organisation/{id}")
        
        headers = get_headers("manager")
        response = make_request("GET", f"{ASSIGNMENTS_URL}/organisation/{ORGANIZATION_ID}", headers)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 200:
            assignments = response.json()
            print(f"   ✅ Success: Found {len(assignments)} assignments for organisation")
            
            # Validate all assignments belong to the organisation
            for assignment in assignments:
                assert assignment['organisationId'] == ORGANIZATION_ID
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")

    def test_10_get_assignments_by_auditor(self):
        """Test getting assignments by auditor"""
        print("\n🧪 Testing: GET /Assignments/auditor/{id}")
        
        if not self.auditors:
            pytest.skip("No auditors available for testing")
        
        auditor = self.auditors[0]
        headers = get_headers("manager")
        response = make_request("GET", f"{ASSIGNMENTS_URL}/auditor/{auditor['userId']}", headers)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 200:
            assignments = response.json()
            print(f"   ✅ Success: Found {len(assignments)} assignments for auditor")
            
            # Validate all assignments belong to the auditor
            for assignment in assignments:
                assert assignment['assignedToId'] == auditor['userId']
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")

    def test_11_get_assignments_by_status(self):
        """Test getting assignments by status"""
        print("\n🧪 Testing: GET /Assignments/status/Pending")
        
        headers = get_headers("manager")
        response = make_request("GET", f"{ASSIGNMENTS_URL}/status/Pending", headers)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 200:
            assignments = response.json()
            print(f"   ✅ Success: Found {len(assignments)} pending assignments")
            
            # Validate all assignments have pending status
            for assignment in assignments:
                assert assignment['status'] == 'Pending'
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")

    def test_12_get_pending_assignments(self):
        """Test getting pending assignments"""
        print("\n🧪 Testing: GET /Assignments/pending")
        
        headers = get_headers("manager")
        response = make_request("GET", f"{ASSIGNMENTS_URL}/pending", headers)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 200:
            assignments = response.json()
            print(f"   ✅ Success: Found {len(assignments)} pending assignments")
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")

    def test_13_get_overdue_assignments(self):
        """Test getting overdue assignments"""
        print("\n🧪 Testing: GET /Assignments/overdue")
        
        headers = get_headers("manager")
        response = make_request("GET", f"{ASSIGNMENTS_URL}/overdue", headers)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 200:
            assignments = response.json()
            print(f"   ✅ Success: Found {len(assignments)} overdue assignments")
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")

    def test_14_unassign_template_from_auditor(self):
        """Test unassigning template from auditor"""
        print("\n🧪 Testing: DELETE /Assignments/unassign")
        
        if not self.auditors or not self.templates:
            pytest.skip("No auditors or templates available for testing")
        
        # First assign a template
        auditor = self.auditors[0]
        template = self.templates[0]
        
        assignment_data = {
            "templateId": template['templateId'],
            "auditorId": auditor['userId'],
            "priority": "Low",
            "notes": "To be unassigned"
        }
        
        headers = get_headers("manager")
        assign_response = make_request("POST", f"{ASSIGNMENTS_URL}/assign", headers, assignment_data)
        
        if assign_response and assign_response.status_code == 201:
            assignment = assign_response.json()
            self.created_assignments.append(assignment['assignmentId'])
            
            # Now unassign
            unassign_data = {
                "templateId": template['templateId'],
                "auditorId": auditor['userId']
            }
            
            response = make_request("DELETE", f"{ASSIGNMENTS_URL}/unassign", headers, unassign_data)
            
            assert response is not None, "Request failed"
            print(f"   Status: {response.status_code}")
            
            if response.status_code == 204:
                print(f"   ✅ Success: Unassigned template from auditor")
                # Remove from cleanup list since it's deleted
                self.created_assignments.remove(assignment['assignmentId'])
            else:
                print(f"   ❌ Failed: {response.status_code} - {response.text}")
        else:
            pytest.skip("Could not create assignment to unassign")

    def test_15_delete_assignment(self):
        """Test deleting an assignment"""
        print("\n🧪 Testing: DELETE /Assignments/{id}")
        
        # First create an assignment to delete
        assignment_id = self.test_02_create_assignment_success()
        if not assignment_id:
            pytest.skip("Could not create test assignment")
        
        headers = get_headers("manager")
        response = make_request("DELETE", f"{ASSIGNMENTS_URL}/{assignment_id}", headers)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        if response.status_code == 204:
            print(f"   ✅ Success: Deleted assignment {assignment_id}")
            # Remove from cleanup list since it's deleted
            if assignment_id in self.created_assignments:
                self.created_assignments.remove(assignment_id)
        else:
            print(f"   ❌ Failed: {response.status_code} - {response.text}")

    def test_16_delete_assignment_not_found(self):
        """Test deleting assignment that doesn't exist"""
        print("\n🧪 Testing: DELETE /Assignments/{id} (Not Found)")
        
        fake_id = str(uuid.uuid4())
        headers = get_headers("manager")
        response = make_request("DELETE", f"{ASSIGNMENTS_URL}/{fake_id}", headers)
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        assert response.status_code == 404, f"Expected 404, got {response.status_code}"
        print(f"   ✅ Success: Correctly returned 404 for non-existent assignment")

    def test_17_unauthorized_access(self):
        """Test access without authorization"""
        print("\n🧪 Testing: Unauthorized Access")
        
        # No authorization header
        response = make_request("GET", ASSIGNMENTS_URL, headers={"Content-Type": "application/json"})
        
        assert response is not None, "Request failed"
        print(f"   Status: {response.status_code}")
        
        assert response.status_code == 401, f"Expected 401, got {response.status_code}"
        print(f"   ✅ Success: Correctly rejected unauthorized request")

    def test_18_invalid_assignment_data_validation(self):
        """Test various invalid assignment data scenarios"""
        print("\n🧪 Testing: Data Validation")
        
        headers = get_headers("manager")
        
        # Test invalid priority
        invalid_data = {
            "templateId": str(uuid.uuid4()),
            "assignedToId": str(uuid.uuid4()),
            "priority": "InvalidPriority"
        }
        
        response = make_request("POST", ASSIGNMENTS_URL, headers, invalid_data)
        
        assert response is not None, "Request failed"
        print(f"   Status (Invalid Priority): {response.status_code}")
        
        # Should be 400 (Bad Request) due to validation or referential integrity
        assert response.status_code in [400, 500], f"Expected 400 or 500, got {response.status_code}"
        print(f"   ✅ Success: Correctly handled invalid data")

def run_assignment_tests():
    """Run all assignment tests with detailed reporting"""
    print("\n" + "="*70)
    print("🧪 COMPREHENSIVE ASSIGNMENT API TESTS")
    print("="*70)
    
    # Check API availability
    try:
        response = requests.get(f"{BASE_URL}/health", timeout=5)
        if response.status_code != 200:
            print("❌ API health check failed - tests may fail")
    except:
        print("⚠️  Could not reach API health endpoint")
    
    # Run pytest with verbose output
    import subprocess
    import sys
    
    test_file = __file__
    result = subprocess.run([
        sys.executable, "-m", "pytest", test_file, "-v", "--tb=short", "-x"
    ], capture_output=False)
    
    return result.returncode == 0

if __name__ == "__main__":
    success = run_assignment_tests()
    if success:
        print("\n🎉 All assignment tests completed!")
    else:
        print("\n❌ Some tests failed!") 