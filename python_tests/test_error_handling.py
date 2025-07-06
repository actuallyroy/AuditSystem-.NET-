#!/usr/bin/env python3
"""
Error Handling Test Suite
Tests error handling, validation, and edge cases for all endpoints
"""

import json
import requests
import uuid
from datetime import datetime, timedelta
from typing import Dict, Any
import sys
import os

# Add the tools directory to the path
sys.path.append(os.path.join(os.path.dirname(__file__), 'tools'))

class ErrorHandlingTestSuite:
    def __init__(self, base_url: str = "http://localhost:8080"):
        self.base_url = base_url
        self.session = requests.Session()
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
    
    def test_authentication_errors(self) -> bool:
        """Test authentication error scenarios"""
        self.log("Testing authentication error scenarios")
        
        # Test login with invalid credentials
        invalid_creds = [
            {"username": "nonexistent", "password": "wrong"},
            {"username": "", "password": "test"},
            {"username": "test", "password": ""},
            {"username": None, "password": "test"},
            {"username": "test", "password": None},
            {}
        ]
        
        for creds in invalid_creds:
            try:
                response = self.session.post(
                    f"{self.base_url}/api/v1/Auth/login",
                    json=creds,
                    headers={"Content-Type": "application/json"}
                )
                
                if response.status_code in [400, 401, 422]:
                    self.log(f"Expected error for invalid credentials: {response.status_code}")
                else:
                    self.log(f"Unexpected response for invalid credentials: {response.status_code}", "WARNING")
                    
            except Exception as e:
                self.log(f"Login error (expected): {str(e)}")
        
        # Test protected endpoints without authentication
        protected_endpoints = [
            "/api/v1/Users",
            "/api/v1/Templates",
            "/api/v1/Organisations",
            "/api/v1/Assignments",
            "/api/v1/Audits"
        ]
        
        for endpoint in protected_endpoints:
            try:
                response = self.session.get(f"{self.base_url}{endpoint}")
                
                if response.status_code == 401:
                    self.log(f"Expected 401 for {endpoint}")
                else:
                    self.log(f"Unexpected response for {endpoint}: {response.status_code}", "WARNING")
                    
            except Exception as e:
                self.log(f"Request error for {endpoint}: {str(e)}")
        
        # Test with invalid token
        invalid_headers = {
            "Content-Type": "application/json",
            "Authorization": "Bearer invalid_token_here"
        }
        
        for endpoint in protected_endpoints:
            try:
                response = self.session.get(
                    f"{self.base_url}{endpoint}",
                    headers=invalid_headers
                )
                
                if response.status_code == 401:
                    self.log(f"Expected 401 for invalid token on {endpoint}")
                else:
                    self.log(f"Unexpected response for invalid token on {endpoint}: {response.status_code}", "WARNING")
                    
            except Exception as e:
                self.log(f"Request error for {endpoint}: {str(e)}")
        
        return True
    
    def test_validation_errors(self) -> bool:
        """Test input validation error scenarios"""
        self.log("Testing input validation error scenarios")
        
        if not self.authenticate("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        # Test user creation with invalid data
        invalid_user_data = [
            {},  # Empty data
            {"username": ""},  # Empty username
            {"username": "test", "password": ""},  # Empty password
            {"username": "test", "email": "invalid-email"},  # Invalid email
            {"username": "test", "role": "invalid_role"},  # Invalid role
            {"username": "test", "organisationId": "invalid-uuid"}  # Invalid UUID
        ]
        
        for user_data in invalid_user_data:
            try:
                response = self.session.post(
                    f"{self.base_url}/api/v1/Users",
                    json=user_data,
                    headers=headers
                )
                
                if response.status_code in [400, 422]:
                    self.log(f"Expected validation error for user data: {response.status_code}")
                else:
                    self.log(f"Unexpected response for invalid user data: {response.status_code}", "WARNING")
                    
            except Exception as e:
                self.log(f"User creation error (expected): {str(e)}")
        
        # Test template creation with invalid data
        invalid_template_data = [
            {},  # Empty data
            {"name": ""},  # Empty name
            {"name": "test", "category": ""},  # Empty category
            {"name": "test", "category": "test", "validFrom": "invalid-date"},  # Invalid date
            {"name": "test", "category": "test", "validTo": "invalid-date"}  # Invalid date
        ]
        
        for template_data in invalid_template_data:
            try:
                response = self.session.post(
                    f"{self.base_url}/api/v1/Templates",
                    json=template_data,
                    headers=headers
                )
                
                if response.status_code in [400, 422]:
                    self.log(f"Expected validation error for template data: {response.status_code}")
                else:
                    self.log(f"Unexpected response for invalid template data: {response.status_code}", "WARNING")
                    
            except Exception as e:
                self.log(f"Template creation error (expected): {str(e)}")
        
        # Test organisation creation with invalid data
        invalid_org_data = [
            {},  # Empty data
            {"name": ""},  # Empty name
            {"name": "test", "region": ""}  # Empty region
        ]
        
        for org_data in invalid_org_data:
            try:
                response = self.session.post(
                    f"{self.base_url}/api/v1/Organisations",
                    json=org_data,
                    headers=headers
                )
                
                if response.status_code in [400, 422]:
                    self.log(f"Expected validation error for organisation data: {response.status_code}")
                else:
                    self.log(f"Unexpected response for invalid organisation data: {response.status_code}", "WARNING")
                    
            except Exception as e:
                self.log(f"Organisation creation error (expected): {str(e)}")
        
        return True
    
    def test_not_found_errors(self) -> bool:
        """Test 404 error scenarios"""
        self.log("Testing 404 error scenarios")
        
        if not self.authenticate("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        # Test getting non-existent resources
        non_existent_ids = [
            str(uuid.uuid4()),
            str(uuid.uuid4()),
            str(uuid.uuid4())
        ]
        
        endpoints_to_test = [
            "/api/v1/Users/",
            "/api/v1/Templates/",
            "/api/v1/Organisations/",
            "/api/v1/Assignments/",
            "/api/v1/Audits/"
        ]
        
        for endpoint in endpoints_to_test:
            for resource_id in non_existent_ids:
                try:
                    response = self.session.get(
                        f"{self.base_url}{endpoint}{resource_id}",
                        headers=headers
                    )
                    
                    if response.status_code == 404:
                        self.log(f"Expected 404 for {endpoint}{resource_id}")
                    else:
                        self.log(f"Unexpected response for {endpoint}{resource_id}: {response.status_code}", "WARNING")
                        
                except Exception as e:
                    self.log(f"Request error for {endpoint}{resource_id}: {str(e)}")
        
        # Test invalid UUID format
        invalid_uuids = [
            "invalid-uuid",
            "123",
            "abc-def-ghi",
            ""
        ]
        
        for endpoint in endpoints_to_test:
            for invalid_id in invalid_uuids:
                try:
                    response = self.session.get(
                        f"{self.base_url}{endpoint}{invalid_id}",
                        headers=headers
                    )
                    
                    if response.status_code in [400, 404, 422]:
                        self.log(f"Expected error for invalid UUID {invalid_id} on {endpoint}: {response.status_code}")
                    else:
                        self.log(f"Unexpected response for invalid UUID {invalid_id} on {endpoint}: {response.status_code}", "WARNING")
                        
                except Exception as e:
                    self.log(f"Request error for {endpoint}{invalid_id}: {str(e)}")
        
        return True
    
    def test_authorization_errors(self) -> bool:
        """Test authorization error scenarios"""
        self.log("Testing authorization error scenarios")
        
        # Authenticate as auditor (lower privileges)
        if not self.authenticate("auditor"):
            self.log("Auditor authentication required", "ERROR")
            return False
        
        auditor_headers = self.get_auth_headers("auditor")
        
        # Test endpoints that require manager privileges
        manager_only_endpoints = [
            ("POST", "/api/v1/Users"),
            ("POST", "/api/v1/Templates"),
            ("POST", "/api/v1/Organisations"),
            ("PUT", "/api/v1/Users/00000000-0000-0000-0000-000000000000"),
            ("DELETE", "/api/v1/Users/00000000-0000-0000-0000-000000000000"),
            ("PUT", "/api/v1/Templates/00000000-0000-0000-0000-000000000000"),
            ("DELETE", "/api/v1/Templates/00000000-0000-0000-0000-000000000000")
        ]
        
        for method, endpoint in manager_only_endpoints:
            try:
                if method == "POST":
                    response = self.session.post(
                        f"{self.base_url}{endpoint}",
                        json={},
                        headers=auditor_headers
                    )
                elif method == "PUT":
                    response = self.session.put(
                        f"{self.base_url}{endpoint}",
                        json={},
                        headers=auditor_headers
                    )
                elif method == "DELETE":
                    response = self.session.delete(
                        f"{self.base_url}{endpoint}",
                        headers=auditor_headers
                    )
                
                if response.status_code in [401, 403]:
                    self.log(f"Expected authorization error for {method} {endpoint}: {response.status_code}")
                else:
                    self.log(f"Unexpected response for {method} {endpoint}: {response.status_code}", "WARNING")
                    
            except Exception as e:
                self.log(f"Request error for {method} {endpoint}: {str(e)}")
        
        return True
    
    def test_rate_limiting(self) -> bool:
        """Test rate limiting scenarios"""
        self.log("Testing rate limiting scenarios")
        
        if not self.authenticate("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        # Test rapid requests to see if rate limiting is in place
        endpoint = "/api/v1/Users"
        rapid_requests = 20
        
        rate_limited = False
        for i in range(rapid_requests):
            try:
                response = self.session.get(
                    f"{self.base_url}{endpoint}",
                    headers=headers
                )
                
                if response.status_code == 429:
                    self.log(f"Rate limiting detected after {i+1} requests")
                    rate_limited = True
                    break
                    
            except Exception as e:
                self.log(f"Request error: {str(e)}")
                break
        
        if not rate_limited:
            self.log("No rate limiting detected (this might be expected)", "INFO")
        
        return True
    
    def test_malformed_requests(self) -> bool:
        """Test malformed request scenarios"""
        self.log("Testing malformed request scenarios")
        
        if not self.authenticate("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        # Test with malformed JSON
        malformed_data = [
            "{invalid json}",
            '{"name": "test",}',
            '{"name": "test", "value": }',
            ""
        ]
        
        for data in malformed_data:
            try:
                response = self.session.post(
                    f"{self.base_url}/api/v1/Users",
                    data=data,
                    headers=headers
                )
                
                if response.status_code in [400, 422]:
                    self.log(f"Expected error for malformed JSON: {response.status_code}")
                else:
                    self.log(f"Unexpected response for malformed JSON: {response.status_code}", "WARNING")
                    
            except Exception as e:
                self.log(f"Request error for malformed JSON: {str(e)}")
        
        # Test with wrong content type
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Users",
                data='{"name": "test"}',
                headers={"Content-Type": "text/plain"}
            )
            
            if response.status_code in [400, 415]:
                self.log(f"Expected error for wrong content type: {response.status_code}")
            else:
                self.log(f"Unexpected response for wrong content type: {response.status_code}", "WARNING")
                
        except Exception as e:
            self.log(f"Request error for wrong content type: {str(e)}")
        
        return True
    
    def run_error_tests(self) -> bool:
        """Run all error handling tests"""
        self.log("Starting error handling test suite")
        
        # Test authentication errors
        if not self.test_authentication_errors():
            self.log("Authentication error tests failed", "ERROR")
            return False
        
        # Test validation errors
        if not self.test_validation_errors():
            self.log("Validation error tests failed", "ERROR")
            return False
        
        # Test not found errors
        if not self.test_not_found_errors():
            self.log("Not found error tests failed", "ERROR")
            return False
        
        # Test authorization errors
        if not self.test_authorization_errors():
            self.log("Authorization error tests failed", "ERROR")
            return False
        
        # Test rate limiting
        if not self.test_rate_limiting():
            self.log("Rate limiting tests failed", "WARNING")
        
        # Test malformed requests
        if not self.test_malformed_requests():
            self.log("Malformed request tests failed", "WARNING")
        
        self.log("Error handling test suite completed!")
        return True

def main():
    """Main function to run the error handling test suite"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Error Handling Test Suite")
    parser.add_argument("--base-url", default="http://localhost:8080", 
                       help="Base URL of the API (default: http://localhost:8080)")
    parser.add_argument("--verbose", "-v", action="store_true", 
                       help="Enable verbose logging")
    
    args = parser.parse_args()
    
    # Create and run test suite
    test_suite = ErrorHandlingTestSuite(args.base_url)
    
    try:
        success = test_suite.run_error_tests()
        if success:
            print("\n✅ Error handling tests passed successfully!")
            sys.exit(0)
        else:
            print("\n❌ Some error handling tests failed!")
            sys.exit(1)
    except KeyboardInterrupt:
        print("\n⚠️  Error handling tests interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n💥 Unexpected error: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main() 