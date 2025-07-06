#!/usr/bin/env python3
"""
Comprehensive API Test Suite for Retail Execution Audit System
Tests all endpoints with proper authentication and data validation
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

class APITestSuite:
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
    
    def test_health_check(self) -> bool:
        """Test health check endpoint"""
        self.log("Testing health check endpoint")
        try:
            response = self.session.get(f"{self.base_url}/health")
            if response.status_code == 200:
                self.log("Health check passed")
                return True
            else:
                self.log(f"Health check failed: {response.status_code}", "ERROR")
                return False
        except Exception as e:
            self.log(f"Health check error: {str(e)}", "ERROR")
            return False
    
    def test_auth_endpoints(self) -> bool:
        """Test authentication endpoints"""
        self.log("Testing authentication endpoints")
        
        # Test registration
        register_data = {
            "username": f"testuser_{uuid.uuid4().hex[:8]}",
            "firstName": "Test",
            "lastName": "User",
            "email": f"test{uuid.uuid4().hex[:8]}@example.com",
            "phone": "1234567890",
            "password": "TestPassword123!"
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Auth/register",
                json=register_data,
                headers={"Content-Type": "application/json"}
            )
            
            if response.status_code == 200:
                self.log("User registration successful")
                self.test_data["test_user"] = response.json()
            else:
                self.log(f"Registration failed: {response.status_code}", "WARNING")
        
        except Exception as e:
            self.log(f"Registration error: {str(e)}", "ERROR")
        
        # Test login for both roles
        success = True
        for role in ["manager", "auditor"]:
            if not self.authenticate(role):
                success = False
        
        return success
    
    def test_organisation_endpoints(self) -> bool:
        """Test organisation endpoints"""
        self.log("Testing organisation endpoints")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        # Create organisation
        org_data = {
            "name": f"Test Organisation {uuid.uuid4().hex[:8]}",
            "region": "Test Region",
            "type": "Retail"
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Organisations",
                json=org_data,
                headers=headers
            )
            
            if response.status_code in (200, 201):
                self.test_data["organisation"] = response.json()
                self.log("Organisation created successfully")
            else:
                self.log(f"Organisation creation failed: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Organisation creation error: {str(e)}", "ERROR")
            return False
        
        org_id = self.test_data["organisation"]["organisationId"]
        
        # Test get organisation by ID
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Organisations/{org_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get organisation by ID successful")
            else:
                self.log(f"Get organisation failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get organisation error: {str(e)}", "ERROR")
        
        # Test update organisation
        update_data = {
            "organisationId": org_id,
            "name": f"Updated Organisation {uuid.uuid4().hex[:8]}",
            "region": "Updated Region",
            "type": "Updated Type"
        }
        
        try:
            response = self.session.put(
                f"{self.base_url}/api/v1/Organisations/{org_id}",
                json=update_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Organisation update successful")
            else:
                self.log(f"Organisation update failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Organisation update error: {str(e)}", "ERROR")
        
        # Test get all organisations
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Organisations",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get all organisations successful")
            else:
                self.log(f"Get all organisations failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get all organisations error: {str(e)}", "ERROR")
        
        # Test available organisations
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Organisations/available",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get available organisations successful")
            else:
                self.log(f"Get available organisations failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get available organisations error: {str(e)}", "ERROR")
        
        return True
    
    def test_user_endpoints(self) -> bool:
        """Test user endpoints"""
        self.log("Testing user endpoints")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        org_id = self.test_data.get("organisation", {}).get("organisationId")
        
        if not org_id:
            self.log("Organisation ID required", "ERROR")
            return False
        
        # Create user
        user_data = {
            "organisationId": org_id,
            "username": f"testuser_{uuid.uuid4().hex[:8]}",
            "firstName": "Test",
            "lastName": "User",
            "email": f"test{uuid.uuid4().hex[:8]}@example.com",
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
            
            if response.status_code in (200, 201):
                self.test_data["test_user"] = response.json()
                self.log("User created successfully")
            else:
                self.log(f"User creation failed: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"User creation error: {str(e)}", "ERROR")
            return False
        
        user_id = self.test_data["test_user"]["userId"]
        
        # Test get user by ID
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Users/{user_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get user by ID successful")
            else:
                self.log(f"Get user failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get user error: {str(e)}", "ERROR")
        
        # Test get user by username
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Users/by-username/{user_data['username']}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get user by username successful")
            else:
                self.log(f"Get user by username failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get user by username error: {str(e)}", "ERROR")
        
        # Test get users by organisation
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Users/by-organisation/{org_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get users by organisation successful")
            else:
                self.log(f"Get users by organisation failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get users by organisation error: {str(e)}", "ERROR")
        
        # Test get users by role
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Users/by-role/auditor",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get users by role successful")
            else:
                self.log(f"Get users by role failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get users by role error: {str(e)}", "ERROR")
        
        # Test update user
        update_data = {
            "userId": user_id,
            "firstName": "Updated",
            "lastName": "User",
            "email": f"updated{uuid.uuid4().hex[:8]}@example.com",
            "phone": "0987654321",
            "role": "auditor",
            "isActive": True
        }
        
        try:
            response = self.session.put(
                f"{self.base_url}/api/v1/Users/{user_id}",
                json=update_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("User update successful")
            else:
                self.log(f"User update failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"User update error: {str(e)}", "ERROR")
        
        # Test change password
        password_data = {
            "currentPassword": "TestPassword123!",
            "newPassword": "NewPassword123!"
        }
        
        try:
            response = self.session.patch(
                f"{self.base_url}/api/v1/Users/{user_id}/change-password",
                json=password_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Password change successful")
            else:
                self.log(f"Password change failed: {response.status_code}", "WARNING")
                
        except Exception as e:
            self.log(f"Password change error: {str(e)}", "WARNING")
        
        # Test deactivate user
        try:
            response = self.session.patch(
                f"{self.base_url}/api/v1/Users/{user_id}/deactivate",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("User deactivation successful")
            else:
                self.log(f"User deactivation failed: {response.status_code}", "WARNING")
                
        except Exception as e:
            self.log(f"User deactivation error: {str(e)}", "WARNING")
        
        # Test get all users
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Users",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get all users successful")
            else:
                self.log(f"Get all users failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get all users error: {str(e)}", "ERROR")
        
        return True
    
    def test_template_endpoints(self) -> bool:
        """Test template endpoints"""
        self.log("Testing template endpoints")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        # Create template
        template_data = {
            "name": f"Test Template {uuid.uuid4().hex[:8]}",
            "description": "A test template for API testing",
            "category": "Compliance",
            "questions": {
                "section1": [
                    {
                        "id": "q1",
                        "type": "text",
                        "question": "What is the store name?",
                        "required": True
                    },
                    {
                        "id": "q2",
                        "type": "number",
                        "question": "How many products are displayed?",
                        "required": False
                    }
                ]
            },
            "scoringRules": {
                "passThreshold": 80,
                "criticalQuestions": ["q1"]
            },
            "validFrom": (datetime.now() - timedelta(days=1)).isoformat(),
            "validTo": (datetime.now() + timedelta(days=30)).isoformat()
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Templates",
                json=template_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.test_data["template"] = response.json()
                self.log("Template created successfully")
            else:
                self.log(f"Template creation failed: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Template creation error: {str(e)}", "ERROR")
            return False
        
        template_id = self.test_data["template"]["templateId"]
        
        # Test get template by ID
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Templates/{template_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get template by ID successful")
            else:
                self.log(f"Get template failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get template error: {str(e)}", "ERROR")
        
        # Test update template
        update_data = {
            "name": f"Updated Template {uuid.uuid4().hex[:8]}",
            "description": "Updated description",
            "category": "Quality",
            "questions": template_data["questions"],
            "scoringRules": template_data["scoringRules"]
        }
        
        try:
            response = self.session.put(
                f"{self.base_url}/api/v1/Templates/{template_id}",
                json=update_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Template update successful")
            else:
                self.log(f"Template update failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Template update error: {str(e)}", "ERROR")
        
        # Test publish template
        try:
            response = self.session.put(
                f"{self.base_url}/api/v1/Templates/{template_id}/publish",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Template publish successful")
            else:
                self.log(f"Template publish failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Template publish error: {str(e)}", "ERROR")
        
        # Test get published templates
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Templates/published",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get published templates successful")
            else:
                self.log(f"Get published templates failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get published templates error: {str(e)}", "ERROR")
        
        # Test get templates by category
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Templates/category/Compliance",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get templates by category successful")
            else:
                self.log(f"Get templates by category failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get templates by category error: {str(e)}", "ERROR")
        
        # Test get assigned templates
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Templates/assigned",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get assigned templates successful")
            else:
                self.log(f"Get assigned templates failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get assigned templates error: {str(e)}", "ERROR")
        
        # Test get all templates
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Templates",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get all templates successful")
            else:
                self.log(f"Get all templates failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get all templates error: {str(e)}", "ERROR")
        
        return True
    
    def test_assignment_endpoints(self) -> bool:
        """Test assignment endpoints"""
        self.log("Testing assignment endpoints")
        
        if not self.auth_tokens.get("manager"):
            self.log("Manager authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("manager")
        
        # Get template and user IDs
        template_id = self.test_data.get("template", {}).get("templateId")
        user_id = self.test_data.get("test_user", {}).get("userId")
        org_id = self.test_data.get("organisation", {}).get("organisationId")
        
        if not all([template_id, user_id, org_id]):
            self.log("Template, user, and organisation IDs required", "ERROR")
            return False
        
        # Create assignment
        assignment_data = {
            "templateId": template_id,
            "assignedToId": user_id,
            "dueDate": (datetime.now() + timedelta(days=7)).isoformat(),
            "priority": "high",
            "notes": "Test assignment for API testing",
            "storeInfo": {
                "storeName": "Test Store",
                "location": "Test Location"
            }
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Assignments",
                json=assignment_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.test_data["assignment"] = response.json()
                self.log("Assignment created successfully")
            else:
                self.log(f"Assignment creation failed: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Assignment creation error: {str(e)}", "ERROR")
            return False
        
        assignment_id = self.test_data["assignment"]["assignmentId"]
        
        # Test get assignment by ID
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Assignments/{assignment_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get assignment by ID successful")
            else:
                self.log(f"Get assignment failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get assignment error: {str(e)}", "ERROR")
        
        # Test update assignment
        update_data = {
            "assignmentId": assignment_id,
            "dueDate": (datetime.now() + timedelta(days=14)).isoformat(),
            "priority": "medium",
            "notes": "Updated assignment notes",
            "status": "pending",
            "storeInfo": {
                "storeName": "Updated Store",
                "location": "Updated Location"
            }
        }
        
        try:
            response = self.session.put(
                f"{self.base_url}/api/v1/Assignments/{assignment_id}",
                json=update_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Assignment update successful")
            else:
                self.log(f"Assignment update failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Assignment update error: {str(e)}", "ERROR")
        
        # Test assign template to auditor
        assign_data = {
            "templateId": template_id,
            "auditorId": user_id,
            "dueDate": (datetime.now() + timedelta(days=10)).isoformat(),
            "priority": "high",
            "notes": "Direct assignment",
            "storeInfo": {
                "storeName": "Direct Store",
                "location": "Direct Location"
            }
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Assignments/assign",
                json=assign_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Direct assignment successful")
            else:
                self.log(f"Direct assignment failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Direct assignment error: {str(e)}", "ERROR")
        
        # Test get assignments by organisation
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Assignments/organisation/{org_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get assignments by organisation successful")
            else:
                self.log(f"Get assignments by organisation failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get assignments by organisation error: {str(e)}", "ERROR")
        
        # Test get assignments by auditor
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Assignments/auditor/{user_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get assignments by auditor successful")
            else:
                self.log(f"Get assignments by auditor failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get assignments by auditor error: {str(e)}", "ERROR")
        
        # Test get assignments by status
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Assignments/status/pending",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get assignments by status successful")
            else:
                self.log(f"Get assignments by status failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get assignments by status error: {str(e)}", "ERROR")
        
        # Test get pending assignments
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Assignments/pending",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get pending assignments successful")
            else:
                self.log(f"Get pending assignments failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get pending assignments error: {str(e)}", "ERROR")
        
        # Test get overdue assignments
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Assignments/overdue",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get overdue assignments successful")
            else:
                self.log(f"Get overdue assignments failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get overdue assignments error: {str(e)}", "ERROR")
        
        # Test update assignment status
        status_data = {
            "status": "fulfilled"
        }
        
        try:
            response = self.session.patch(
                f"{self.base_url}/api/v1/Assignments/{assignment_id}/status",
                json=status_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Assignment status update successful")
            else:
                self.log(f"Assignment status update failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Assignment status update error: {str(e)}", "ERROR")
        
        # Test get all assignments
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Assignments",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get all assignments successful")
            else:
                self.log(f"Get all assignments failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get all assignments error: {str(e)}", "ERROR")
        
        return True
    
    def test_audit_endpoints(self) -> bool:
        """Test audit endpoints"""
        self.log("Testing audit endpoints")
        
        if not self.auth_tokens.get("auditor"):
            self.log("Auditor authentication required", "ERROR")
            return False
        
        headers = self.get_auth_headers("auditor")
        
        # Get template and user IDs
        template_id = self.test_data.get("template", {}).get("templateId")
        user_id = self.test_data.get("test_user", {}).get("userId")
        org_id = self.test_data.get("organisation", {}).get("organisationId")
        
        if not all([template_id, user_id, org_id]):
            self.log("Template, user, and organisation IDs required", "ERROR")
            return False
        
        # Create audit
        audit_data = {
            "templateId": template_id,
            "storeName": "Test Store",
            "storeLocation": "Test Location",
            "storeInfo": {
                "storeName": "Test Store",
                "location": "Test Location",
                "type": "Retail"
            },
            "location": {
                "latitude": 40.7128,
                "longitude": -74.0060,
                "accuracy": 10
            },
            "responses": {
                "q1": "Test Store",
                "q2": 50
            },
            "media": {
                "photos": ["photo1.jpg", "photo2.jpg"],
                "signatures": ["signature1.png"]
            },
            "status": "in_progress",
            "score": 85.0,
            "criticalIssues": 0
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/api/v1/Audits",
                json=audit_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.test_data["audit"] = response.json()
                self.log("Audit created successfully")
            else:
                self.log(f"Audit creation failed: {response.status_code}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Audit creation error: {str(e)}", "ERROR")
            return False
        
        audit_id = self.test_data["audit"]["auditId"]
        
        # Test get audit by ID
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Audits/{audit_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get audit by ID successful")
            else:
                self.log(f"Get audit failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get audit error: {str(e)}", "ERROR")
        
        # Test submit audit
        submit_data = {
            "auditId": audit_id,
            "responses": {
                "q1": "Final Store Name",
                "q2": 75
            },
            "media": {
                "photos": ["final_photo1.jpg", "final_photo2.jpg"],
                "signatures": ["final_signature.png"]
            },
            "storeInfo": {
                "storeName": "Final Store",
                "location": "Final Location",
                "type": "Retail"
            },
            "location": {
                "latitude": 40.7128,
                "longitude": -74.0060,
                "accuracy": 5
            }
        }
        
        try:
            response = self.session.put(
                f"{self.base_url}/api/v1/Audits/{audit_id}/submit",
                json=submit_data,
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Audit submission successful")
            else:
                self.log(f"Audit submission failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Audit submission error: {str(e)}", "ERROR")
        
        # Test update audit status (as manager)
        if self.auth_tokens.get("manager"):
            manager_headers = self.get_auth_headers("manager")
            status_data = {
                "status": "approved",
                "managerNotes": "Good work on this audit",
                "isFlagged": False
            }
            
            try:
                response = self.session.patch(
                    f"{self.base_url}/api/v1/Audits/{audit_id}/status",
                    json=status_data,
                    headers=manager_headers
                )
                
                if response.status_code == 200:
                    self.log("Audit status update successful")
                else:
                    self.log(f"Audit status update failed: {response.status_code}", "ERROR")
                    
            except Exception as e:
                self.log(f"Audit status update error: {str(e)}", "ERROR")
        
        # Test flag audit
        try:
            response = self.session.patch(
                f"{self.base_url}/api/v1/Audits/{audit_id}/flag",
                json=True,
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Audit flag successful")
            else:
                self.log(f"Audit flag failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Audit flag error: {str(e)}", "ERROR")
        
        # Test get audits by auditor
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Audits/by-auditor/{user_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get audits by auditor successful")
            else:
                self.log(f"Get audits by auditor failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get audits by auditor error: {str(e)}", "ERROR")
        
        # Test get audits by organisation
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Audits/by-organisation/{org_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get audits by organisation successful")
            else:
                self.log(f"Get audits by organisation failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get audits by organisation error: {str(e)}", "ERROR")
        
        # Test get audits by template
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Audits/by-template/{template_id}",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get audits by template successful")
            else:
                self.log(f"Get audits by template failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get audits by template error: {str(e)}", "ERROR")
        
        # Test get all audits
        try:
            response = self.session.get(
                f"{self.base_url}/api/v1/Audits",
                headers=headers
            )
            
            if response.status_code == 200:
                self.log("Get all audits successful")
            else:
                self.log(f"Get all audits failed: {response.status_code}", "ERROR")
                
        except Exception as e:
            self.log(f"Get all audits error: {str(e)}", "ERROR")
        
        return True
    
    def run_all_tests(self) -> bool:
        """Run all test suites"""
        self.log("Starting comprehensive API test suite")
        
        # Test health check first
        if not self.test_health_check():
            self.log("Health check failed, stopping tests", "ERROR")
            return False
        
        # Test authentication
        if not self.test_auth_endpoints():
            self.log("Authentication tests failed", "ERROR")
            return False
        
        # Test organisation endpoints
        if not self.test_organisation_endpoints():
            self.log("Organisation tests failed", "ERROR")
            return False
        
        # Test user endpoints
        if not self.test_user_endpoints():
            self.log("User tests failed", "ERROR")
            return False
        
        # Test template endpoints
        if not self.test_template_endpoints():
            self.log("Template tests failed", "ERROR")
            return False
        
        # Test assignment endpoints
        if not self.test_assignment_endpoints():
            self.log("Assignment tests failed", "ERROR")
            return False
        
        # Test audit endpoints
        if not self.test_audit_endpoints():
            self.log("Audit tests failed", "ERROR")
            return False
        
        self.log("All test suites completed successfully!")
        return True

def main():
    """Main function to run the test suite"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Comprehensive API Test Suite")
    parser.add_argument("--base-url", default="http://localhost:8080", 
                       help="Base URL of the API (default: http://localhost:8080)")
    parser.add_argument("--verbose", "-v", action="store_true", 
                       help="Enable verbose logging")
    
    args = parser.parse_args()
    
    # Create and run test suite
    test_suite = APITestSuite(args.base_url)
    
    try:
        success = test_suite.run_all_tests()
        if success:
            print("\n✅ All tests passed successfully!")
            sys.exit(0)
        else:
            print("\n❌ Some tests failed!")
            sys.exit(1)
    except KeyboardInterrupt:
        print("\n⚠️  Tests interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n💥 Unexpected error: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main() 