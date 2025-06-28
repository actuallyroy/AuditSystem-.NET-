# Retail Execution Audit System API Documentation

This document outlines the available API endpoints for the Retail Execution Audit System, with a focus on the authentication flow for frontend integration.

## Base URL

```
http://localhost:5049/api/v1
```

## Authentication

The API uses JWT (JSON Web Token) for authentication. After successful login or registration, you'll receive a token that should be included in subsequent requests.

### Including the Token in Requests

For authenticated endpoints, include the token in the Authorization header:

```
Authorization: Bearer <your_jwt_token>
```

## Authentication Endpoints

### Register a New User

**Endpoint:** `POST /Auth/register`

**Authentication:** None (Public)

**Request Body:**
```json
{
  "organisationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Optional UUID
  "username": "johndoe",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phone": "1234567890",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "userId": "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f",
  "username": "johndoe",
  "firstName": "John",
  "lastName": "Doe",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "role": "auditor"
}
```

**Notes:**
- New users are automatically assigned the "auditor" role
- The `organisationId` field is optional. If not provided, a default organisation named "{FirstName}'s Organisation" will be created automatically
- Username must be unique

**Possible Errors:**
- 400 Bad Request: Username already exists
- 400 Bad Request: Invalid organisation ID
- 500 Internal Server Error: Unexpected error during registration

### User Login

**Endpoint:** `POST /Auth/login`

**Authentication:** None (Public)

**Request Body:**
```json
{
  "username": "johndoe",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "userId": "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f",
  "username": "johndoe",
  "firstName": "John",
  "lastName": "Doe",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "role": "auditor"
}
```

**Possible Errors:**
- 401 Unauthorized: Invalid username or password
- 401 Unauthorized: User account is deactivated
- 400 Bad Request: Database error with user organisation
- 500 Internal Server Error: Unexpected error during login

## User Management Endpoints

### Get Current User Details

**Endpoint:** `GET /Users/{id}`

**Authentication:** Required (Any authenticated user can access their own details)

**URL Parameters:**
- `id`: The UUID of the user

**Response (200 OK):**
```json
{
  "userId": "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f",
  "organisationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", // May be null
  "username": "johndoe",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phone": "1234567890",
  "role": "auditor",
  "isActive": true,
  "createdAt": "2025-06-28T14:25:00Z",
  "updatedAt": "2025-06-28T14:25:00Z"
}
```

**Possible Errors:**
- 401 Unauthorized: Not authenticated
- 404 Not Found: User not found
- 500 Internal Server Error

### Change User Password

**Endpoint:** `PATCH /Users/{id}/change-password`

**Authentication:** Required (Users can only change their own password)

**URL Parameters:**
- `id`: The UUID of the user

**Request Body:**
```json
{
  "currentPassword": "SecurePassword123!",
  "newPassword": "NewSecurePassword456!"
}
```

**Response (204 No Content):**
No content in the response body on success.

**Possible Errors:**
- 400 Bad Request: Invalid current password
- 401 Unauthorized: Not authenticated
- 500 Internal Server Error

## Admin/Manager Endpoints

The following endpoints require admin or manager privileges and are typically not used in the frontend authentication flow:

### Get All Users

**Endpoint:** `GET /Users`

**Authentication:** Required (Admin or Manager role)

**Response (200 OK):**
```json
[
  {
    "userId": "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f",
    "username": "johndoe",
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "role": "auditor",
    "isActive": true
    // other user properties
  },
  // more users
]
```

### Get User by Username

**Endpoint:** `GET /Users/by-username/{username}`

**Authentication:** Required (Admin or Manager role)

**URL Parameters:**
- `username`: The username of the user

### Get Users by Organisation

**Endpoint:** `GET /Users/by-organisation/{organisationId}`

**Authentication:** Required (Admin or Manager role)

**URL Parameters:**
- `organisationId`: The UUID of the organisation

### Get Users by Role

**Endpoint:** `GET /Users/by-role/{role}`

**Authentication:** Required (Admin or Manager role)

