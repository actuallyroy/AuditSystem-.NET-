import pytest
import requests
import json
import time
import jwt
import hashlib
import base64
from datetime import datetime, timedelta
from faker import Faker
import uuid

# Try to import psycopg2, make it optional
try:
    import psycopg2
    from psycopg2.extras import RealDictCursor
    psycopg2_available = True
except ImportError:
    psycopg2_available = False

# Test Configuration
API_BASE_URL = "http://localhost:8080/api/v1"
DB_CONFIG = {
    "host": "localhost",
    "port": 5432,
    "database": "retail-execution-audit-system",
    "user": "postgres",
    "password": "123456"
}

JWT_SECRET = "YourDevelopmentSecretKeyHereMakeSureItIsAtLeast32CharactersLong"
JWT_ISSUER = "AuditSystem"
JWT_AUDIENCE = "AuditSystemClients"

fake = Faker()

@pytest.fixture(scope="session")
def api_client():
    """HTTP client for API testing"""
    session = requests.Session()
    session.headers.update({
        'Content-Type': 'application/json',
        'Accept': 'application/json'
    })
    return session

@pytest.fixture(scope="session")
def db_connection():
    """Database connection for direct DB operations"""
    if psycopg2_available:
        conn = psycopg2.connect(**DB_CONFIG)
        yield conn
        conn.close()
    else:
        pytest.skip("psycopg2 is not available")

@pytest.fixture
def db_cursor(db_connection):
    """Database cursor with auto-commit"""
    with db_connection.cursor(cursor_factory=RealDictCursor) as cursor:
        yield cursor
        db_connection.commit()

@pytest.fixture
def admin_token():
    """Generate JWT token for admin user"""
    payload = {
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': '550e8400-e29b-41d4-a716-446655440000',  # admin user from seed data
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'admin',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname': 'System',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname': 'Administrator',
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'admin',
        'iss': JWT_ISSUER,
        'aud': JWT_AUDIENCE,
        'iat': datetime.utcnow(),
        'exp': datetime.utcnow() + timedelta(hours=1)
    }
    token = jwt.encode(payload, JWT_SECRET, algorithm='HS256')
    return f"Bearer {token}"

@pytest.fixture
def manager_token():
    """Generate JWT token for manager user"""
    payload = {
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': '550e8400-e29b-41d4-a716-446655440001',  # manager user from seed data
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'manager1',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname': 'John',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname': 'Manager',
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'manager',
        'iss': JWT_ISSUER,
        'aud': JWT_AUDIENCE,
        'iat': datetime.utcnow(),
        'exp': datetime.utcnow() + timedelta(hours=1)
    }
    token = jwt.encode(payload, JWT_SECRET, algorithm='HS256')
    return f"Bearer {token}"

@pytest.fixture
def auditor_token():
    """Generate JWT token for auditor user"""
    payload = {
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': '550e8400-e29b-41d4-a716-446655440003',  # auditor user from seed data
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'auditor1',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname': 'Mike',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname': 'Auditor',
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'auditor',
        'iss': JWT_ISSUER,
        'aud': JWT_AUDIENCE,
        'iat': datetime.utcnow(),
        'exp': datetime.utcnow() + timedelta(hours=1)
    }
    token = jwt.encode(payload, JWT_SECRET, algorithm='HS256')
    return f"Bearer {token}"

@pytest.fixture
def test_organisation_id():
    """Returns the test organisation ID from seed data"""
    return "f47ac10b-58cc-4372-a567-0e02b2c3d479"

@pytest.fixture
def test_user_data():
    """Generate test user data"""
    return {
        "organisationId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
        "username": f"test_user_{uuid.uuid4().hex[:8]}",
        "firstName": fake.first_name(),
        "lastName": fake.last_name(),
        "email": fake.email(),
        "phone": fake.phone_number(),
        "role": "auditor",
        "password": "TestPassword123!"
    }

@pytest.fixture
def created_test_user(api_client, admin_token, test_user_data):
    """Create a test user and return its data"""
    response = api_client.post(
        f"{API_BASE_URL}/users",
        headers={"Authorization": admin_token},
        json=test_user_data
    )
    if response.status_code == 201:
        user = response.json()
        # Store password for later use
        user['password'] = test_user_data['password']
        yield user
        # Cleanup: deactivate user after test
        api_client.patch(
            f"{API_BASE_URL}/users/{user['userId']}/deactivate",
            headers={"Authorization": admin_token}
        )
    else:
        pytest.fail(f"Failed to create test user: {response.text}")

@pytest.fixture
def seed_user_credentials():
    """Credentials for seed data users"""
    return {
        "admin": {
            "userId": "550e8400-e29b-41d4-a716-446655440000",
            "username": "admin",
            "password": "password123",  # This should match the seed data password
            "role": "admin"
        },
        "manager": {
            "userId": "550e8400-e29b-41d4-a716-446655440001", 
            "username": "manager1",
            "password": "password123",
            "role": "manager"
        },
        "auditor": {
            "userId": "550e8400-e29b-41d4-a716-446655440003",
            "username": "auditor1", 
            "password": "password123",
            "role": "auditor"
        }
    }

@pytest.fixture(scope="session", autouse=True)
def check_api_availability():
    """Ensure API is available before running tests"""
    try:
        response = requests.get(f"http://localhost:8080/health", timeout=5)
        if response.status_code != 200:
            pytest.fail("API is not responding correctly")
    except requests.exceptions.RequestException:
        pytest.fail("API is not accessible at http://localhost:8080")

def cleanup_test_users(db_cursor, pattern="test_user_%"):
    """Helper function to cleanup test users"""
    db_cursor.execute(
        "DELETE FROM users WHERE username LIKE %s AND username != 'admin'",
        (pattern,)
    )

@pytest.fixture(autouse=True)
def cleanup_after_test():
    """Cleanup test data after each test"""
    yield
    # Only try to cleanup if psycopg2 is available
    if psycopg2_available:
        try:
            conn = psycopg2.connect(**DB_CONFIG)
            with conn.cursor(cursor_factory=RealDictCursor) as cursor:
                cleanup_test_users(cursor)
                conn.commit()
            conn.close()
        except Exception:
            # Ignore cleanup errors for API-only tests
            pass 