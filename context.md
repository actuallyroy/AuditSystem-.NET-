# Retail Execution Audit System - Project Context

## Project Overview
A comprehensive retail execution audit system built with .NET 8 Web API backend and PostgreSQL database. The system supports multi-tenant organizations with role-based access control, template management, audit assignments, and execution tracking.

## Architecture
- **Backend**: .NET 8 Web API with Clean Architecture
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT-based with role-based authorization
- **Documentation**: Swagger/OpenAPI
- **Containerization**: Docker with docker-compose
- **Testing**: Python-based test tools

## Project Structure

### Root Level Files
- **`AuditSystem.sln`** - Main Visual Studio solution file
- **`docker-compose.yml`** - Production Docker configuration
- **`docker-compose.dev.yml`** - Development Docker configuration  
- **`Dockerfile`** - Container build instructions
- **`.dockerignore`** - Docker build exclusions
- **`.gitignore`** - Git version control exclusions
- **`tables.sql`** - Database schema definition
- **`README.md`** - Main project documentation
- **`README.Docker.md`** - Docker-specific setup guide
- **`project-guide.md`** - Development guidelines
- **`full-project-spec.md`** - Complete project specification
- **`api-documentation.md`** - API endpoint documentation
- **`AuditSystem.postman_collection.json`** - Postman API collection
- **`AuditSystem.API.http`** - HTTP client requests file
- **`test_and_rebuild.py`** - Python test and rebuild script
- **`context.md`** - This file (project context)

### Documentation Files
- **`authentication_improvements_summary.md`** - Authentication and authorization improvements
- **`template-implementation-summary.md`** - Template system implementation details
- **`test_permissions_summary.md`** - Permission testing results

### Backend Source Code (`src/`)

#### 1. AuditSystem.API (Presentation Layer)
**Location**: `src/AuditSystem.API/`
**Purpose**: Web API controllers, DTOs, and configuration

**Core Files**:
- **`Program.cs`** - Application entry point and configuration
- **`AuditSystem.API.csproj`** - Project file with dependencies
- **`appsettings.json`** - Production configuration
- **`appsettings.Development.json`** - Development configuration
- **`AuditSystem.API.http`** - HTTP client requests

**Controllers** (`Controllers/`):
- **`BaseApiController.cs`** - Base controller with common functionality
- **`AuthController.cs`** - Authentication and user management endpoints
- **`UsersController.cs`** - User CRUD operations
- **`OrganisationsController.cs`** - Organization management
- **`TemplatesController.cs`** - Template CRUD and assignment
- **`AssignmentsController.cs`** - Audit assignment management
- **`AuditsController.cs`** - Audit execution and reporting

**Models** (`Models/`):
- **`UserDto.cs`** - User data transfer objects
- **`TemplateDto.cs`** - Template DTOs with validation
- **`AuditDto.cs`** - Audit DTOs with store information extraction

**Authorization** (`Authorization/`):
- **`CaseInsensitiveRoleRequirement.cs`** - Custom role requirement handler

**Swagger** (`SwaggerSchemaFilters/`):
- **`JsonElementSchemaFilter.cs`** - Custom Swagger schema filtering

**Supporting Directories**:
- **`Properties/`** - Project properties and launch settings
- **`logs/`** - Application log files
- **`bin/`** - Compiled binaries
- **`obj/`** - Build artifacts
- **`src/`** - Additional source files

#### 2. AuditSystem.Domain (Domain Layer)
**Location**: `src/AuditSystem.Domain/`
**Purpose**: Business entities, interfaces, and domain logic

**Project File**:
- **`AuditSystem.Domain.csproj`** - Domain project configuration

**Entities** (`Entities/`):
- **`User.cs`** - User domain entity with roles and organization
- **`Organisation.cs`** - Organization entity
- **`OrganisationInvitation.cs`** - Organization invitation entity
- **`Template.cs`** - Audit template entity
- **`Assignment.cs`** - Audit assignment entity
- **`Audit.cs`** - Audit execution entity
- **`Report.cs`** - Audit report entity
- **`Log.cs`** - System logging entity

**Repositories** (`Repositories/`):
- **`IRepository.cs`** - Generic repository interface
- **`IUserRepository.cs`** - User-specific repository interface
- **`IOrganisationRepository.cs`** - Organization repository interface
- **`ITemplateRepository.cs`** - Template repository interface
- **`IAssignmentRepository.cs`** - Assignment repository interface
- **`IAuditRepository.cs`** - Audit repository interface

**Services** (`Services/`):
- **`IUserService.cs`** - User business logic interface
- **`IOrganisationService.cs`** - Organization business logic interface
- **`ITemplateService.cs`** - Template business logic interface
- **`IAssignmentService.cs`** - Assignment business logic interface
- **`IAuditService.cs`** - Audit business logic interface

