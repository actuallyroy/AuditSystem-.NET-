# Project Context: Retail Execution Audit System

This document lists the important files in the backend solution and describes their purpose.

## Solution & Configuration
- **AuditSystem.sln**: The Visual Studio solution file that groups all projects in the backend.
- **appsettings.Development.json**: Configuration file for development environment (database, JWT, logging, etc.).

## Domain Layer (Business Entities & Contracts)
- **src/AuditSystem.Domain/Entities/**: Contains C# classes representing the core business entities:
  - `Organisation.cs`: Organisation/company data.
  - `User.cs`: User account and profile data.
  - `Template.cs`: Audit template definition (structure, questions, scoring).
  - `Assignment.cs`: Audit assignment to users/stores.
  - `Audit.cs`: Audit execution and results.
  - `Report.cs`: Generated reports metadata.
  - `Log.cs`: System and user activity logs.
- **src/AuditSystem.Domain/Repositories/**: Repository interfaces for data access abstraction:
  - `IRepository.cs`: Generic CRUD repository interface.
  - `IUserRepository.cs`, `ITemplateRepository.cs`, `IAuditRepository.cs`: Entity-specific repository contracts.
- **src/AuditSystem.Domain/Services/**: Service interfaces for business logic abstraction:
  - `IUserService.cs`, `ITemplateService.cs`, `IAuditService.cs`: Contracts for user, template, and audit business logic.

## Infrastructure Layer (Data Access)
- **src/AuditSystem.Infrastructure/Data/AuditSystemDbContext.cs**: Entity Framework Core DbContext mapping entities to the PostgreSQL database schema.
- **src/AuditSystem.Infrastructure/Repositories/**: Concrete implementations of repository interfaces for EF Core.
  - `Repository.cs`: Generic repository implementation.
  - `UserRepository.cs`, `TemplateRepository.cs`, `AuditRepository.cs`: Entity-specific repository implementations.

## Services Layer (Business Logic)
- **src/AuditSystem.Services/UserService.cs**: Implements user-related business logic (authentication, password management, etc.).

## API Layer (Web API)
- **src/AuditSystem.API/Program.cs**: Main entry point; configures DI, middleware, authentication, Swagger, etc.
- **src/AuditSystem.API/Controllers/**: Web API controllers for handling HTTP requests:
  - `BaseApiController.cs`: Base class for API controllers.
  - `UsersController.cs`: Endpoints for user management (CRUD, password, etc.).
  - `AuthController.cs`: Endpoints for authentication (login, JWT token issuance).

## Database & Specs
- **tables.sql**: SQL script for creating the initial PostgreSQL schema.
- **full-project-spec.md**: Full functional and technical specification for the system.
- **project-guide.md**: Onboarding and environment setup guide for developers.

---

This structure follows clean architecture principles, separating business logic, data access, and API layers for maintainability and scalability. 