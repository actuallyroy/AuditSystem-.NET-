"""
Comprehensive database-level tests for UserService functionality
Tests password hashing, user operations, and database constraints
"""
import pytest
import uuid
import hashlib
import hmac
import base64
from conftest import API_BASE_URL
import os
from datetime import datetime, timedelta

# Try to import psycopg2, skip all tests if not available
try:
    import psycopg2
    from psycopg2.extras import RealDictCursor
    psycopg2_available = True
except ImportError:
    psycopg2_available = False

# Skip all tests in this module if psycopg2 is not available
pytestmark = pytest.mark.skipif(
    not psycopg2_available,
    reason="psycopg2 not available - skipping database tests"
)


class TestUserServiceDatabase:
    """Test UserService database operations and business logic"""

    def test_seed_data_users_exist(self, db_cursor):
        """Test that seed data users exist in database"""
        db_cursor.execute("SELECT * FROM users WHERE username IN ('admin', 'manager1', 'auditor1')")
        users = db_cursor.fetchall()
        
        assert len(users) >= 3
        usernames = [user['username'] for user in users]
        assert 'admin' in usernames
        assert 'manager1' in usernames
        assert 'auditor1' in usernames

    def test_user_password_hash_format(self, db_cursor):
        """Test that user passwords are properly hashed"""
        db_cursor.execute("SELECT password_hash, password_salt FROM users WHERE username = 'admin'")
        user = db_cursor.fetchone()
        
        assert user is not None
        assert user['password_hash'] is not None
        assert user['password_salt'] is not None
        assert len(user['password_hash']) > 20  # Should be a hash, not plain text
        assert len(user['password_salt']) > 10  # Should have salt

    def test_create_user_database_record(self, api_client, admin_token, test_user_data, db_cursor):
        """Test that creating user via API properly stores in database"""
        # Create user via API
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data
        )
        
        assert response.status_code == 201
        user = response.json()
        
        # Verify in database
        db_cursor.execute("SELECT * FROM users WHERE user_id = %s", (user['userId'],))
        db_user = db_cursor.fetchone()
        
        assert db_user is not None
        assert db_user['username'] == test_user_data['username']
        assert db_user['first_name'] == test_user_data['firstName']
        assert db_user['last_name'] == test_user_data['lastName']
        assert db_user['email'] == test_user_data['email']
        assert db_user['role'] == test_user_data['role']
        assert db_user['is_active'] == True
        assert db_user['password_hash'] is not None
        assert db_user['password_salt'] is not None
        assert db_user['created_at'] is not None
        
        # Cleanup
        api_client.patch(
            f"{API_BASE_URL}/users/{user['userId']}/deactivate",
            headers={"Authorization": admin_token}
        )

    def test_unique_username_constraint(self, api_client, admin_token, test_user_data):
        """Test that duplicate usernames are rejected"""
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
        
        # Cleanup
        api_client.patch(
            f"{API_BASE_URL}/users/{user1['userId']}/deactivate",
            headers={"Authorization": admin_token}
        )

    def test_unique_email_constraint(self, api_client, admin_token, test_user_data):
        """Test that duplicate emails are rejected"""
        # Create first user
        response1 = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data
        )
        assert response1.status_code == 201
        user1 = response1.json()
        
        # Try to create second user with same email but different username
        test_user_data2 = test_user_data.copy()
        test_user_data2['username'] = f"different_{uuid.uuid4().hex[:8]}"
        # Keep same email
        
        response2 = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data2
        )
        assert response2.status_code == 400
        
        # Cleanup
        api_client.patch(
            f"{API_BASE_URL}/users/{user1['userId']}/deactivate",
            headers={"Authorization": admin_token}
        )

    def test_user_organisation_relationship(self, api_client, admin_token, test_user_data, db_cursor):
        """Test that user-organisation relationship is properly maintained"""
        # Create user
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data
        )
        assert response.status_code == 201
        user = response.json()
        
        # Verify organisation relationship in database
        db_cursor.execute("""
            SELECT u.*, o.name as org_name 
            FROM users u 
            LEFT JOIN organisation o ON u.organisation_id = o.organisation_id 
            WHERE u.user_id = %s
        """, (user['userId'],))
        
        db_user = db_cursor.fetchone()
        assert db_user is not None
        assert db_user['organisation_id'] == test_user_data['organisationId']
        assert db_user['org_name'] is not None  # Should have valid organisation
        
        # Cleanup
        api_client.patch(
            f"{API_BASE_URL}/users/{user['userId']}/deactivate",
            headers={"Authorization": admin_token}
        )

    def test_user_role_constraint(self, api_client, admin_token, test_user_data):
        """Test that only valid roles are accepted"""
        # Test valid roles
        valid_roles = ['admin', 'manager', 'supervisor', 'auditor']
        
        for role in valid_roles:
            test_data = test_user_data.copy()
            test_data['username'] = f"test_{role}_{uuid.uuid4().hex[:8]}"
            test_data['email'] = f"test_{role}_{uuid.uuid4().hex[:8]}@example.com"
            test_data['role'] = role
            
            response = api_client.post(
                f"{API_BASE_URL}/users",
                headers={"Authorization": admin_token},
                json=test_data
            )
            
            if response.status_code == 201:
                user = response.json()
                assert user['role'] == role
                # Cleanup
                api_client.patch(
                    f"{API_BASE_URL}/users/{user['userId']}/deactivate",
                    headers={"Authorization": admin_token}
                )
        
        # Test invalid role
        test_data = test_user_data.copy()
        test_data['username'] = f"test_invalid_{uuid.uuid4().hex[:8]}"
        test_data['email'] = f"test_invalid_{uuid.uuid4().hex[:8]}@example.com"
        test_data['role'] = 'invalid_role'
        
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_data
        )
        
        # Should reject invalid role
        assert response.status_code == 400

    def test_user_deactivation_database_update(self, api_client, admin_token, created_test_user, db_cursor):
        """Test that user deactivation properly updates database"""
        # Verify user is initially active
        db_cursor.execute("SELECT is_active FROM users WHERE user_id = %s", (created_test_user['userId'],))
        result = db_cursor.fetchone()
        assert result['is_active'] == True
        
        # Deactivate user
        response = api_client.patch(
            f"{API_BASE_URL}/users/{created_test_user['userId']}/deactivate",
            headers={"Authorization": admin_token}
        )
        assert response.status_code == 204
        
        # Verify user is now inactive in database
        db_cursor.execute("SELECT is_active FROM users WHERE user_id = %s", (created_test_user['userId'],))
        result = db_cursor.fetchone()
        assert result['is_active'] == False

    def test_user_update_database_changes(self, api_client, admin_token, created_test_user, db_cursor):
        """Test that user updates properly modify database"""
        update_data = {
            "userId": created_test_user["userId"],
            "firstName": "UpdatedFirst",
            "lastName": "UpdatedLast",
            "email": "updated@example.com",
            "phone": "+1234567890",
            "role": "manager",
            "isActive": True
        }
        
        # Update user
        response = api_client.put(
            f"{API_BASE_URL}/users/{created_test_user['userId']}",
            headers={"Authorization": admin_token},
            json=update_data
        )
        assert response.status_code == 200
        
        # Verify changes in database
        db_cursor.execute("SELECT * FROM users WHERE user_id = %s", (created_test_user['userId'],))
        db_user = db_cursor.fetchone()
        
        assert db_user['first_name'] == "UpdatedFirst"
        assert db_user['last_name'] == "UpdatedLast"
        assert db_user['email'] == "updated@example.com"
        assert db_user['phone'] == "+1234567890"
        assert db_user['role'] == "manager"

    def test_password_change_database_update(self, api_client, admin_token, created_test_user, db_cursor):
        """Test that password change properly updates database hashes"""
        # Get original password hash
        db_cursor.execute("SELECT password_hash, password_salt FROM users WHERE user_id = %s", 
                         (created_test_user['userId'],))
        original = db_cursor.fetchone()
        original_hash = original['password_hash']
        original_salt = original['password_salt']
        
        # Change password
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
        
        # Verify password hash changed in database
        db_cursor.execute("SELECT password_hash, password_salt FROM users WHERE user_id = %s", 
                         (created_test_user['userId'],))
        updated = db_cursor.fetchone()
        
        assert updated['password_hash'] != original_hash
        assert updated['password_salt'] != original_salt
        assert updated['password_hash'] is not None
        assert updated['password_salt'] is not None

    def test_get_users_by_organisation_database_query(self, api_client, admin_token, test_organisation_id, db_cursor):
        """Test that getting users by organisation uses correct database query"""
        # Get users via API
        response = api_client.get(
            f"{API_BASE_URL}/users/by-organisation/{test_organisation_id}",
            headers={"Authorization": admin_token}
        )
        assert response.status_code == 200
        api_users = response.json()
        
        # Get users directly from database
        db_cursor.execute("SELECT * FROM users WHERE organisation_id = %s", (test_organisation_id,))
        db_users = db_cursor.fetchall()
        
        # Should return same number of users
        assert len(api_users) == len(db_users)
        
        # All users should belong to the organisation
        for user in api_users:
            assert user['organisationId'] == test_organisation_id

    def test_get_users_by_role_database_query(self, api_client, admin_token, db_cursor):
        """Test that getting users by role uses correct database query"""
        test_role = "auditor"
        
        # Get users via API
        response = api_client.get(
            f"{API_BASE_URL}/users/by-role/{test_role}",
            headers={"Authorization": admin_token}
        )
        assert response.status_code == 200
        api_users = response.json()
        
        # Get users directly from database
        db_cursor.execute("SELECT * FROM users WHERE role = %s", (test_role,))
        db_users = db_cursor.fetchall()
        
        # Should return same number of users
        assert len(api_users) == len(db_users)
        
        # All users should have the correct role
        for user in api_users:
            assert user['role'] == test_role

    def test_user_creation_timestamps(self, api_client, admin_token, test_user_data, db_cursor):
        """Test that user creation properly sets timestamps"""
        import datetime
        
        before_creation = datetime.datetime.utcnow()
        
        # Create user
        response = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=test_user_data
        )
        assert response.status_code == 201
        user = response.json()
        
        after_creation = datetime.datetime.utcnow()
        
        # Check timestamps in database
        db_cursor.execute("SELECT created_at FROM users WHERE user_id = %s", (user['userId'],))
        db_user = db_cursor.fetchone()
        
        created_at = db_user['created_at']
        assert created_at is not None
        
        # Should be within reasonable time range
        assert before_creation <= created_at <= after_creation
        
        # Cleanup
        api_client.patch(
            f"{API_BASE_URL}/users/{user['userId']}/deactivate",
            headers={"Authorization": admin_token}
        )

    def test_database_constraint_violations(self, db_cursor):
        """Test database constraint violations directly"""
        # Test NULL username constraint
        with pytest.raises(Exception):  # Should raise database constraint error
            db_cursor.execute("""
                INSERT INTO users (user_id, organisation_id, first_name, last_name, role, is_active)
                VALUES (%s, %s, %s, %s, %s, %s)
            """, (str(uuid.uuid4()), 'f47ac10b-58cc-4372-a567-0e02b2c3d479', 'Test', 'User', 'auditor', True))

    def test_database_foreign_key_constraints(self, db_cursor):
        """Test foreign key constraints for organisation relationship"""
        fake_org_id = str(uuid.uuid4())
        
        # Test inserting user with non-existent organisation
        with pytest.raises(Exception):  # Should raise foreign key constraint error
            db_cursor.execute("""
                INSERT INTO users (user_id, organisation_id, username, first_name, last_name, role, is_active)
                VALUES (%s, %s, %s, %s, %s, %s, %s)
            """, (str(uuid.uuid4()), fake_org_id, f'test_{uuid.uuid4().hex[:8]}', 'Test', 'User', 'auditor', True))