**Supporting Directories**:
- **`bin/`** - Compiled binaries
- **`obj/`** - Build artifacts

#### 3. AuditSystem.Services (Application Layer)
**Location**: `src/AuditSystem.Services/`
**Purpose**: Business logic implementation

**Service Implementations**:
- **`UserService.cs`** - User management business logic
- **`OrganisationService.cs`** - Organization management logic
- **`TemplateService.cs`** - Template CRUD and validation logic
- **`AssignmentService.cs`** - Assignment management logic
- **`AuditService.cs`** - Audit execution and reporting logic

**Project File**:
- **`AuditSystem.Services.csproj`** - Services project configuration

**Supporting Directories**:
- **`bin/`** - Compiled binaries
- **`obj/`** - Build artifacts

#### 4. AuditSystem.Infrastructure (Infrastructure Layer)
**Location**: `src/AuditSystem.Infrastructure/`
**Purpose**: Data access, external services, and infrastructure concerns

**Key Components**:
- **`AuditSystemDbContext.cs`** - Entity Framework DbContext
- **`Class1.cs`** - Infrastructure placeholder
- **`AuditSystem.Infrastructure.csproj`** - Infrastructure project file

**Repositories** (`Repositories/`):
- **`Repository.cs`** - Generic repository implementation
- **`UserRepository.cs`** - User data access implementation
- **`OrganisationRepository.cs`** - Organization data access
- **`TemplateRepository.cs`** - Template data access
- **`AssignmentRepository.cs`** - Assignment data access
- **`AuditRepository.cs`** - Audit data access

**Data** (`Data/`):
- **`AuditSystemDbContext.cs`** - Main database context

**Supporting Directories**:
- **`bin/`** - Compiled binaries
- **`obj/`** - Build artifacts

#### 5. AuditSystem.Common (Shared Layer)
**Location**: `src/AuditSystem.Common/`
**Purpose**: Shared utilities, constants, and common functionality

**Project File**:
- **`AuditSystem.Common.csproj`** - Common project configuration

**Supporting Directories**:
- **`bin/`** - Compiled binaries
- **`obj/`** - Build artifacts

#### 6. Additional Source Files
**Location**: `src/src/`
**Purpose**: Additional source files and build artifacts

**Supporting Directories**:
- **`obj/`** - Build artifacts including Entity Framework targets

### Database and Configuration

#### Database Scripts (`init-scripts/`)
- **`01-init-database.sql`** - Database initialization script
- **`02-seed-data.sql`** - Seed data for development

#### PgAdmin Configuration (`pgadmin/`)
- **`servers.json`** - PgAdmin server configurations
- **`pgpass`** - Production password file
- **`pgpass.dev`** - Development password file

### Testing and Tools

#### Python Testing Tools (`python_tests/`)
- **`test_and_rebuild.py`** - Main test and rebuild script
- **`tools/`** - Testing utilities
  - **`db_query_tool.py`** - Database query testing tool
  - **`test_credentials.json`** - Test user credentials

## Key Features

### Authentication & Authorization
- JWT-based authentication
- Role-based access control (Admin, Manager, Auditor)
- Organization-based data isolation
- Custom authorization requirements

### Template Management
- Template CRUD operations
- Template assignment to auditors
- Template uniqueness per user
- Role-based template access

### Audit Execution
- Audit assignment management
- Audit execution tracking
- Status management (draft, in_progress, completed, approved, rejected)
- Store information extraction from JSONB

### Organization Management
- Multi-tenant organization support
- User invitation system
- Organization-based data isolation

## Database Schema
- **Users**: User accounts with roles and organization membership
- **Organisations**: Multi-tenant organizations
- **OrganisationInvitations**: Pending organization invitations
- **Templates**: Audit templates with JSONB structure
- **Assignments**: Template assignments to auditors
- **Audits**: Audit executions with store information
- **Reports**: Audit reports and summaries
- **Logs**: System activity logging

## Development Workflow
1. **Database**: Use `tables.sql` for schema, `init-scripts/` for setup
2. **API**: Controllers in `AuditSystem.API/Controllers/`
3. **Business Logic**: Services in `AuditSystem.Services/`
4. **Data Access**: Repositories in `AuditSystem.Infrastructure/`
5. **Testing**: Use `python_tests/` tools and Postman collection
6. **Documentation**: API docs in `api-documentation.md`

## Docker Support
- Production: `docker-compose.yml`
- Development: `docker-compose.dev.yml`
- Documentation: `README.Docker.md`
- Build: `Dockerfile` and `.dockerignore`

This comprehensive structure supports a scalable, maintainable retail audit system with proper separation of concerns and enterprise-grade features. 