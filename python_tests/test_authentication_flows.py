"""
Comprehensive tests for authentication and authorization flows
Tests JWT token handling, role-based access control, and auth edge cases
"""
import pytest
import jwt
from datetime import datetime, timedelta
from conftest import API_BASE_URL, JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE


class TestAuthenticationFlows:
    """Test authentication mechanisms and JWT token handling"""

    def test_valid_admin_token_access(self, api_client, admin_token):
        """Test valid admin token allows access to admin endpoints"""
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 200

    def test_valid_manager_token_access(self, api_client, manager_token):
        """Test valid manager token allows access to manager endpoints"""
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": manager_token}
        )
        
        assert response.status_code == 200

    def test_valid_auditor_token_limited_access(self, api_client, auditor_token):
        """Test auditor token has limited access"""
        # Should be able to access own profile endpoint (if exists)
        # Should NOT be able to access admin endpoints
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": auditor_token}
        )
        
        assert response.status_code == 403

    def test_no_authorization_header(self, api_client):
        """Test request without Authorization header fails"""
        response = api_client.get(f"{API_BASE_URL}/users")
        
        assert response.status_code == 401

    def test_malformed_authorization_header(self, api_client):
        """Test request with malformed Authorization header fails"""
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": "InvalidTokenFormat"}
        )
        
        assert response.status_code == 401

    def test_invalid_token_signature(self, api_client):
        """Test request with invalid token signature fails"""
        # Create token with wrong secret
        payload = {
            'sub': '550e8400-e29b-41d4-a716-446655440000',
            'username': 'admin',
            'role': 'admin',
            'iss': JWT_ISSUER,
            'aud': JWT_AUDIENCE,
            'iat': datetime.utcnow(),
            'exp': datetime.utcnow() + timedelta(hours=1)
        }
        invalid_token = jwt.encode(payload, "wrong_secret", algorithm='HS256')
        
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": f"Bearer {invalid_token}"}
        )
        
        assert response.status_code == 401

    def test_expired_token(self, api_client):
        """Test request with expired token fails"""
        # Create expired token
        payload = {
            'sub': '550e8400-e29b-41d4-a716-446655440000',
            'username': 'admin',
            'role': 'admin',
            'iss': JWT_ISSUER,
            'aud': JWT_AUDIENCE,
            'iat': datetime.utcnow() - timedelta(hours=2),
            'exp': datetime.utcnow() - timedelta(hours=1)  # Expired 1 hour ago
        }
        expired_token = jwt.encode(payload, JWT_SECRET, algorithm='HS256')
        
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": f"Bearer {expired_token}"}
        )
        
        assert response.status_code == 401

    def test_token_invalid_issuer(self, api_client):
        """Test token with invalid issuer fails"""
        payload = {
            'sub': '550e8400-e29b-41d4-a716-446655440000',
            'username': 'admin',
            'role': 'admin',
            'iss': 'WrongIssuer',  # Invalid issuer
            'aud': JWT_AUDIENCE,
            'iat': datetime.utcnow(),
            'exp': datetime.utcnow() + timedelta(hours=1)
        }
        invalid_token = jwt.encode(payload, JWT_SECRET, algorithm='HS256')
        
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": f"Bearer {invalid_token}"}
        )
        
        assert response.status_code == 401

    def test_token_invalid_audience(self, api_client):
        """Test token with invalid audience fails"""
        payload = {
            'sub': '550e8400-e29b-41d4-a716-446655440000',
            'username': 'admin',
            'role': 'admin',
            'iss': JWT_ISSUER,
            'aud': 'WrongAudience',  # Invalid audience
            'iat': datetime.utcnow(),
            'exp': datetime.utcnow() + timedelta(hours=1)
        }
        invalid_token = jwt.encode(payload, JWT_SECRET, algorithm='HS256')
        
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": f"Bearer {invalid_token}"}
        )
        
        assert response.status_code == 401

    def test_token_missing_required_claims(self, api_client):
        """Test token missing required claims fails"""
        # Token without 'sub' claim
        payload = {
            'username': 'admin',
            'role': 'admin',
            'iss': JWT_ISSUER,
            'aud': JWT_AUDIENCE,
            'iat': datetime.utcnow(),
            'exp': datetime.utcnow() + timedelta(hours=1)
            # Missing 'sub' claim
        }
        invalid_token = jwt.encode(payload, JWT_SECRET, algorithm='HS256')
        
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": f"Bearer {invalid_token}"}
        )
        
        assert response.status_code == 401


