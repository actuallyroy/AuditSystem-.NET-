# Retail Execution Audit System

This repository contains a .NET Core API for the Retail Execution Audit System along with various test scripts.

## Project Structure

The solution follows a clean architecture approach with the following projects:
- **AuditSystem.API**: Web API endpoints and controllers
- **AuditSystem.Domain**: Core domain models, entities, and interfaces
- **AuditSystem.Infrastructure**: Data access and external services implementation
- **AuditSystem.Services**: Business logic and service implementations
- **AuditSystem.Common**: Shared utilities and helpers

## Prerequisites

1. .NET 9.0 SDK
2. PostgreSQL database
3. Python 3.6+ (for running test scripts)

## Database Setup

1. Create a PostgreSQL database named `retail-execution-audit-system`
2. Run the SQL script in `tables.sql` to create the schema

## Running the API

```bash
cd src/AuditSystem.API
dotnet run
```

The API will be available at http://localhost:5049.

## API Testing

We provide several test scripts to verify the API functionality:

### Simple Test Script

For a quick test of the user registration endpoint:

```bash
pip install requests psycopg2-binary
python simple_test.py
```

### Comprehensive Test Suite

For testing all main API endpoints:

```bash
python full_api_test.py
```

This script tests:
- Organisation creation
- User registration
- User login
- Getting user details
- Updating user information
- Changing password
- Login with new password

See `testing-instructions.md` for detailed testing instructions.

## Known Issues

### Foreign Key Constraint

Users must be associated with an existing organisation. The test scripts handle this by creating an organisation in the database before registering a user.

### Role Constraint

The `users` table has a constraint on the `role` field. Valid roles are:
- auditor
- manager
- supervisor
- admin

The API automatically assigns the 'auditor' role to new users during registration.

## Additional Test Files

- `AuditSystem.postman_collection.json`: Postman collection for API testing
- `api-test.html`: Simple HTML page for testing the API in a browser
- `test-api.js`: Node.js script for testing the API
- `test_api.py`: Another Python script for testing the API

## Troubleshooting

See `testing-instructions.md` for detailed troubleshooting information. 