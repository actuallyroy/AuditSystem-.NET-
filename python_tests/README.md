# UserService & UsersController Comprehensive Test Suite

This directory contains comprehensive Python test cases for thoroughly testing the UserService and UsersController components of the Retail Execution Audit System.

## 📋 Test Coverage

### 🎯 **UserService Testing**
- **Authentication**: Password hashing, verification, security
- **User CRUD Operations**: Create, read, update, deactivate users
- **Data Validation**: Input validation, constraints, edge cases
- **Database Integration**: Direct database operations, transactions
- **Business Logic**: Role-based operations, organization relationships

### 🌐 **UsersController API Testing**
- **All Endpoints**: Complete coverage of all API endpoints
- **HTTP Methods**: GET, POST, PUT, PATCH operations
- **Authentication & Authorization**: JWT token validation, role-based access
- **Request/Response Validation**: Proper data structures, error handling
- **Status Codes**: Correct HTTP status codes for all scenarios

### 🔐 **Security Testing**
- **JWT Token Validation**: Valid/invalid tokens, expiration, claims
- **Role-Based Access Control**: Admin, Manager, Supervisor, Auditor permissions
- **Password Security**: Hashing, salting, complexity requirements
- **Input Sanitization**: SQL injection, XSS prevention

## 📁 Test Files

| File | Description | Test Count |
|------|-------------|------------|
| `test_users_controller_api.py` | API endpoint tests | 25+ tests |
| `test_authentication_flows.py` | Auth & authorization tests | 20+ tests |
| `test_user_service_database.py` | Database & business logic tests | 15+ tests |
| `conftest.py` | Test configuration & fixtures | N/A |
| `quick_test.py` | Quick smoke tests | 6 tests |
| `run_tests.py` | Test runner with reporting | N/A |

## 🚀 Quick Start

### 1. Prerequisites
- Docker containers running (`docker-compose up -d`)
- API accessible at http://localhost:8080
- PostgreSQL database with seed data

### 2. Quick Validation
```bash
cd python_tests
python quick_test.py
```

### 3. Comprehensive Test Suite
```bash
cd python_tests  
python run_tests.py
```

## 📊 Test Categories

### **API Endpoint Tests** (`test_users_controller_api.py`)

#### GET Endpoints
- ✅ `GET /api/users` - Get all users (admin/manager only)
- ✅ `GET /api/users/{id}` - Get user by ID
- ✅ `GET /api/users/by-username/{username}` - Get user by username
- ✅ `GET /api/users/by-organisation/{orgId}` - Get users by organization
- ✅ `GET /api/users/by-role/{role}` - Get users by role

#### POST Endpoints
- ✅ `POST /api/users` - Create new user (admin only)

#### PUT Endpoints  
- ✅ `PUT /api/users/{id}` - Update user (admin/manager)

#### PATCH Endpoints
- ✅ `PATCH /api/users/{id}/deactivate` - Deactivate user (admin only)
- ✅ `PATCH /api/users/{id}/change-password` - Change password

### **Authentication Tests** (`test_authentication_flows.py`)

#### JWT Token Validation
- ✅ Valid tokens (admin, manager, auditor)
- ✅ Invalid signatures, expired tokens
- ✅ Missing/malformed authorization headers
- ✅ Invalid issuer/audience claims
- ✅ Token edge cases (future iat, long tokens)

#### Role-Based Access Control
- ✅ Admin permissions (full access)
- ✅ Manager permissions (limited admin access)
- ✅ Auditor permissions (restricted access)
- ✅ Unauthorized access rejection

### **Database Tests** (`test_user_service_database.py`)

#### Data Integrity
- ✅ Seed data verification
- ✅ Password hashing validation
- ✅ Unique constraints (username, email)
- ✅ Foreign key relationships

#### CRUD Operations
- ✅ User creation with proper data storage
- ✅ User updates reflected in database
- ✅ User deactivation status changes
- ✅ Password changes update hashes

#### Security
- ✅ Passwords never stored in plaintext
- ✅ Salt uniqueness for same passwords
- ✅ Password complexity requirements

## 🛠️ Test Configuration

### Database Configuration
```python
DB_CONFIG = {
    "host": "localhost",
    "port": 5432,
    "database": "retail-execution-audit-system", 
    "user": "postgres",
    "password": "123456"
}
```

### API Configuration
```python
API_BASE_URL = "http://localhost:8080/api"
```

### JWT Configuration
```python
JWT_SECRET = "YourProductionSecretKeyHereMakeSureItIsAtLeast32CharactersLongAndSecure"
JWT_ISSUER = "AuditSystem"
JWT_AUDIENCE = "AuditSystemClients"
```

## 📋 Test Data