**URL Parameters:**
- `role`: The role name (auditor, manager, supervisor, admin)

### Create User (Admin only)

**Endpoint:** `POST /Users`

**Authentication:** Required (Admin role only)

**Request Body:** Same as registration endpoint, but includes role field

### Update User

**Endpoint:** `PUT /Users/{id}`

**Authentication:** Required (Admin or Manager role)

**URL Parameters:**
- `id`: The UUID of the user

**Request Body:**
```json
{
  "userId": "8f7e6d5c-4b3a-2c1d-0e9f-8a7b6c5d4e3f",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phone": "1234567890",
  "role": "auditor",
  "isActive": true
}
```

### Deactivate User

**Endpoint:** `PATCH /Users/{id}/deactivate`

**Authentication:** Required (Admin role only)

**URL Parameters:**
- `id`: The UUID of the user

## Token Information

- JWT tokens expire after 8 hours
- Tokens include the following claims:
  - User ID (NameIdentifier)
  - Username (Name)
  - First Name (GivenName)
  - Last Name (Surname)
  - Role
  - Email (if provided)

## Error Handling

All endpoints follow a consistent error response format:

### 400 Bad Request
```json
{
  "message": "Error message describing the issue"
}
```

Common 400 errors:
- "Username already exists"
- "Email is already registered"
- "Error processing organisation ID. Please try again or leave it blank."
- "Database error with user organisation. Please contact support."

### 401 Unauthorized
```json
{
  "message": "Invalid username or password"
}
```
or
```json
{
  "message": "User account is deactivated"
}
```

### 403 Forbidden
Returned when a user attempts to access an endpoint without the required role.

### 404 Not Found
```json
{
  "message": "Resource not found"
}
```

### 500 Internal Server Error
```json
{
  "message": "An unexpected error occurred. Please try again later."
}
```

## Known Issues and Workarounds

### Organisation ID Field

The `organisationId` field in the User entity is optional (nullable):

1. When registering a new user:
   - You can omit the `organisationId` field completely
   - If you don't provide an `organisationId`, a default organisation will be created automatically
   - If you provide an `organisationId`, it must be a valid UUID of an existing organisation
   - If you provide an invalid `organisationId`, you'll receive an error

2. Default Organisation Creation:
   - When no organisation ID is provided, the system creates an organisation named "{FirstName}'s Organisation"
   - If an organisation with that name already exists, a timestamp is added to make it unique
   - The user is automatically associated with this new organisation

3. If you encounter errors related to organisation ID:
   - Try registering without an organisation ID to let the system create one for you
   - Verify that the organisation exists if you want to associate the user with a specific one
   - Check that the UUID format is correct

### Database Constraint Errors

If you encounter database constraint errors:
- Check that the role value is one of: "auditor", "manager", "supervisor", "admin"
- Verify that the organisation exists before attempting to register a user with its ID

## Authentication Flow for Frontend Integration

### Typical Authentication Flow

1. **User Registration**:
   - Call `POST /Auth/register` with user details (organisation ID is optional)
   - Store the returned JWT token in local storage or secure cookie
   - Redirect to the main application

2. **User Login**:
   - Call `POST /Auth/login` with username and password
   - Store the returned JWT token in local storage or secure cookie
   - Redirect to the main application

3. **Accessing Protected Resources**:
   - Include the JWT token in the Authorization header for all requests
   - Handle 401 responses by redirecting to the login page

4. **Token Expiration**:
   - When the token expires (after 8 hours), the API will return a 401 response
   - Redirect the user to the login page to obtain a new token

5. **Changing Password**:
   - Call `PATCH /Users/{id}/change-password` with current and new password
   - On success, optionally log the user out and request a new login

## Role-Based Access Control

The API implements role-based access control with the following roles:
- **auditor**: Regular users who can perform audits
- **manager**: Can manage users and view reports
- **supervisor**: Can supervise audits and manage templates
- **admin**: Full system access

Frontend applications should adjust their UI based on the user's role to show only relevant features. 