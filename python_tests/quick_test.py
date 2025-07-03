#!/usr/bin/env python3
"""
Quick validation test for UserService and UsersController
Simple smoke tests to verify basic functionality
"""
import requests
import json
import uuid
import jwt
from datetime import datetime, timedelta

# Configuration
API_BASE_URL = "http://localhost:8080/api"
JWT_SECRET = "YourProductionSecretKeyHereMakeSureItIsAtLeast32CharactersLongAndSecure"
JWT_ISSUER = "AuditSystem"
JWT_AUDIENCE = "AuditSystemClients"

def generate_admin_token():
    """Generate JWT token for admin user"""
    payload = {
        'sub': '550e8400-e29b-41d4-a716-446655440000',
        'username': 'admin',
        'role': 'admin',
        'organisation_id': 'f47ac10b-58cc-4372-a567-0e02b2c3d479',
        'iss': JWT_ISSUER,
        'aud': JWT_AUDIENCE,
        'iat': datetime.utcnow(),
        'exp': datetime.utcnow() + timedelta(hours=1)
    }
    token = jwt.encode(payload, JWT_SECRET, algorithm='HS256')
    return f"Bearer {token}"

def test_api_health():
    """Test API health endpoint"""
    print("🏥 Testing API health...")
    try:
        response = requests.get("http://localhost:8080/health", timeout=5)
        if response.status_code == 200:
            print("✅ API health check passed")
            return True
        else:
            print(f"❌ API health check failed: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ API not accessible: {e}")
        return False

def test_get_users():
    """Test getting all users"""
    print("\n👥 Testing get all users...")
    try:
        token = generate_admin_token()
        response = requests.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": token}
        )
        
        if response.status_code == 200:
            users = response.json()
            print(f"✅ Got {len(users)} users")
            
            # Check if we have expected seed users
            usernames = [user.get('username') for user in users]
            expected_users = ['admin', 'manager1', 'auditor1']
            found_users = [u for u in expected_users if u in usernames]
            print(f"   Found seed users: {found_users}")
            
            return True
        else:
            print(f"❌ Failed to get users: {response.status_code} - {response.text}")
            return False
    except Exception as e:
        print(f"❌ Error getting users: {e}")
        return False

def test_create_and_delete_user():
    """Test creating and deleting a user"""
    print("\n👤 Testing create and delete user...")
    try:
        token = generate_admin_token()
        
        # Create test user
        user_data = {
            "organisationId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
            "username": f"quick_test_{uuid.uuid4().hex[:8]}",
            "firstName": "Quick",
            "lastName": "Test",
            "email": f"quicktest_{uuid.uuid4().hex[:8]}@example.com",
            "phone": "+1234567890",
            "role": "auditor",
            "password": "TestPassword123!"
        }
        
        # Create user
        response = requests.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": token, "Content-Type": "application/json"},
            json=user_data
        )
        
        if response.status_code == 201:
            user = response.json()
            user_id = user['userId']
            print(f"✅ User created successfully: {user['username']}")
            
            # Test getting the user
            get_response = requests.get(
                f"{API_BASE_URL}/users/{user_id}",
                headers={"Authorization": token}
            )
            
            if get_response.status_code == 200:
                print("✅ User retrieval successful")
            else:
                print("❌ User retrieval failed")
            
            # Deactivate user (cleanup)
            delete_response = requests.patch(
                f"{API_BASE_URL}/users/{user_id}/deactivate",
                headers={"Authorization": token}
            )
            
            if delete_response.status_code == 204:
                print("✅ User deactivation successful")
                return True
            else:
                print(f"❌ User deactivation failed: {delete_response.status_code}")
                return False
        else:
            print(f"❌ User creation failed: {response.status_code} - {response.text}")
            return False
            
    except Exception as e:
        print(f"❌ Error in create/delete test: {e}")
        return False