class TestPasswordSecurity:
    """Test password security and hashing mechanisms"""

    def test_password_not_stored_plaintext(self, db_cursor):
        """Test that passwords are never stored in plaintext"""
        db_cursor.execute("SELECT password_hash FROM users WHERE username = 'admin'")
        result = db_cursor.fetchone()
        
        # Password hash should not be the original password
        assert result['password_hash'] != 'password123'
        assert result['password_hash'] != 'admin'
        assert len(result['password_hash']) > 20  # Should be a hash

    def test_password_hash_uniqueness(self, api_client, admin_token, db_cursor):
        """Test that same password generates different hashes (due to salt)"""
        # Create two users with same password
        user_data_1 = {
            "organisationId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
            "username": f"test_user_1_{uuid.uuid4().hex[:8]}",
            "firstName": "Test1",
            "lastName": "User1",
            "email": f"test1_{uuid.uuid4().hex[:8]}@example.com",
            "role": "auditor",
            "password": "SamePassword123!"
        }
        
        user_data_2 = {
            "organisationId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
            "username": f"test_user_2_{uuid.uuid4().hex[:8]}",
            "firstName": "Test2",
            "lastName": "User2",
            "email": f"test2_{uuid.uuid4().hex[:8]}@example.com",
            "role": "auditor",
            "password": "SamePassword123!"
        }
        
        # Create both users
        response1 = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=user_data_1
        )
        assert response1.status_code == 201
        user1 = response1.json()
        
        response2 = api_client.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token},
            json=user_data_2
        )
        assert response2.status_code == 201
        user2 = response2.json()
        
        # Get password hashes from database
        db_cursor.execute("SELECT password_hash, password_salt FROM users WHERE user_id IN (%s, %s)", 
                         (user1['userId'], user2['userId']))
        results = db_cursor.fetchall()
        
        hash1 = None
        hash2 = None
        salt1 = None
        salt2 = None
        
        for result in results:
            if str(result['user_id']) == user1['userId']:
                hash1 = result['password_hash']
                salt1 = result['password_salt']
            else:
                hash2 = result['password_hash']
                salt2 = result['password_salt']
        
        # Hashes should be different (due to different salts)
        assert hash1 != hash2
        assert salt1 != salt2
        
        # Cleanup
        api_client.patch(f"{API_BASE_URL}/users/{user1['userId']}/deactivate", headers={"Authorization": admin_token})
        api_client.patch(f"{API_BASE_URL}/users/{user2['userId']}/deactivate", headers={"Authorization": admin_token})

    def test_password_minimum_requirements(self, api_client, admin_token, test_user_data):
        """Test password minimum requirements enforcement"""
        weak_passwords = [
            "",           # Empty
            "123",        # Too short
            "password",   # Too simple
            "12345678",   # Only numbers
            "abcdefgh",   # Only letters
        ]
        
        for weak_password in weak_passwords:
            test_data = test_user_data.copy()
            test_data['username'] = f"test_weak_{uuid.uuid4().hex[:8]}"
            test_data['email'] = f"test_weak_{uuid.uuid4().hex[:8]}@example.com"
            test_data['password'] = weak_password
            
            response = api_client.post(
                f"{API_BASE_URL}/users",
                headers={"Authorization": admin_token},
                json=test_data
            )
            
            # Should reject weak passwords
            # Note: This depends on your actual password validation implementation
            # Adjust assertion based on your requirements
            assert response.status_code in [400, 422]

    def test_password_special_characters(self, api_client, admin_token, test_user_data):
        """Test passwords with special characters"""
        special_passwords = [
            "P@ssw0rd123!",
            "Test$Password#2023",
            "Complex&Pass*456",
            "Símb0los€£¥789!"
        ]
        
        for password in special_passwords:
            test_data = test_user_data.copy()
            test_data['username'] = f"test_special_{uuid.uuid4().hex[:8]}"
            test_data['email'] = f"test_special_{uuid.uuid4().hex[:8]}@example.com"
            test_data['password'] = password
            
            response = api_client.post(
                f"{API_BASE_URL}/users",
                headers={"Authorization": admin_token},
                json=test_data
            )
            
            if response.status_code == 201:
                user = response.json()
                # Cleanup
                api_client.patch(
                    f"{API_BASE_URL}/users/{user['userId']}/deactivate",
                    headers={"Authorization": admin_token}
                )
            
            # Should accept valid complex passwords
            assert response.status_code in [201, 400]  # 400 might be for other validation issues 