### Seed Users (from database)
- **admin** (role: admin) - Full system access
- **manager1** (role: manager) - Management access  
- **auditor1** (role: auditor) - Limited access

### Test Organization
- **ID**: `f47ac10b-58cc-4372-a567-0e02b2c3d479`
- **Name**: ACME Retail Corp

## 🧪 Test Scenarios

### Positive Test Cases
- ✅ Valid user creation with all required fields
- ✅ Successful authentication with correct credentials
- ✅ Proper role-based access control
- ✅ User updates with valid data
- ✅ Password changes with correct current password

### Negative Test Cases  
- ❌ Duplicate username/email rejection
- ❌ Invalid JWT token rejection
- ❌ Unauthorized access attempts
- ❌ Invalid data format handling
- ❌ Non-existent user operations

### Edge Cases
- 🔍 Extremely long usernames/data
- 🔍 Special characters in names
- 🔍 Unicode character handling
- 🔍 Concurrent authentication requests
- 🔍 Malformed JSON requests

## 📈 Test Reports

After running tests, the following reports are generated:

### HTML Report
- **Location**: `test_reports/report.html`
- **Content**: Detailed test results with pass/fail status
- **Features**: Interactive, filterable results

### Coverage Report
- **Location**: `htmlcov/index.html`
- **Content**: Code coverage analysis
- **Metrics**: Line coverage, branch coverage

### JUnit XML
- **Location**: `test_reports/junit.xml`
- **Purpose**: CI/CD integration
- **Format**: Standard JUnit XML format

## 🔧 Troubleshooting

### Common Issues

#### API Not Accessible
```
❌ API is not accessible: Connection refused
```
**Solution**: Ensure Docker containers are running
```bash
docker-compose up -d
docker-compose ps
```

#### Database Connection Failed
```
❌ Database connection failed: Connection refused
```
**Solution**: Check PostgreSQL container status
```bash
docker-compose logs postgres
```

#### JWT Token Invalid
```
❌ Invalid token properly rejected: 200
```
**Solution**: Verify JWT secret matches API configuration

#### Test Dependencies Missing
```
ModuleNotFoundError: No module named 'pytest'
```
**Solution**: Install requirements
```bash
pip install -r requirements.txt
```

### Environment Variables

If needed, you can override configuration:

```bash
# API URL
export API_BASE_URL="http://localhost:8080/api"

# Database connection
export DB_HOST="localhost"
export DB_PORT="5432"
export DB_NAME="retail-execution-audit-system"
export DB_USER="postgres"
export DB_PASS="123456"
```

## 📚 Dependencies

### Required Python Packages
- `pytest` - Test framework
- `requests` - HTTP client
- `psycopg2-binary` - PostgreSQL adapter
- `pyjwt` - JWT token handling
- `faker` - Test data generation
- `pytest-html` - HTML reporting
- `pytest-cov` - Coverage reporting

### Installation
```bash
pip install -r requirements.txt
```

## 🎯 Test Execution Modes

### 1. Quick Smoke Tests
```bash
python quick_test.py
```
- **Duration**: ~30 seconds
- **Purpose**: Basic functionality validation
- **Tests**: 6 core scenarios

### 2. Individual Test Suites
```bash
pytest test_users_controller_api.py -v
pytest test_authentication_flows.py -v  
pytest test_user_service_database.py -v
```
- **Duration**: ~2-3 minutes each
- **Purpose**: Focused testing per component

### 3. Complete Test Suite
```bash
python run_tests.py
```
- **Duration**: ~5-10 minutes
- **Purpose**: Comprehensive validation
- **Reports**: HTML, coverage, JUnit XML

### 4. Continuous Integration
```bash
pytest --junitxml=junit.xml --cov=. --cov-report=xml
```
- **Purpose**: CI/CD pipeline integration
- **Output**: Machine-readable reports

## 🏆 Success Criteria

### All Tests Pass
- ✅ 60+ individual test cases pass
- ✅ All HTTP status codes correct
- ✅ All security measures validated
- ✅ All CRUD operations functional

### Performance Benchmarks
- ⚡ API response time < 500ms
- ⚡ Database queries optimized
- ⚡ No memory leaks detected

### Security Validation
- 🔒 No plaintext passwords stored
- 🔒 JWT tokens properly validated
- 🔒 Role-based access enforced
- 🔒 Input sanitization working

## 📞 Support

If you encounter issues:

1. **Check Prerequisites**: Ensure all containers are running
2. **Review Logs**: Check API and database logs
3. **Validate Configuration**: Verify connection strings and secrets
4. **Run Quick Test**: Use `quick_test.py` for basic validation
5. **Check Documentation**: Review API documentation and test files 