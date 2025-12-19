I want to build a production-grade ledger microservice similar to Keeper, a leading Earned Wage Access (EWA) platform. This service will track all user transactions with financial-grade precision and reliability.

#### Technical Requirements:
Framework: .NET 8 / ASP.NET Core
Database: PostgreSQL with Entity Framework Core
Architecture: Clean, layered architecture with clear separation of concerns
Quality: Production-ready with proper validation, error handling, and data integrity

#### Functional Requirements:
Transaction Management API (CRUD operations)
Create, Read, Update, Delete transactions
Each transaction must include: UserId, Amount, Type (Incoming/Outgoing), Fee, Timestamp, Description

#### Statistics API:
Global Stats: Total volume processed + total fees collected across all users
User Stats: Current balance + transaction count for specific UserId

#### Production Considerations (The "Silent" Tasks):
Data Integrity: Prevent negative balances, enforce transaction constraints
Validation: Comprehensive input validation at API boundaries
Error Handling: Global exception handling with proper HTTP status codes
Security: API security considerations (ready for authentication/authorization)
Performance: Pagination, efficient queries, database optimization
Auditability: Complete transaction trail with timestamps
Maintainability: Clean code, dependency injection, proper logging

Note: User must register first before using any API.

#### Architecture Plan:
##### 1. Project Structure
- Ledger.API/
   - Controllers/
   - Services/
   - Repositories/
   - Models/
   - Data/
   - DTOs/
   - Helpers/
   - Middleware/
   - Program.cs
- Ledger.Tests/
   - LedgerTests.cs
   - StatsTests.cs

##### 2. Core Components
- Data Layer
	- TransactionEntity
	- ApplicationDbContext
	- Database Migrations
- Repository Layer
	- IRefreshTokenRepository
	- RefreshTokenRepository
	- ITransactionRepository
	- TransactionRepository
	- IStatsRepository
	- StatsRepository
- Service Layer
	- IAuthService
	- AuthService
	- ITransactionService
	- TransactionService
	- IStatsService
	- StatsService
- Controller Layer
	- AuthController
	- TransactionsController
	- StatsController
- DTOs
	- TransactionCreateDto
	- TransactionUpdateDto
	- TransactionResponseDto
	- StatsDto
- Infrastructure
	- Global Exception Handler
	- Request/Response Logger Middleware
	- Health Check Endpoint
	- Swagger/Swashbuckle Documentation

##### 3. Solid Authentication/Authorization
- User Password to be SHA256 hashed and stored in Login table.
- Refresh Token to be stored in db.
- Refresh Token must have a rotation strategy.
- All controllers except for AuthController must have [Authorize]
- Claims-based permissions to be implemented
- Each user must be permitted to access his own transaction unless admin or above.

##### 4. Rate Limiting:
- Protect against transactions flooding (10 requests/min per user)

##### 5. Audit Trail:
Maintain Audit trail for every action including register, login and transactions.
Audit Log Table must have:
   - CreatedBy (User ID from token)
   - IpAddress (From HttpContext)
   - UserAgent (Client identification)
   - ServerTimestamp (Not client time)
		
##### 6. API Endpoints:
	POST    /api/transactions (User specific)
	GET     /api/transactions (Admin only)
	GET     /api/transactions/{id} (User owns or Admin)
	PUT     /api/transactions/{id} (User owns or Admin)
	DELETE  /api/transactions/{id} (Soft delete - Admin only)
	GET     /api/users/{userId}/transactions (User owns or Admin)
	
	GET     /api/statistics/global (Admin only)
	GET     /api/statistics/users/{userId} (User owns or Admin)
	GET     /api/health (Public health check)
	GET     /api/readiness (Internal)
	
##### 7. Unit Tests:
- Write Health test
- Write app readinless test
- Write various test cases for Authentication and Authorization.
- Write various test cases for User Transactions.
- Write Admin test cases.

