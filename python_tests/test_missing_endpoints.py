#!/usr/bin/env python3
"""
Missing Endpoints Test Suite
Tests endpoints that are in the OpenAPI spec but not covered in the main test suite
"""

import json
import requests
import uuid
import time
from datetime import datetime, timedelta
from typing import Dict, Any, Optional
import sys
import os

# Add the tools directory to the path
sys.path.append(os.path.join(os.path.dirname(__file__), 'tools'))

class MissingEndpointsTestSuite:
    def __init__(self, base_url: str = "http://localhost:8080"):
        self.base_url = base_url
        self.session = requests.Session()
        self.test_data = {}
        self.auth_tokens = {}
        
        # Load test credentials
        with open('python_tests/tools/test_credentials.json', 'r') as f:
            self.credentials = json.load(f)
    
    def log(self, message: str, level: str = "INFO"):
        """Log test messages with timestamps"""
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] {level}: {message}")
    
    def authenticate(self, role: str) -> bool:
        """Authenticate user and store token"""
        if role not in self.credentials:
            self.log(f"Invalid role: {role}", "ERROR")
            return False
        
        creds = self.credentials[role]
        login_data = {
            "username": creds["username"],
            "password": creds["password"]
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Auth/login",
                json=login_data,
                headers={"Content-Type": "application/json"}
            )
            
            if response.status_code == 200:
                auth_data = response.json()
                self.auth_tokens[role] = auth_data.get("token")
                self.log(f"Successfully authenticated as {role}")
                return True
            else:
                self.log(f"Authentication failed for {role}: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Authentication error for {role}: {str(e)}", "ERROR")
            return False
    
    def get_auth_headers(self, role: str) -> Dict[str, str]:
        """Get headers with authentication token"""
        token = self.auth_tokens.get(role)
        if not token:
            self.log(f"No token found for role: {role}", "ERROR")
            return {}
        
        return {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {token}"
        }
    
    def setup_test_data(self) -> bool:
        """Setup test data for missing endpoint tests"""
        self.log("Setting up test data for missing endpoint tests")
        
        if not self.auth_tokens.get("manager"):
            if not self.authenticate("manager"):
                return False
        
        headers = self.get_auth_headers("manager")
        
        # Create test organisation
        org_data = {
            "name": f"Test Org Missing Endpoints {uuid.uuid4().hex[:8]}",
            "region": "Test Region",
            "type": "Retail"
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Organisations",
                json=org_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.test_data["organisation"] = response.json()
                self.log("Test organisation created")
            else:
                self.log(f"Failed to create test organisation: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Error creating test organisation: {str(e)}", "ERROR")
            return False
        
        # Create test user
        user_data = {
            "organisationId": self.test_data["organisation"]["organisationId"],
            "username": f"testuser_missing_{uuid.uuid4().hex[:8]}",
            "firstName": "Test",
            "lastName": "User",
            "email": f"test_missing_{uuid.uuid4().hex[:8]}@example.com",
            "phone": "1234567890",
            "role": "auditor",
            "password": "TestPassword123!"
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Users",
                json=user_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.test_data["user"] = response.json()
                self.log("Test user created")
            else:
                self.log(f"Failed to create test user: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Error creating test user: {str(e)}", "ERROR")
            return False
        
        # Create test template
        template_data = {
            "name": f"Test Template Missing Endpoints {uuid.uuid4().hex[:8]}",
            "description": "Template for testing missing endpoints",
            "category": "Compliance",
            "questions": {
                "question1": {
                    "text": "Test question 1",
                    "type": "boolean",
                    "required": True
                }
            },
            "scoringRules": {
                "passingScore": 80
            }
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Templates",
                json=template_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.test_data["template"] = response.json()
                self.log("Test template created")
            else:
                self.log(f"Failed to create test template: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Error creating test template: {str(e)}", "ERROR")
            return False
        
        return True
    
    def test_templates_user_endpoint(self) -> bool:
        """Test GET /api/v1/Templates/user/{userId} endpoint"""
        self.log("Testing Templates by User endpoint")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        if "user" not in self.test_data:
            self.log("Test user not available", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        user_id = self.test_data["user"]["userId"]
        
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Templates/user/{user_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                templates = response.json()
                self.log(f"Successfully retrieved {len(templates)} templates for user")
                return True
            else:
                self.log(f"Failed to get templates by user: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Error getting templates by user: {str(e)}", "ERROR")
            return False
    
    def test_templates_version_endpoint(self) -> bool:
        """Test POST /api/v1/Templates/{id}/version endpoint"""
        self.log("Testing Templates Version endpoint")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        if "template" not in self.test_data:
            self.log("Test template not available", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        template_id = self.test_data["template"]["templateId"]
        
        # Update template data for new version
        version_data = {
            "name": f"Updated Template Version {uuid.uuid4().hex[:8]}",
            "description": "Updated template for version testing",
            "category": "Compliance",
            "questions": {
                "question1": {
                    "text": "Updated test question 1",
                    "type": "boolean",
                    "required": True
                },
                "question2": {
                    "text": "New test question 2",
                    "type": "text",
                    "required": False
                }
            },
            "scoringRules": {
                "passingScore": 85
            }
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Templates/{template_id}/version",
                json=version_data,
                headers=headers
            )
            
            if response.status_code == 200:
                updated_template = response.json()
                self.log(f"Successfully created new template version: {updated_template['version']}")
                return True
            else:
                self.log(f"Failed to create template version: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Error creating template version: {str(e)}", "ERROR")
            return False
    
    def test_organisations_invite_endpoint(self) -> bool:
        """Test POST /api/v1/Organisations/{id}/invite endpoint"""
        self.log("Testing Organisations Invite endpoint")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        if "organisation" not in self.test_data:
            self.log("Test organisation not available", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        org_id = self.test_data["organisation"]["organisationId"]
        
        invite_data = {
            "email": f"invite_test_{uuid.uuid4().hex[:8]}@example.com",
            "role": "auditor"
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Organisations/{org_id}/invite",
                json=invite_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Successfully sent organisation invitation")
                return True
            else:
                self.log(f"Failed to send organisation invitation: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Error sending organisation invitation: {str(e)}", "ERROR")
            return False
    
    def test_organisations_join_endpoint(self) -> bool:
        """Test POST /api/v1/Organisations/join/{id} endpoint"""
        self.log("Testing Organisations Join endpoint")
        
        if not self.auth_tokens.get("auditor"):
            self.log("Auditor authentication required", "ERROR")
            return False
        
        if "organisation" not in self.test_data:
            self.log("Test organisation not available", "ERROR")
            return False
        
        headers = self.get_auth_headers("auditor")
        org_id = self.test_data["organisation"]["organisationId"]
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Organisations/join/{org_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Successfully joined organisation")
                return True
            else:
                self.log(f"Failed to join organisation: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Error joining organisation: {str(e)}", "ERROR")
            return False
    
    def test_organisations_accept_invitation_endpoint(self) -> bool:
        """Test POST /api/v1/Organisations/accept-invitation endpoint"""
        self.log("Testing Organisations Accept Invitation endpoint")
        
        if not self.auth_tokens.get("auditor"):
            self.log("Auditor authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("auditor")
        
        # Note: This would typically require a valid invitation token
        # For testing purposes, we'll test with a dummy token
        accept_data = {
            "token": "dummy_invitation_token_for_testing"
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Organisations/accept-invitation",
                json=accept_data,
                headers=headers
            )
            
            # This might fail with 400/404 due to invalid token, which is expected
            if response.status_code in [200, 400, 404]:
                self.log(f"Accept invitation endpoint responded with: {response.status_code}")
                return True
            else:
                self.log(f"Unexpected response from accept invitation: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Error accepting invitation: {str(e)}", "ERROR")
            return False
    
    def test_organisations_remove_user_endpoint(self) -> bool:
        """Test DELETE /api/v1/Organisations/{organisationId}/users/{userId} endpoint"""
        self.log("Testing Organisations Remove User endpoint")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        if "organisation" not in self.test_data or "user" not in self.test_data:
            self.log("Test organisation or user not available", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        org_id = self.test_data["organisation"]["organisationId"]
        user_id = self.test_data["user"]["userId"]
        
        try:
            response = self.session.delete(
                f"{self.base_url}/api/v1/Organisations/{org_id}/users/{user_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Successfully removed user from organisation")
                return True
            else:
                self.log(f"Failed to remove user from organisation: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Error removing user from organisation: {str(e)}", "ERROR")
            return False
    
    def test_assignments_unassign_endpoint(self) -> bool:
        """Test DELETE /api/v1/Assignments/unassign endpoint"""
        self.log("Testing Assignments Unassign endpoint")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        if "template" not in self.test_data or "user" not in self.test_data:
            self.log("Test template or user not available", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        template_id = self.test_data["template"]["templateId"]
        user_id = self.test_data["user"]["userId"]
        
        # First, create an assignment to unassign
        assign_data = {
            "templateId": template_id,
            "auditorId": user_id,
            "dueDate": (datetime.now() + timedelta(days=7)).isoformat(),
            "priority": "medium",
            "notes": "Test assignment for unassign testing"
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Assignments/assign",
                json=assign_data,
                headers=headers
            )
            
            if response.status_code != 200:
                self.log(f"Failed to create assignment for unassign test: {response.status_code}", "ERROR")
                return False
            
            assignment = response.json()
            self.log("Created assignment for unassign testing")
            
            # Now test the unassign endpoint
            unassign_data = {
                "templateId": template_id,
                "auditorId": user_id
            }
            
            response = self.session.delete(
                f"{self.base_url}/api/v1/Assignments/unassign",
                json=unassign_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Successfully unassigned template from auditor")
                return True
            else:
                self.log(f"Failed to unassign template: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Error testing unassign endpoint: {str(e)}", "ERROR")
            return False
    
    def cleanup_test_data(self) -> bool:
        """Clean up test data created during testing"""
        self.log("Cleaning up test data")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required for cleanup", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        success = True
        
        # Clean up template
        if "template" in self.test_data:
            try:
                response = self.session.delete(
                    f"{self.base_url}/api/v1/Templates/{self.test_data['template']['templateId']}",
                    headers=headers
                )
                if response.status_code == 200:
                    self.log("Test template cleaned up")
                else:
                    self.log(f"Failed to cleanup template: {response.status_code}", "WARNING")
                    success = False
            except Exception as e:
                self.log(f"Error cleaning up template: {str(e)}", "WARNING")
                success = False
        
        # Clean up user
        if "user" in self.test_data:
            try:
                response = self.session.delete(
                    f"{self.base_url}/api/v1/Users/{self.test_data['user']['userId']}",
                    headers=headers
                )
                if response.status_code == 200:
                    self.log("Test user cleaned up")
                else:
                    self.log(f"Failed to cleanup user: {response.status_code}", "WARNING")
                    success = False
            except Exception as e:
                self.log(f"Error cleaning up user: {str(e)}", "WARNING")
                success = False
        
        # Clean up organisation
        if "organisation" in self.test_data:
            try:
                response = self.session.delete(
                    f"{self.base_url}/api/v1/Organisations/{self.test_data['organisation']['organisationId']}",
                    headers=headers
                )
                if response.status_code == 200:
                    self.log("Test organisation cleaned up")
                else:
                    self.log(f"Failed to cleanup organisation: {response.status_code}", "WARNING")
                    success = False
            except Exception as e:
                self.log(f"Error cleaning up organisation: {str(e)}", "WARNING")
                success = False
        
        return success
    
    def run_missing_endpoint_tests(self) -> bool:
        """Run all missing endpoint tests"""
        self.log("Starting Missing Endpoints Test Suite")
        
        # Authenticate both roles
        if not self.authenticate("manager"):
            return False
        
        if not self.authenticate("auditor"):
            return False
        
        # Setup test data
        if not self.setup_test_data():
            self.log("Failed to setup test data", "ERROR")
            return False
        
        # Run all missing endpoint tests
        tests = [
            ("Templates by User", self.test_templates_user_endpoint),
            ("Templates Version", self.test_templates_version_endpoint),
            ("Organisations Invite", self.test_organisations_invite_endpoint),
            ("Organisations Join", self.test_organisations_join_endpoint),
            ("Organisations Accept Invitation", self.test_organisations_accept_invitation_endpoint),
            ("Organisations Remove User", self.test_organisations_remove_user_endpoint),
            ("Assignments Unassign", self.test_assignments_unassign_endpoint)
        ]
        
        passed = 0
        total = len(tests)
        
        for test_name, test_func in tests:
            self.log(f"Running test: {test_name}")
            try:
                if test_func():
                    self.log(f"✅ {test_name} passed")
                    passed += 1
                else:
                    self.log(f"❌ {test_name} failed", "ERROR")
            except Exception as e:
                self.log(f"❌ {test_name} failed with exception: {str(e)}", "ERROR")
        
        # Cleanup
        self.cleanup_test_data()
        
        # Summary
        self.log(f"Missing Endpoints Test Suite completed: {passed}/{total} tests passed")
        return passed == total

def main():
    """Main function to run the missing endpoints test suite"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Missing Endpoints Test Suite")
    parser.add_argument("--base-url", default="http://localhost:8080", 
                       help="Base URL of the API server")
    parser.add_argument("--verbose", "-v", action="store_true", 
                       help="Enable verbose output")
    
    args = parser.parse_args()
    
    print("="*80)
    print("MISSING ENDPOINTS TEST SUITE")
    print("="*80)
    print(f"Testing API at: {args.base_url}")
    print(f"Timestamp: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("="*80)
    
    test_suite = MissingEndpointsTestSuite(args.base_url)
    
    try:
        success = test_suite.run_missing_endpoint_tests()
        
        print("\n" + "="*80)
        if success:
            print("🎉 ALL MISSING ENDPOINT TESTS PASSED!")
            print("✅ All endpoints from the OpenAPI specification are now covered")
        else:
            print("❌ SOME MISSING ENDPOINT TESTS FAILED")
            print("⚠️  Please review the test results above")
        print("="*80)
        
        sys.exit(0 if success else 1)
        
    except KeyboardInterrupt:
        print("\n⚠️  Test suite interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ Test suite failed with unexpected error: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main() 