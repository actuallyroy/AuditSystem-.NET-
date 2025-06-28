# Retail Execution Audit System - API Testing Instructions

This document provides instructions on how to test the Retail Execution Audit System API.

## Prerequisites

Before running the tests, make sure you have:

1. PostgreSQL database running with the schema created (see `tables.sql`)
2. API running at http://localhost:5049
3. Python 3.6+ installed with the following packages:
   - requests
   - psycopg2-binary

## Install Required Packages

```bash
pip install requests psycopg2-binary
```

## Available Test Scripts

We provide several test scripts with different levels of complexity:

### 1. Simple Test Script

This script performs a basic test of the user registration endpoint:

```bash
python simple_test.py
```

This script:
- Creates an organisation in the database
- Registers a new user via the API
- Displays the API response

### 2. Full API Test Suite

This script performs a comprehensive test of all main API endpoints:

```bash
python full_api_test.py
```

This script tests:
- Organisation creation in the database
- User registration
- User login
- Getting user details
- Updating user (with permission handling)
- Changing password
- Login with new password

## Database Configuration

Both test scripts use the following database configuration:

```python
DB_CONFIG = {
    "host": "localhost",
    "port": 5432,
    "database": "retail-execution-audit-system",
    "user": "postgres",
    "password": "123456"
}
```

If your database configuration is different, please update these values in the test scripts.

## API Configuration

The tests assume the API is running at:

```
http://localhost:5049/api/v1
```

If your API is running at a different URL, please update the `API_BASE_URL` variable in the test scripts.

## Troubleshooting

### Connection Issues

If you see connection errors, make sure:
- The API is running at the expected URL
- The database is running and accessible with the provided credentials
- No firewall is blocking the connections

### Database Constraint Errors

If you see database constraint errors:
- Make sure the database schema is correctly set up (see `tables.sql`)
- Check that the required tables and constraints are created
- Verify that the test scripts are creating the required records in the correct order

### Authentication Issues

If you see authentication errors:
- Make sure the JWT token configuration in the API matches what the client expects
- Check that the user roles are set up correctly in the database
- Verify that the API is correctly validating tokens

## Additional Test Files

For testing with other tools, we also provide:

- `AuditSystem.postman_collection.json` - Postman collection for API testing
- `api-test.html` - Simple HTML page for testing the API in a browser
- `test-api.js` - Node.js script for testing the API
- `test_api.py` - Another Python script for testing the API 