# Retail Execution Audit System - API Test Suite

This directory contains a comprehensive test suite for the Retail Execution Audit System API. The tests cover all endpoints, error handling, caching functionality, and edge cases.

## Test Structure

### Test Files

1. **`test_api_comprehensive.py`** - Main comprehensive test suite
   - Tests all CRUD operations for all entities
   - Covers authentication, authorization, and business logic
   - Tests data validation and response formats
   - Includes end-to-end workflow testing

2. **`test_cache_endpoints.py`** - Cache functionality tests
   - Tests Redis cache health and statistics
   - Measures cache performance improvements
   - Tests cache management operations (clear, warmup)
   - Validates cache invalidation patterns

3. **`test_error_handling.py`** - Error handling and edge cases
   - Tests authentication and authorization errors
   - Validates input validation and error responses
   - Tests 404 scenarios and malformed requests
   - Checks rate limiting and security measures

4. **`test_missing_endpoints.py`** - Missing endpoints test suite
   - Tests endpoints not covered in main test suite
   - Covers template versioning, organisation invitations
   - Tests assignment unassign functionality
   - Validates user-organisation relationships

5. **`run_all_tests.py`** - Master test runner
   - Executes all test suites in the correct order
   - Generates comprehensive reports
   - Provides detailed success/failure analysis

### Test Credentials

The tests use credentials defined in `tools/test_credentials.json`:

```json
{
  "manager": {
    "username": "amitkumar93525@gmail.com",
    "password": "idDcXT.as5tAK2g"
  },
  "auditor": {
    "username": "johndoe",
    "password": "password123"
  }
}
```

## Running the Tests

### Prerequisites

1. **API Server Running**: Ensure the .NET API server is running on port 8080 (Docker default)
2. **Database**: PostgreSQL database should be accessible
3. **Redis**: Redis cache server should be running
4. **Python Dependencies**: Only standard library modules are used

### Basic Usage

#### Run All Tests
```bash
python run_all_tests.py
```

#### Run with Custom API URL
```bash
python run_all_tests.py --base-url http://localhost:8080
```

#### Run Specific Test Suite
```bash
# Run only comprehensive tests
python run_all_tests.py --test comprehensive

# Run only cache tests
python run_all_tests.py --test cache

# Run only error handling tests
python run_all_tests.py --test errors

# Run only missing endpoints tests
python run_all_tests.py --test missing
```

#### Run Individual Test Files
```bash
# Run comprehensive tests directly
python test_api_comprehensive.py

# Run cache tests directly
python test_cache_endpoints.py

# Run error handling tests directly
python test_error_handling.py

# Run missing endpoints tests directly
python test_missing_endpoints.py
```

## Test Coverage

### Authentication & Authorization
- ✅ User registration and login
- ✅ JWT token validation
- ✅ Role-based access control
- ✅ Protected endpoint access
- ✅ Invalid credential handling

### User Management
- ✅ Create, read, update, delete users
- ✅ User search by username, organisation, role
- ✅ Password change functionality
- ✅ User deactivation
- ✅ Input validation

### Organisation Management
- ✅ Create, read, update, delete organisations
- ✅ User invitation system
- ✅ Organisation joining
- ✅ Multi-tenant data isolation

### Template Management
- ✅ Create, read, update, delete templates
- ✅ Template publishing and versioning
- ✅ Template categorization and search
- ✅ Template assignment tracking
- ✅ JSONB question and scoring validation

### Assignment Management
- ✅ Create, read, update, delete assignments
- ✅ Template assignment to auditors
- ✅ Assignment status management
- ✅ Due date and priority handling
- ✅ Organisation and auditor filtering

### Audit Execution
- ✅ Create, read, update, delete audits
- ✅ Audit submission workflow
- ✅ Status management and approval
- ✅ Media and location handling
- ✅ Scoring and critical issues tracking

### Cache Functionality
- ✅ Redis health monitoring
- ✅ Cache statistics and performance
- ✅ Cache invalidation patterns
- ✅ Cache warmup operations
- ✅ Response time improvements

### Error Handling
- ✅ Authentication errors (401, 403)
- ✅ Validation errors (400, 422)
- ✅ Not found errors (404)
- ✅ Rate limiting (429)
- ✅ Malformed request handling

## Test Results

### Success Indicators
- ✅ All HTTP status codes are correct
- ✅ Response data matches expected schemas
- ✅ Authentication and authorization work properly
- ✅ Cache performance shows improvements
- ✅ Error responses are properly formatted

### Common Issues
- ❌ API server not running
- ❌ Database connection issues
- ❌ Redis connection problems
- ❌ Invalid test credentials
- ❌ Network connectivity issues

## Report Generation

The test runner generates detailed reports including:

1. **Summary Statistics**
   - Total test suites executed
   - Pass/fail counts
   - Success rate percentage
   - Total execution time

2. **Detailed Results**
   - Individual test suite results
   - Execution times
   - Error messages and stack traces

3. **Recommendations**
   - Action items for failed tests
   - Common troubleshooting steps
   - Performance optimization suggestions

4. **JSON Report File**
   - Machine-readable detailed report
   - Timestamped for tracking
   - Includes all test data and results

## Troubleshooting

### Common Problems

1. **Authentication Failures**
   - Verify credentials in `tools/test_credentials.json`
   - Check if users exist in the database
   - Ensure password hashing is working

2. **Connection Errors**
   - Verify API server is running
   - Check base URL configuration
   - Test network connectivity

3. **Database Errors**
   - Ensure PostgreSQL is running
   - Check connection string
   - Verify database schema is up to date

4. **Cache Errors**
   - Verify Redis is running
   - Check Redis connection configuration
   - Ensure cache keys are properly formatted

### Debug Mode

For detailed debugging, run individual test files with verbose output:

```bash
python test_api_comprehensive.py --verbose
```

## Performance Testing

The cache tests include performance measurements:

- **First Request**: Measures uncached response time
- **Second Request**: Measures cached response time
- **Improvement Calculation**: Shows percentage improvement
- **Cache Hit Validation**: Confirms caching is working

## Security Testing

The error handling tests include security validations:

- **Authentication Bypass**: Tests protected endpoints without auth
- **Invalid Tokens**: Tests with malformed JWT tokens
- **Authorization**: Tests role-based access control
- **Input Validation**: Tests malicious input handling

## Continuous Integration

These tests can be integrated into CI/CD pipelines:

```yaml
# Example GitHub Actions workflow
- name: Run API Tests
  run: |
    cd python_tests
    python run_all_tests.py --base-url ${{ secrets.API_URL }}
```

## Contributing

When adding new tests:

1. Follow the existing test structure
2. Use proper authentication and authorization
3. Include both positive and negative test cases
4. Add appropriate error handling
5. Update this README with new test coverage

## Support

For issues with the test suite:

1. Check the troubleshooting section
2. Review the generated test reports
3. Verify all prerequisites are met
4. Check the API server logs for errors 