def test_authentication_and_authorization():
    """Test authentication and authorization"""
    print("\n🔐 Testing authentication and authorization...")
    try:
        # Test with valid admin token
        admin_token = generate_admin_token()
        response = requests.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": admin_token}
        )
        
        if response.status_code == 200:
            print("✅ Admin authentication successful")
        else:
            print(f"❌ Admin authentication failed: {response.status_code}")
            return False
        
        # Test without token
        response = requests.get(f"{API_BASE_URL}/users")
        if response.status_code == 401:
            print("✅ Unauthorized access properly rejected")
        else:
            print(f"❌ Unauthorized access not properly rejected: {response.status_code}")
            return False
        
        # Test with invalid token
        response = requests.get(
            f"{API_BASE_URL}/users",
            headers={"Authorization": "Bearer invalid_token"}
        )
        if response.status_code == 401:
            print("✅ Invalid token properly rejected")
            return True
        else:
            print(f"❌ Invalid token not properly rejected: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"❌ Error in auth test: {e}")
        return False

def test_user_by_organisation():
    """Test getting users by organisation"""
    print("\n🏢 Testing get users by organisation...")
    try:
        token = generate_admin_token()
        org_id = "f47ac10b-58cc-4372-a567-0e02b2c3d479"
        
        response = requests.get(
            f"{API_BASE_URL}/users/by-organisation/{org_id}",
            headers={"Authorization": token}
        )
        
        if response.status_code == 200:
            users = response.json()
            print(f"✅ Got {len(users)} users for organisation")
            
            # Verify all users belong to the organisation
            for user in users:
                if user.get('organisationId') != org_id:
                    print(f"❌ User {user.get('username')} doesn't belong to expected org")
                    return False
            
            print("✅ All users belong to correct organisation")
            return True
        else:
            print(f"❌ Failed to get users by organisation: {response.status_code}")
            return False
            
    except Exception as e:
        print(f"❌ Error in organisation test: {e}")
        return False

def test_change_password():
    """Test password change functionality"""
    print("\n🔑 Testing password change...")
    try:
        token = generate_admin_token()
        
        # First create a test user
        user_data = {
            "organisationId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
            "username": f"pwd_test_{uuid.uuid4().hex[:8]}",
            "firstName": "Password",
            "lastName": "Test",
            "email": f"pwdtest_{uuid.uuid4().hex[:8]}@example.com",
            "role": "auditor",
            "password": "OriginalPassword123!"
        }
        
        create_response = requests.post(
            f"{API_BASE_URL}/users",
            headers={"Authorization": token, "Content-Type": "application/json"},
            json=user_data
        )
        
        if create_response.status_code == 201:
            user = create_response.json()
            user_id = user['userId']
            
            # Change password
            password_change_data = {
                "currentPassword": "OriginalPassword123!",
                "newPassword": "NewPassword456!"
            }
            
            pwd_response = requests.patch(
                f"{API_BASE_URL}/users/{user_id}/change-password",
                headers={"Authorization": token, "Content-Type": "application/json"},
                json=password_change_data
            )
            
            if pwd_response.status_code == 204:
                print("✅ Password change successful")
                
                # Cleanup
                requests.patch(
                    f"{API_BASE_URL}/users/{user_id}/deactivate",
                    headers={"Authorization": token}
                )
                return True
            else:
                print(f"❌ Password change failed: {pwd_response.status_code} - {pwd_response.text}")
                # Cleanup
                requests.patch(
                    f"{API_BASE_URL}/users/{user_id}/deactivate",
                    headers={"Authorization": token}
                )
                return False
        else:
            print(f"❌ Failed to create test user for password test: {create_response.status_code}")
            return False
            
    except Exception as e:
        print(f"❌ Error in password change test: {e}")
        return False

def main():
    """Run quick validation tests"""
    print("🚀 Quick UserService & UsersController Validation")
    print("="*60)
    
    tests = [
        ("API Health", test_api_health),
        ("Get Users", test_get_users),
        ("Create & Delete User", test_create_and_delete_user),
        ("Authentication", test_authentication_and_authorization),
        ("Users by Organisation", test_user_by_organisation),
        ("Change Password", test_change_password)
    ]
    
    results = []
    
    for test_name, test_func in tests:
        try:
            result = test_func()
            results.append((test_name, result))
        except Exception as e:
            print(f"❌ {test_name} test crashed: {e}")
            results.append((test_name, False))
    
    # Summary
    print("\n" + "="*60)
    print("QUICK TEST SUMMARY")
    print("="*60)
    
    passed = 0
    total = len(results)
    
    for test_name, result in results:
        status = "PASSED" if result else "FAILED"
        icon = "✅" if result else "❌"
        print(f"{icon} {test_name:<25} {status}")
        if result:
            passed += 1
    
    print(f"\nResult: {passed}/{total} tests passed")
    
    if passed == total:
        print("🎉 All quick tests passed! Your API is working correctly.")
        print("\nYou can now run the comprehensive test suite:")
        print("   python run_tests.py")
        return True
    else:
        print("💥 Some quick tests failed. Check your API configuration.")
        return False

if __name__ == "__main__":
    success = main()
    exit(0 if success else 1) 