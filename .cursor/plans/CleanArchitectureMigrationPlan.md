# Clean Architecture Migration Plan for Ledger.API

**TL;DR**
Split `Ledger.API` into four projects: `Ledger.Domain`, `Ledger.Application`, `Ledger.Infrastructure`, and keep `Ledger.API` as Presentation. This decouples business rules from persistence and improves testability and security.

---

## Objective
Convert the monolithic `Ledger.API` project into Clean Architecture layers and migrate code accordingly.

## Layer Mapping (what goes where)

**Domain** (business entities / enums)
- Ledger.API/Models/Transaction.cs
- Ledger.API/Models/Login.cs
- Ledger.API/Models/RefreshToken.cs
- Ledger.API/Models/AuditLog.cs
- Ledger.API/Models/IdempotencyKey.cs
- Ledger.API/Models/Role.cs
- Ledger.API/Models/TransactionType.cs

**Application** (use-cases, DTOs, interfaces)
- Ledger.API/DTOs/* (TransactionCreateDto, TransactionUpdateDto, TransactionResponseDto, StatsDto, RefreshTokenDto, LoginDto, RegisterDto, TokenResponseDto)
- Ledger.API/Services/ITransactionService.cs
- Ledger.API/Services/IStatsService.cs
- Ledger.API/Services/IAuthService.cs
- Ledger.API/Services/IAuditService.cs
- Ledger.API/Repositories/ITransactionRepository.cs
- Ledger.API/Repositories/ILoginRepository.cs
- Ledger.API/Repositories/IRefreshTokenRepository.cs
- Ledger.API/Repositories/IStatsRepository.cs
- Ledger.API/Repositories/IIdempotencyRepository.cs
- Ledger.API/Repositories/IAuditRepository.cs

**Infrastructure** (persistence, external integrations, hosted services)
- Ledger.API/Data/ApplicationDbContext.cs
- Ledger.API/Data/ApplicationDbContextFactory.cs
- Ledger.API/Data/Migrations/*
- Ledger.API/Repositories/* (implementations)
- Ledger.API/Helpers/JwtHelper.cs
- Ledger.API/Helpers/PasswordHasher.cs
- Ledger.API/Services/IdempotencyCleanupService.cs

**Presentation** (Web API, controllers, middleware, composition root)
- Ledger.API/Controllers/*
- Ledger.API/Middleware/*
- Ledger.API/Program.cs
- ClaimsHelper and other HTTP helpers
- appsettings.json / appsettings.Development.json

**Mixed concerns / files needing refactor**
- `Ledger.API/Services/TransactionService.cs` — currently uses `ApplicationDbContext` and repositories directly; should depend on repository interfaces and a UnitOfWork abstraction instead.
- `Ledger.API/Services/AuthService.cs` — depends on concrete Jwt and Password helpers; introduce `IJwtService` and `IPasswordHasher` interfaces in Application and implement in Infrastructure.
- DTOs referencing domain enums — ensure DTOs live in Application and reference Domain types intentionally.
- `IdempotencyCleanupService` should be in Infrastructure (hosted background task).

---

## Migration Plan (ordered steps)

1) Create `Ledger.Domain` project — Move entities & enums (Effort: medium 2–6h)
- Files to move: Models listed above.
- Add new class library `Ledger.Domain` targeting same TF as solution.
- Update namespaces; update references in Application & Infrastructure.
- Tests: update type references in tests.

2) Create `Ledger.Application` project — Move DTOs and interfaces (Effort: medium 2–6h)
- Files to move: DTOs and service/repository interfaces.
- Add `Ledger.Application` project and reference `Ledger.Domain`.
- Keep mapping contracts and use-case interfaces here.
- Tests: update tests to reference Application DTOs & interfaces.

3) Create `Ledger.Infrastructure` project — Move persistence & platform code (Effort: large 6–20h)
- Files to move: DbContext, migrations, repository implementations, Jwt/Password helpers, hosted services.
- Implement `IJwtService`, `IPasswordHasher`, and `IUnitOfWork` (or `ITransactionScope`) interfaces defined in Application.
- Provide `AddInfrastructureServices(this IServiceCollection)` extension to register DbContext, repositories, hosted services, and helpers.
- Migrations should be set to the Infrastructure assembly; test EF Core commands.

4) Refactor Transaction use-case and unit-of-work (Effort: large 6–20h)
- Refactor `TransactionService` so Application-level use-case depends only on repository interfaces and a unit-of-work abstraction; implement unit-of-work in Infrastructure using `ApplicationDbContext.Database.BeginTransactionAsync()`.
- Move mapping to Application (or add AutoMapper).
- Ensure idempotency conflict handling and transaction semantics remain covered by integration tests.

5) Update `Ledger.API` (Presentation) to reference Application and call services (Effort: small 0.5–2h)
- Replace any direct repository/DbContext usage in controllers with calls to `I*Service` interfaces from Application.
- Keep Program.cs as composition root and call `AddInfrastructureServices`.
- Keep middleware and HTTP helpers in Presentation.

6) Update tests, CI, and EF tooling (Effort: medium 2–6h)
- Update test project references to new projects; adapt WebApplicationFactory to register Infrastructure services for integration tests.
- Update CI pipeline to build all projects and run migrations from Infrastructure (or use test migrations strategy).
- Run full test suite and fix issues.

---

## Migration & Deployment Considerations
- Migrations: moving DbContext and migrations to Infrastructure requires setting the migrations assembly and testing `dotnet ef` commands to generate/apply migrations correctly. Validate SQL in staging and have backup/rollback strategy.
- Configuration: keep connection strings in Presentation config but ensure Infrastructure reads them via DI. Consider secrets management for production.
- Integration tests: use SQLite in-memory or test containers to validate transactional and concurrency behaviors before deploying.

## Risks & Mitigations
- Namespace/assembly moves causing widespread compile errors — mitigate by moving incrementally and updating tests immediately.
- EF migrations break after moving DbContext — mitigate by setting migrations assembly explicitly and testing ef commands early.
- Circular dependencies — enforce layer rules: Application -> Domain; Infrastructure -> Application,Domain; Presentation -> Application.
- Idempotency race conditions — ensure Infrastructure insert handles unique-constraint conflicts and Application logic retries/returns existing response.
- Tests/CI breakage — update tests along the way and run CI after each step.

---

## Recommendations
- Implement `IUnitOfWork` in Infrastructure backed by `ApplicationDbContext.Database.BeginTransactionAsync()` and use it in Application services.
- Make password hashing parameters configurable; consider Argon2id later.
- Review stored `ResponseBody` for idempotency entries to avoid storing sensitive information; encrypt or redact if necessary.
- Add integration tests using SQLite or testcontainers to validate transaction semantics.

---

If you want, I can now scaffold the four new projects and make the first incremental move (create `Ledger.Domain` and move entities). Tell me which step to start with, and I will implement it and run the tests.