class TestRoleBasedAccessControl:
    """Test role-based access control for different endpoints"""

    def test_admin_can_create_users(self, api_client, admin_token, test_user_data):
        """Test admin can create users"""
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data
        )
        
        assert response.status_code == 201
        
        # Cleanup
        if response.status_code == 201:
            user = response.json()
            api_client.patch(
                f"{API_BASE_URL}/users/{user['userId']}/deactivate",
                headers={"Authorization": admin_token}
            )

    def test_manager_cannot_create_users(self, api_client, manager_token, test_user_data):
        """Test manager cannot create users"""
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": manager_token},
            json=test_user_data
        )
        
        assert response.status_code == 403

    def test_auditor_cannot_create_users(self, api_client, auditor_token, test_user_data):
        """Test auditor cannot create users"""
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": auditor_token},
            json=test_user_data
        )
        
        assert response.status_code == 403

    def test_admin_can_deactivate_users(self, api_client, admin_token, created_test_user):
        """Test admin can deactivate users"""
        response = api_client.patch(
            f"{API_BASE_URL}/users/{created_test_user['userId']}/deactivate",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 204

    def test_manager_cannot_deactivate_users(self, api_client, manager_token, created_test_user):
        """Test manager cannot deactivate users"""
        response = api_client.patch(
            f"{API_BASE_URL}/users/{created_test_user['userId']}/deactivate",
            headers={"Authorization": manager_token}
        )
        
        assert response.status_code == 403

    def test_auditor_cannot_deactivate_users(self, api_client, auditor_token, created_test_user):
        """Test auditor cannot deactivate users"""
        response = api_client.patch(
            f"{API_BASE_URL}/users/{created_test_user['userId']}/deactivate",
            headers={"Authorization": auditor_token}
        )
        
        assert response.status_code == 403

    def test_admin_can_view_users(self, api_client, admin_token):
        """Test admin can view all users"""
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 200

    def test_manager_can_view_users(self, api_client, manager_token):
        """Test manager can view all users"""
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": manager_token}
        )
        
        assert response.status_code == 200

    def test_auditor_cannot_view_all_users(self, api_client, auditor_token):
        """Test auditor cannot view all users"""
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": auditor_token}
        )
        
        assert response.status_code == 403

    def test_admin_can_view_user_by_username(self, api_client, admin_token):
        """Test admin can view user by username"""
        response = api_client.get(
            f"{API_BASE_URL}/users/by-username/admin",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 200

    def test_manager_can_view_user_by_username(self, api_client, manager_token):
        """Test manager can view user by username"""
        response = api_client.get(
            f"{API_BASE_URL}/users/by-username/admin",
            headers={"Authorization": manager_token}
        )
        
        assert response.status_code == 200

    def test_auditor_cannot_view_user_by_username(self, api_client, auditor_token):
        """Test auditor cannot view user by username"""
        response = api_client.get(
            f"{API_BASE_URL}/users/by-username/admin",
            headers={"Authorization": auditor_token}
        )
        
        assert response.status_code == 403

    def test_user_can_view_own_profile(self, api_client, auditor_token):
        """Test user can view their own profile"""
        # Get auditor's own profile
        response = api_client.get(
            f"{API_BASE_URL}/users/550e8400-e29b-41d4-a716-446655440003",
            headers={"Authorization": auditor_token}
        )
        
        assert response.status_code == 200

    def test_admin_can_update_any_user(self, api_client, admin_token, created_test_user):
        """Test admin can update any user"""
        update_data = {
            "userId": created_test_user["userId"],
            "firstName": "AdminUpdated",
            "lastName": created_test_user["lastName"],
            "email": created_test_user["email"],
            "phone": created_test_user["phone"],
            "role": created_test_user["role"],
            "isActive": True
        }
        
        response = api_client.put(
            f"{API_BASE_URL}/users/{created_test_user['userId']}",
            headers={"Authorization": admin_token},
            json=update_data
        )
        
        assert response.status_code == 200
        user = response.json()
        assert user["firstName"] == "AdminUpdated"

    def test_manager_can_update_users(self, api_client, manager_token, created_test_user):
        """Test manager can update users"""
        update_data = {
            "userId": created_test_user["userId"],
            "firstName": "ManagerUpdated",
            "lastName": created_test_user["lastName"],
            "email": created_test_user["email"],
            "phone": created_test_user["phone"],
            "role": created_test_user["role"],
            "isActive": True
        }
        
        response = api_client.put(
            f"{API_BASE_URL}/users/{created_test_user['userId']}",
            headers={"Authorization": manager_token},
            json=update_data
        )
        
        assert response.status_code == 200

    def test_auditor_cannot_update_users(self, api_client, auditor_token, created_test_user):
        """Test auditor cannot update other users"""
        update_data = {
            "userId": created_test_user["userId"],
            "firstName": "AuditorUpdated",
            "lastName": created_test_user["lastName"],
            "email": created_test_user["email"],
            "phone": created_test_user["phone"],
            "role": created_test_user["role"],
            "isActive": True
        }
        
        response = api_client.put(
            f"{API_BASE_URL}/users/{created_test_user['userId']}",
            headers={"Authorization": auditor_token},
            json=update_data
        )
        
        assert response.status_code == 403


class TestTokenEdgeCases:
    """Test edge cases and security scenarios for JWT tokens"""

    def test_token_with_future_issued_at(self, api_client):
        """Test token with future 'iat' claim"""
        payload = {
            'sub': '550e8400-e29b-41d4-a716-446655440000',
            'username': 'admin',
            'role': 'admin',
            'iss': JWT_ISSUER,
            'aud': JWT_AUDIENCE,
            'iat': datetime.utcnow() + timedelta(hours=1),  # Future iat
            'exp': datetime.utcnow() + timedelta(hours=2)
        }
        future_token = jwt.encode(payload, JWT_SECRET, algorithm='HS256')
        
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": f"Bearer {future_token}"}
        )
        
        # Should either accept or reject based on implementation
        assert response.status_code in [200, 401]

    def test_token_with_invalid_role(self, api_client):
        """Test token with non-existent role"""
        payload = {
            'sub': '550e8400-e29b-41d4-a716-446655440000',
            'username': 'admin',
            'role': 'super_admin',  # Invalid role
            'iss': JWT_ISSUER,
            'aud': JWT_AUDIENCE,
            'iat': datetime.utcnow(),
            'exp': datetime.utcnow() + timedelta(hours=1)
        }
        invalid_role_token = jwt.encode(payload, JWT_SECRET, algorithm='HS256')
        
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": f"Bearer {invalid_role_token}"}
        )
        
        # Should either reject or handle gracefully
        assert response.status_code in [200, 401, 403]

    def test_very_long_token(self, api_client):
        """Test extremely long JWT token"""
        payload = {
            'sub': '550e8400-e29b-41d4-a716-446655440000',
            'username': 'admin',
            'role': 'admin',
            'iss': JWT_ISSUER,
            'aud': JWT_AUDIENCE,
            'iat': datetime.utcnow(),
            'exp': datetime.utcnow() + timedelta(hours=1),
            'extra_data': 'a' * 10000  # Very long additional data
        }
        long_token = jwt.encode(payload, JWT_SECRET, algorithm='HS256')
        
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": f"Bearer {long_token}"}
        )
        
        # Should handle long tokens appropriately
        assert response.status_code in [200, 400, 413]

    def test_multiple_authorization_headers(self, api_client, admin_token):
        """Test request with multiple Authorization headers"""
        headers = {
            "Authorization": [admin_token, "Bearer fake_token"]
        }
        
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers=headers
        )
        
        # Should handle multiple headers appropriately
        assert response.status_code in [200, 400, 401]

    def test_case_sensitive_bearer_prefix(self, api_client, admin_token):
        """Test different cases for Bearer prefix"""
        token_part = admin_token.split(' ')[1]
        
        test_cases = [
            f"bearer {token_part}",
            f"BEARER {token_part}",
            f"Bearer {token_part}",
            f"BeArEr {token_part}"
        ]
        
        for auth_header in test_cases:
            response = api_client.get(
                f"{API_BASE_URL}/users",
                headers={"Authorization": auth_header}
            )
            
            # Should handle case variations appropriately
            assert response.status_code in [200, 401]


class TestConcurrentAuthentication:
    """Test concurrent authentication scenarios"""

    def test_same_token_multiple_requests(self, api_client, admin_token):
        """Test using same token for multiple concurrent requests"""
        import threading
        import time
        
        results = []
        
        def make_request():
            response = api_client.get(
                f"{API_BASE_URL}/users",
                headers={"Authorization": admin_token}
            )
            results.append(response.status_code)
        
        # Create multiple threads
        threads = []
        for _ in range(5):
            thread = threading.Thread(target=make_request)
            threads.append(thread)
        
        # Start all threads
        for thread in threads:
            thread.start()
        
        # Wait for all threads to complete
        for thread in threads:
            thread.join()
        
        # All requests should succeed
        assert all(status == 200 for status in results)
        assert len(results) == 5 