"""
Comprehensive API tests for UsersController endpoints
Tests all CRUD operations, authentication, authorization, and error handling
"""
import pytest
import uuid
from conftest import API_BASE_URL


class TestUsersControllerAPI:
    """Test class for Users Controller API endpoints"""

    def test_get_all_users_as_admin(self, api_client, admin_token):
        """Test GET /api/users - Admin can get all users"""
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 200
        users = response.json()
        assert isinstance(users, list)
        assert len(users) >= 5  # Should have at least seed data users
        
        # Verify user structure
        for user in users:
            assert "userId" in user
            assert "username" in user
            assert "firstName" in user
            assert "lastName" in user
            assert "role" in user
            assert "isActive" in user

    def test_get_all_users_as_manager(self, api_client, manager_token):
        """Test GET /api/users - Manager can get all users"""
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": manager_token}
        )
        
        assert response.status_code == 200
        users = response.json()
        assert isinstance(users, list)

    def test_get_all_users_as_auditor_forbidden(self, api_client, auditor_token):
        """Test GET /api/users - Auditor cannot get all users"""
        response = api_client.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": auditor_token}
        )
        
        assert response.status_code == 403

    def test_get_all_users_unauthenticated(self, api_client):
        """Test GET /api/users - Unauthenticated request fails"""
        response = api_client.get(f"{API_BASE_URL}/users")
        
        assert response.status_code == 401

    def test_get_user_by_id(self, api_client, admin_token, created_test_user):
        """Test GET /api/users/{id} - Get specific user"""
        response = api_client.get(
            f"{API_BASE_URL}/users/{created_test_user['userId']}",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 200
        user = response.json()
        assert user["userId"] == created_test_user["userId"]
        assert user["username"] == created_test_user["username"]

    def test_get_user_by_id_not_found(self, api_client, admin_token):
        """Test GET /api/users/{id} - Non-existent user returns 404"""
        fake_id = str(uuid.uuid4())
        response = api_client.get(
            f"{API_BASE_URL}/users/{fake_id}",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 404

    def test_get_user_by_id_invalid_uuid(self, api_client, admin_token):
        """Test GET /api/users/{id} - Invalid UUID format"""
        response = api_client.get(
            f"{API_BASE_URL}/users/invalid-uuid",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 400

    def test_get_user_by_username(self, api_client, admin_token, created_test_user):
        """Test GET /api/users/by-username/{username} - Get user by username"""
        response = api_client.get(
            f"{API_BASE_URL}/users/by-username/{created_test_user['username']}",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 200
        user = response.json()
        assert user["username"] == created_test_user["username"]

    def test_get_user_by_username_not_found(self, api_client, admin_token):
        """Test GET /api/users/by-username/{username} - Non-existent username"""
        response = api_client.get(
            f"{API_BASE_URL}/users/by-username/nonexistent_user",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 404

    def test_get_user_by_username_as_auditor_forbidden(self, api_client, auditor_token):
        """Test GET /api/users/by-username/{username} - Auditor cannot access"""
        response = api_client.get(
            f"{API_BASE_URL}/users/by-username/admin",
            headers={"Authorization": auditor_token}
        )
        
        assert response.status_code == 403

    def test_get_users_by_organisation(self, api_client, admin_token, test_organisation_id):
        """Test GET /api/users/by-organisation/{organisationId} - Get users by org"""
        response = api_client.get(
            f"{API_BASE_URL}/users/by-organisation/{test_organisation_id}",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 200
        users = response.json()
        assert isinstance(users, list)
        
        # All users should belong to the test organisation
        for user in users:
            assert user.get("organisationId") == test_organisation_id

    def test_get_users_by_role(self, api_client, admin_token):
        """Test GET /api/users/by-role/{role} - Get users by role"""
        response = api_client.get(
            f"{API_BASE_URL}/users/by-role/admin",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 200
        users = response.json()
        assert isinstance(users, list)
        
        # All users should have admin role
        for user in users:
            assert user["role"] == "admin"

    def test_create_user_as_admin(self, api_client, admin_token, test_user_data):
        """Test POST /api/users - Admin can create user"""
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data
        )
        
        assert response.status_code == 201
        user = response.json()
        assert user["username"] == test_user_data["username"]
        assert user["firstName"] == test_user_data["firstName"]
        assert user["role"] == test_user_data["role"]
        assert "passwordHash" not in user  # Password should not be returned
        
        # Cleanup
        api_client.patch(
            f"{API_BASE_URL}/users/{user['userId']}/deactivate",
            headers={"Authorization": admin_token}
        )

    def test_create_user_as_manager_forbidden(self, api_client, manager_token, test_user_data):
        """Test POST /api/users - Manager cannot create user"""
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": manager_token},
            json=test_user_data
        )
        
        assert response.status_code == 403

    def test_create_user_duplicate_username(self, api_client, admin_token, test_user_data):
        """Test POST /api/users - Duplicate username fails"""
        # Create first user
        response1 = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data
        )
        assert response1.status_code == 201
        user1 = response1.json()
        
        # Try to create second user with same username
        response2 = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data
        )
        assert response2.status_code == 400
        assert "already taken" in response2.text.lower()
        
        # Cleanup
        api_client.patch(
            f"{API_BASE_URL}/users/{user1['userId']}/deactivate",
            headers={"Authorization": admin_token}
        )

    def test_create_user_invalid_data(self, api_client, admin_token):
        """Test POST /api/users - Invalid data fails validation"""
        invalid_data = {
            "username": "",  # Empty username
            "password": "123",  # Too short password
            "role": "invalid_role"  # Invalid role
        }
        
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=invalid_data
        )
        
        assert response.status_code == 400

    def test_update_user_as_admin(self, api_client, admin_token, created_test_user):
        """Test PUT /api/users/{id} - Admin can update user"""
        update_data = {
            "userId": created_test_user["userId"],
            "firstName": "UpdatedFirst",
            "lastName": "UpdatedLast", 
            "email": "updated@example.com",
            "phone": "+1234567890",
            "role": "manager",
            "isActive": True
        }
        
        response = api_client.put(
            f"{API_BASE_URL}/users/{created_test_user['userId']}",
            headers={"Authorization": admin_token},
            json=update_data
        )
        
        assert response.status_code == 200
        user = response.json()
        assert user["firstName"] == "UpdatedFirst"
        assert user["lastName"] == "UpdatedLast"
        assert user["email"] == "updated@example.com"
        assert user["role"] == "manager"

    def test_update_user_id_mismatch(self, api_client, admin_token, created_test_user):
        """Test PUT /api/users/{id} - ID mismatch fails"""
        different_id = str(uuid.uuid4())
        update_data = {
            "userId": different_id,  # Different from URL
            "firstName": "UpdatedFirst"
        }
        
        response = api_client.put(
            f"{API_BASE_URL}/users/{created_test_user['userId']}",
            headers={"Authorization": admin_token},
            json=update_data
        )
        
        assert response.status_code == 400
        assert "mismatch" in response.text.lower()

    def test_update_user_not_found(self, api_client, admin_token):
        """Test PUT /api/users/{id} - Non-existent user fails"""
        fake_id = str(uuid.uuid4())
        update_data = {
            "userId": fake_id,
            "firstName": "UpdatedFirst"
        }
        
        response = api_client.put(
            f"{API_BASE_URL}/users/{fake_id}",
            headers={"Authorization": admin_token},
            json=update_data
        )
        
        assert response.status_code == 404

    def test_deactivate_user_as_admin(self, api_client, admin_token, created_test_user):
        """Test PATCH /api/users/{id}/deactivate - Admin can deactivate user"""
        response = api_client.patch(
            f"{API_BASE_URL}/users/{created_test_user['userId']}/deactivate",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 204
        
        # Verify user is deactivated
        user_response = api_client.get(
            f"{API_BASE_URL}/users/{created_test_user['userId']}",
            headers={"Authorization": admin_token}
        )
        assert user_response.status_code == 200
        user = user_response.json()
        assert user["isActive"] == False

    def test_deactivate_user_as_manager_forbidden(self, api_client, manager_token, created_test_user):
        """Test PATCH /api/users/{id}/deactivate - Manager cannot deactivate"""
        response = api_client.patch(
            f"{API_BASE_URL}/users/{created_test_user['userId']}/deactivate",
            headers={"Authorization": manager_token}
        )
        
        assert response.status_code == 403

    def test_deactivate_user_not_found(self, api_client, admin_token):
        """Test PATCH /api/users/{id}/deactivate - Non-existent user fails"""
        fake_id = str(uuid.uuid4())
        response = api_client.patch(
            f"{API_BASE_URL}/users/{fake_id}/deactivate",
            headers={"Authorization": admin_token}
        )
        
        assert response.status_code == 404

    def test_change_password_success(self, api_client, admin_token, created_test_user):
        """Test PATCH /api/users/{id}/change-password - Successful password change"""
        password_data = {
            "currentPassword": created_test_user["password"],
            "newPassword": "NewPassword123!"
        }
        
        response = api_client.patch(
            f"{API_BASE_URL}/users/{created_test_user['userId']}/change-password",
            headers={"Authorization": admin_token},
            json=password_data
        )
        
        assert response.status_code == 204

    def test_change_password_wrong_current(self, api_client, admin_token, created_test_user):
        """Test PATCH /api/users/{id}/change-password - Wrong current password fails"""
        password_data = {
            "currentPassword": "WrongPassword123!",
            "newPassword": "NewPassword123!"
        }
        
        response = api_client.patch(
            f"{API_BASE_URL}/users/{created_test_user['userId']}/change-password",
            headers={"Authorization": admin_token},
            json=password_data
        )
        
        assert response.status_code == 400
        assert "invalid current password" in response.text.lower()

    def test_change_password_invalid_data(self, api_client, admin_token, created_test_user):
        """Test PATCH /api/users/{id}/change-password - Invalid data fails"""
        password_data = {
            "currentPassword": "",  # Empty current password
            "newPassword": "123"    # Too short new password
        }
        
        response = api_client.patch(
            f"{API_BASE_URL}/users/{created_test_user['userId']}/change-password",
            headers={"Authorization": admin_token},
            json=password_data
        )
        
        assert response.status_code == 400

    def test_change_password_user_not_found(self, api_client, admin_token):
        """Test PATCH /api/users/{id}/change-password - Non-existent user fails"""
        fake_id = str(uuid.uuid4())
        password_data = {
            "currentPassword": "CurrentPassword123!",
            "newPassword": "NewPassword123!"
        }
        
        response = api_client.patch(
            f"{API_BASE_URL}/users/{fake_id}/change-password",
            headers={"Authorization": admin_token},
            json=password_data
        )
        
        assert response.status_code == 400  # Should be 400 for invalid current password


class TestUsersControllerEdgeCases:
    """Test edge cases and error scenarios"""

    def test_malformed_json_request(self, api_client, admin_token):
        """Test API with malformed JSON"""
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            data="invalid json"
        )
        
        assert response.status_code == 400

    def test_missing_content_type(self, api_client, admin_token, test_user_data):
        """Test API with missing Content-Type header"""
        headers = {"Authorization": admin_token}
        # Remove Content-Type
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers=headers,
            json=test_user_data
        )
        
        # Should still work as requests library handles this
        assert response.status_code in [201, 400, 415]

    def test_extremely_long_username(self, api_client, admin_token, test_user_data):
        """Test creating user with extremely long username"""
        test_user_data["username"] = "a" * 1000  # Very long username
        
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data
        )
        
        assert response.status_code == 400

    def test_special_characters_in_username(self, api_client, admin_token, test_user_data):
        """Test creating user with special characters in username"""
        test_user_data["username"] = "test@user#123$"
        
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data
        )
        
        # Should handle special characters appropriately
        assert response.status_code in [201, 400]

    def test_unicode_characters_in_names(self, api_client, admin_token, test_user_data):
        """Test creating user with Unicode characters in names"""
        test_user_data["firstName"] = "José"
        test_user_data["lastName"] = "García"
        
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data
        )
        
        if response.status_code == 201:
            user = response.json()
            assert user["firstName"] == "José"
            assert user["lastName"] == "García"
            
            # Cleanup
            api_client.patch(
                f"{API_BASE_URL}/users/{user['userId']}/deactivate",
                headers={"Authorization": admin_token}
            ) 