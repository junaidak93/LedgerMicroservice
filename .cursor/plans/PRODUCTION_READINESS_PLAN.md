# Production Readiness Remediation Plan

_Last updated: 2025-12-21_

TL;DR — Assess and remediate security, data integrity, observability, and operational gaps. Focus first on secrets, password hashing, transactional safety, DTO validation, structured logging/metrics, and per-user rate limiting.

## Prioritized Checklist (short)

1. Move secrets to a secret store (Key Vault / Secrets Manager / Vault) — edit `Ledger.API/Program.cs`, `Ledger.API/appsettings.json`
2. Replace `PasswordHasher` with Argon2id/PBKDF2 — edit `Ledger.API/Helpers/PasswordHasher.cs`, `Ledger.API/Services/AuthService.cs`
3. Add DB transactions & idempotency for money operations — edit `Ledger.API/Services/TransactionService.cs`, `Ledger.API/Repositories/*`, `Ledger.API/Data/ApplicationDbContext.cs`
4. Add DTO validation (FluentValidation) and global model error handling — add validators in `Ledger.API/DTOs`, wire in `Ledger.API/Program.cs`
5. Integrate structured logging & distributed tracing (Serilog, OpenTelemetry) — edit `Ledger.API/Program.cs`, `Ledger.API/Middleware/*`
6. Tighten CORS and add security headers (HSTS, CSP) — edit `Ledger.API/Program.cs`
7. Implement per-user rate limiting keyed by JWT claim — edit `Ledger.API/Program.cs`, middleware
8. Add readiness/health checks including DB and migrations — edit `Ledger.API/Controllers/HealthController.cs`, startup wiring
9. Enforce DB check constraints and unique indexes to preserve invariants — edit `Ledger.API/Data/ApplicationDbContext.cs` and migrations
10. Add idempotency key support for transaction endpoints — edit `Ledger.API/Controllers/TransactionsController.cs`, add `IdempotencyKeys` table
11. Expand integration and concurrency tests — add `Ledger.Tests/Integration/*`
12. Add CI steps to run integration tests and static analysis — edit `.github/workflows/*`
13. Add deployment and migration automation (IaC + migration-run step) — new `infra/` or `deploy/` artifacts
14. Document security and operational runbooks (secrets rotation, incident playbooks) — `docs/` updates
15. Add client guidance for retries/backoff and rate-limit headers — docs and controller responses

## Top 5 Implementation Plans (detailed)

### 1) Move secrets to a secret store (High)
Goal: Remove hard-coded secrets and load from a secure provider.
Steps:
- Choose provider: Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault. (Decision required)
- Add provider libraries and configuration provider extension in `Ledger.API/Program.cs`.
- Remove direct secrets from `Ledger.API/appsettings.json`; refer to configuration keys instead.
- Add local dev fallback using `dotnet user-secrets` or a local `secrets.Development.json` secure file.
- Add an integration test validating secret retrieval and update README with setup steps.
Design notes: Use managed identities for cloud hosts; restrict access to service principals.

### 2) Replace password hasher with Argon2id (High)
Goal: Use a memory-hard, salted algorithm with configurable parameters.
Steps:
- Pick an established .NET library (e.g., `Isopoh.Cryptography.Argon2` or `Konscious.Security.Cryptography.Argon2`).
- Replace implementation in `Ledger.API/Helpers/PasswordHasher.cs` to produce versioned hashes including salt and parameters.
- Update `Ledger.API/Services/AuthService.cs` to rehash legacy SHA256 passwords on successful login.
- Surface hashing parameters (memory, iterations, parallelism) via configuration.
- Add unit tests in `Ledger.Tests/Services` for hashing and verification.
Design notes: Keep a migration strategy for live users to avoid forced resets.

### 3) DB transactions & idempotency (High)
Goal: Make money operations atomic and idempotent under concurrency.
Steps:
- Add transactional API surface to repositories or allow services to control `DbContext` transactions.
- Wrap critical flows in `TransactionService` (`BeginTransactionAsync`/`Commit`/`Rollback`).
- Create an `IdempotencyKeys` table to store request keys and results; accept `Idempotency-Key` header in `TransactionsController`.
- Add DB constraints (check non-negative balance, foreign keys) in `ApplicationDbContext` and migrations.
- Add integration tests that run concurrent creation to assert no double-spend.
Design notes: Use serializable or repeatable-read isolation for critical operations where supported.

### 4) Add DTO validation (Medium)
Goal: Validate inputs at the API boundary and return consistent problem details.
Steps:
- Add `FluentValidation.AspNetCore` package and register validators in `Program.cs`.
- Implement validators (e.g., `TransactionCreateDtoValidator`) next to DTOs in `Ledger.API/DTOs`.
- Add a global validation filter/middleware to return RFC7807 problem details on validation failure.
- Add unit tests for validators and controller tests that assert 400 responses.
Design notes: Keep validation focused on syntactic/semantic checks; preserve business checks in services.

### 5) Structured logging & tracing (Medium)
Goal: Centralized structured logs and distributed traces for diagnostics.
Steps:
- Integrate `Serilog` in `Program.cs` with structured sinks (console, file, Seq/ELK).
- Add OpenTelemetry instrumentation for incoming HTTP, EF Core, and background work.
- Update `AuditMiddleware` to emit structured events with trace and user IDs.
- Expose Prometheus metrics endpoint and instrument key counters and histograms.
- Add verification steps to test logs and traces locally.
Design notes: Ensure log PII redaction and sampling for high-volume traces.

## Findings Summary (key gaps with evidence)
- Secret management: `Ledger.API/Program.cs` reads secrets from config; `Ledger.API/appsettings.json` contains secrets.
- Weak password hashing: `Ledger.API/Helpers/PasswordHasher.cs` uses SHA256 (unsafe for passwords).
- CORS & surface area: `Ledger.API/Program.cs` enables permissive CORS.
- JWT lifecycle shortcomings: `Ledger.API/Helpers/JwtHelper.cs` and refresh token repos lack rotation/revocation design.
- Concurrency: `Ledger.API/Services/TransactionService.cs` and `Ledger.API/Repositories/*` do not use explicit DB transactions or idempotency keys.
- Validation: DTOs exist under `Ledger.API/DTOs` but validators are not present or wired.
- Observability: Only `AuditMiddleware` and `GlobalExceptionHandlerMiddleware` are present; no Serilog/OpenTelemetry.
- Rate limiting: Global limiter configured in `Program.cs` not keyed by user.
- Health checks: `Ledger.API/Controllers/HealthController.cs` exists but needs DB/migration checks wired.

(See repository files for details: `Ledger.API/Program.cs`, `Ledger.API/Helpers/PasswordHasher.cs`, `Ledger.API/Services/TransactionService.cs`, `Ledger.API/Data/ApplicationDbContext.cs`, `Ledger.API/DTOs`.)

## Questions / Decisions Needed
1. Which secrets provider should I target first: Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault?
2. Prefer `Argon2id` or `PBKDF2` for password hashing in your environment?
3. Which top item should I implement now (secrets, password hasher, transactions, validation, or logging)?

## Next steps I can take on request
- Implement and test one top-priority item with code, tests, and docs.
- Add migration and CI changes for the selected item.

---

Generated by the repo analysis session. If you want me to implement the first remediation (code + tests), tell me which item to start with.