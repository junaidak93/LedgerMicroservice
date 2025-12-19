---
name: Ledger Microservice Implementation
overview: Build a production-grade ledger microservice with .NET 8, PostgreSQL, clean architecture, authentication/authorization, audit trails, rate limiting, and comprehensive unit tests.
todos:
  - id: setup-project
    content: "Create solution structure: Ledger.API and Ledger.Tests projects, configure .csproj files with dependencies"
    status: completed
  - id: database-entities
    content: "Create entity models: Login, Transaction, RefreshToken, AuditLog with proper relationships and configurations"
    status: completed
    dependencies:
      - setup-project
  - id: dbcontext-migrations
    content: Implement ApplicationDbContext with entity configurations, create initial migration
    status: completed
    dependencies:
      - database-entities
  - id: repositories
    content: "Implement repository interfaces and implementations: ILoginRepository, ITransactionRepository, IRefreshTokenRepository, IStatsRepository, IAuditRepository"
    status: completed
    dependencies:
      - dbcontext-migrations
  - id: auth-service
    content: Implement AuthService with password hashing (SHA256), JWT token generation, refresh token management with sliding expiration
    status: completed
    dependencies:
      - repositories
  - id: transaction-service
    content: Implement TransactionService with balance validation, cached balance updates, transaction CRUD operations
    status: completed
    dependencies:
      - repositories
  - id: stats-service
    content: Implement StatsService for global and user statistics aggregation
    status: completed
    dependencies:
      - repositories
  - id: audit-service
    content: Implement AuditService for audit trail logging with CreatedBy, IpAddress, UserAgent, ServerTimestamp
    status: completed
    dependencies:
      - repositories
  - id: controllers
    content: Implement AuthController, TransactionsController, StatsController with proper authorization attributes and endpoint logic
    status: completed
    dependencies:
      - auth-service
      - transaction-service
      - stats-service
  - id: middleware
    content: Implement GlobalExceptionHandlerMiddleware, AuditMiddleware for request/response logging
    status: completed
    dependencies:
      - audit-service
  - id: rate-limiting
    content: Configure rate limiting (10 requests/min per user) for transaction endpoints
    status: completed
    dependencies:
      - controllers
  - id: health-checks
    content: Implement health check and readiness endpoints
    status: completed
    dependencies:
      - dbcontext-migrations
  - id: program-config
    content: "Configure Program.cs: DI registration, middleware pipeline, JWT authentication, Swagger, CORS, rate limiting"
    status: completed
    dependencies:
      - middleware
      - rate-limiting
      - health-checks
  - id: unit-tests
    content: "Write unit tests: AuthServiceTests, TransactionServiceTests, StatsServiceTests, repository tests"
    status: completed
    dependencies:
      - services
  - id: integration-tests
    content: "Write integration tests: AuthControllerTests, TransactionsControllerTests, StatsControllerTests, HealthCheckTests, ReadinessTests"
    status: completed
    dependencies:
      - controllers
      - health-checks
  - id: validation-dtos
    content: Add FluentValidation or Data Annotations to all DTOs, implement comprehensive input validation
    status: completed
    dependencies:
      - controllers
---

# Ledger Microser

vice Implementation Plan

## Architecture Overview

This microservice follows a clean, layered architecture with clear separation of concerns:

```javascript
┌─────────────────────────────────────────────────────────┐
│                    API Layer (Controllers)               │
│  AuthController │ TransactionsController │ StatsController│
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│                  Service Layer (Business Logic)          │
│  AuthService │ TransactionService │ StatsService        │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│                Repository Layer (Data Access)            │
│  ITransactionRepository │ IStatsRepository │ IAuthRepo  │
└─────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────┐
│                    Data Layer (EF Core)                  │
│  ApplicationDbContext │ Entities │ Migrations            │
└─────────────────────────────────────────────────────────┘
```



## Database Schema

### Login Table (Users)

- `Id` (Guid, PK)
- `Email` (string, unique, indexed)
- `PasswordHash` (string, SHA256)
- `Role` (enum: User, Admin, SuperAdmin)
- `Balance` (decimal, cached balance)
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime)

### Transaction Table

- `Id` (Guid, PK)
- `UserId` (Guid, FK to Login)
- `Amount` (decimal, precision 18, scale 2)
- `Type` (enum: Incoming, Outgoing)
- `Fee` (decimal, precision 18, scale 2)
- `Description` (string, nullable)
- `Timestamp` (DateTime)
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime)
- `DeletedAt` (DateTime?, nullable for soft delete)

### RefreshToken Table

- `Id` (Guid, PK)
- `UserId` (Guid, FK to Login)
- `Token` (string, indexed)
- `ExpiresAt` (DateTime)
- `CreatedAt` (DateTime)
- `IsRevoked` (bool)
- `RotatedFromTokenId` (Guid?, nullable, for rotation tracking)

### AuditLog Table

- `Id` (Guid, PK)
- `CreatedBy` (Guid?, nullable, FK to Login)
- `Action` (string, e.g., "Register", "Login", "CreateTransaction")
- `EntityType` (string, e.g., "Transaction", "User")
- `EntityId` (Guid?, nullable)
- `IpAddress` (string, nullable)
- `UserAgent` (string, nullable)
- `ServerTimestamp` (DateTime)
- `Details` (JSON string, nullable)

## Project Structure

### Ledger.API/

- **Controllers/**
- `AuthController.cs` - Registration, login, refresh token
- `TransactionsController.cs` - Transaction CRUD operations
- `StatsController.cs` - Global and user statistics
- **Services/**
- `IAuthService.cs` / `AuthService.cs` - Authentication logic, password hashing, token management
- `ITransactionService.cs` / `TransactionService.cs` - Transaction business logic, balance updates
- `IStatsService.cs` / `StatsService.cs` - Statistics aggregation
- `IAuditService.cs` / `AuditService.cs` - Audit logging
- **Repositories/**
- `ILoginRepository.cs` / `LoginRepository.cs` - User data access
- `ITransactionRepository.cs` / `TransactionRepository.cs` - Transaction data access
- `IRefreshTokenRepository.cs` / `RefreshTokenRepository.cs` - Refresh token management
- `IStatsRepository.cs` / `StatsRepository.cs` - Statistics queries
- `IAuditRepository.cs` / `AuditRepository.cs` - Audit log persistence
- **Models/**
- `Login.cs` - User entity
- `Transaction.cs` - Transaction entity
- `RefreshToken.cs` - Refresh token entity
- `AuditLog.cs` - Audit log entity
- `TransactionType.cs` - Enum (Incoming, Outgoing)
- `Role.cs` - Enum (User, Admin, SuperAdmin)
- **DTOs/**
- `RegisterDto.cs` - Email/Username + Password
- `LoginDto.cs` - Credentials
- `TokenResponseDto.cs` - Access + Refresh tokens
- `RefreshTokenDto.cs` - Refresh token request
- `TransactionCreateDto.cs` - Transaction creation
- `TransactionUpdateDto.cs` - Transaction update
- `TransactionResponseDto.cs` - Transaction response
- `StatsDto.cs` - Statistics response
- **Data/**
- `ApplicationDbContext.cs` - EF Core DbContext
- `Configurations/` - Entity configurations
- **Middleware/**
- `GlobalExceptionHandlerMiddleware.cs` - Global error handling
- `AuditMiddleware.cs` - Request/response logging
- **Helpers/**
- `PasswordHasher.cs` - SHA256 hashing utility
- `JwtHelper.cs` - JWT token generation/validation
- `ClaimsHelper.cs` - Claims extraction utilities
- **Extensions/**
- `ServiceCollectionExtensions.cs` - DI registration
- `ApplicationBuilderExtensions.cs` - Middleware registration
- **Program.cs** - Application entry point, configuration

### Ledger.Tests/

- **Controllers/**
- `AuthControllerTests.cs` - Auth endpoint tests
- `TransactionsControllerTests.cs` - Transaction endpoint tests
- `StatsControllerTests.cs` - Stats endpoint tests
- **Services/**
- `AuthServiceTests.cs` - Authentication logic tests
- `TransactionServiceTests.cs` - Transaction logic tests
- `StatsServiceTests.cs` - Statistics tests
- **Repositories/**
- `TransactionRepositoryTests.cs` - Repository tests
- **Helpers/**
- `TestHelpers.cs` - Test utilities, mock data
- **Integration/**
- `HealthCheckTests.cs` - Health endpoint tests
- `ReadinessTests.cs` - Readiness endpoint tests

## Key Implementation Details

### Authentication & Authorization

- **Password Hashing**: SHA256 hashing in `PasswordHasher` helper
- **JWT Tokens**: Access tokens (short-lived) + Refresh tokens (long-lived)
- **Refresh Token Rotation**: Sliding expiration - extend expiry on use, rotate periodically
- **Claims**: UserId, Email, Role stored in JWT claims
- **Authorization**: `[Authorize]` on all controllers except AuthController
- **Permission Checks**: Users can only access their own transactions unless Admin/SuperAdmin

### Data Integrity

- **Balance Validation**: Prevent negative balances in `TransactionService`
- **Transaction Constraints**: Validate Amount > 0, Fee >= 0
- **Cached Balance**: Update `Login.Balance` on transaction create/update/delete
- **Database Constraints**: Unique indexes, foreign keys, check constraints

### Rate Limiting

- **Implementation**: ASP.NET Core built-in rate limiting (10 requests/min per user)
- **Scope**: Applied to transaction endpoints
- **Key**: Based on UserId from JWT claims

### Audit Trail

- **Middleware**: `AuditMiddleware` captures all requests
- **Service**: `AuditService` persists audit logs
- **Fields**: CreatedBy (from token), IpAddress, UserAgent, ServerTimestamp
- **Actions**: Register, Login, CreateTransaction, UpdateTransaction, DeleteTransaction, etc.

### Error Handling

- **Global Middleware**: `GlobalExceptionHandlerMiddleware` catches all exceptions
- **HTTP Status Codes**: Proper mapping (400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 500 Internal Server Error)
- **Error Response DTO**: Consistent error response format

### API Endpoints

**Authentication:**

- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh access token

**Transactions:**

- `POST /api/transactions` - Create transaction (User specific)
- `GET /api/transactions` - List all transactions (Admin only)
- `GET /api/transactions/{id}` - Get transaction (User owns or Admin)
- `PUT /api/transactions/{id}` - Update transaction (User owns or Admin)
- `DELETE /api/transactions/{id}` - Soft delete (Admin only)
- `GET /api/users/{userId}/transactions` - Get user transactions (User owns or Admin)

**Statistics:**

- `GET /api/statistics/global` - Global stats (Admin only)
- `GET /api/statistics/users/{userId}` - User stats (User owns or Admin)

**Health:**

- `GET /api/health` - Public health check
- `GET /api/readiness` - Internal readiness check

### Validation

- **DTO Validation**: FluentValidation or Data Annotations
- **Business Rules**: Service layer validation
- **Input Sanitization**: XSS prevention, SQL injection prevention (EF Core handles)

### Performance

- **Pagination**: Implemented in transaction list endpoints
- **Database Indexes**: UserId, Email, Token, DeletedAt
- **Efficient Queries**: Use projections, avoid N+1 queries
- **Connection Pooling**: Configured in DbContext

### Testing Strategy

- **Unit Tests**: Service and repository logic
- **Integration Tests**: Controller endpoints with in-memory database
- **Test Coverage**: Auth flows, transaction CRUD, authorization, edge cases
- **Test Data**: Factory pattern for test entities

## Implementation Order

1. **Foundation**: Project structure, DbContext, Entities, Migrations
2. **Authentication**: Login entity, password hashing, JWT setup, AuthService, AuthController
3. **Core Features**: Transaction entity, repositories, TransactionService, TransactionsController
4. **Statistics**: StatsRepository, StatsService, StatsController
5. **Infrastructure**: Audit logging, exception handling, rate limiting, middleware
6. **Testing**: Unit tests, integration tests, health checks
7. **Documentation**: Swagger configuration, API documentation

## Configuration Files

- `appsettings.json` - Application configuration
- `appsettings.Development.json` - Development settings
- `.csproj` - Project file with dependencies
- `Ledger.API.csproj` - Main project
- `Ledger.Tests.csproj` - Test project

## Dependencies

- `Microsoft.EntityFrameworkCore` (8.0)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (8.0)
- `Microsoft.AspNetCore.Authentication.JwtBearer` (8.0)
- `Swashbuckle.AspNetCore` (6.5+)
- `FluentValidation.AspNetCore` (11.3+)
- `xunit` (2.4+)
- `Moq` (4.20+)
- `Microsoft.AspNetCore.Mvc.Testing` (8.0)

## Security Considerations

- Password hashing (SHA256)
- JWT token security (expiration, signing)
- Refresh token rotation
- SQL injection prevention (EF Core)
- XSS prevention (input validation)
- CORS configuration
- HTTPS enforcement (production)
- Rate limiting
- Authorization checks at service layer

## Production Readiness Features

- Health checks
- Readiness probes
- Structured logging
- Error handling
- Audit trails