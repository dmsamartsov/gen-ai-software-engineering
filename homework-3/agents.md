# AI Agent Instructions: Virtual Card Lifecycle Management

You are an expert FinTech software engineer. This file contains the global constraints, domain rules, and architectural standards for this repository. You must adhere strictly to these instructions for all code generation, refactoring, and test creation tasks.

## 1. Tech Stack Assumptions
* **Language/Framework:** C# / ASP.NET Core (latest LTS version).
* **ORM:** Entity Framework (EF) Core.
* **Database:** Relational SQL (e.g., PostgreSQL or SQL Server).
* **Validation:** FluentValidation.
* **Testing:** xUnit, Moq, and FluentAssertions.

## 2. Domain Rules
* **Financial Calculations:** All monetary values must be represented as `int` or `long` in cents (e.g., `$10.00` = `1000`). Never use `float` or `double` for financial data.
* **Immutability:** Financial records (transactions, ledgers) are append-only. 
* **Soft Deletes:** Never perform hard `DELETE` operations on domain entities. Always use soft deletes (e.g., `Status = CardStatus.Canceled`).
* **Idempotency:** All mutating operations (Create, Update, Status changes) must be idempotent. If an operation is repeated with the same state or idempotency key, safely return the existing state without side effects.

## 3. Security & Compliance Constraints (Strict)
* **PCI-DSS Scope Minimization:** **NEVER** write code that accepts, processes, logs, or stores a full 16-digit Primary Account Number (PAN) or CVV in plain text. Only handle the `Last4` digits and the opaque `ProviderToken`.
* **RBAC Enforcement:** Every controller endpoint must have explicit role-based access control. Assume `ROLE_END_USER`, `ROLE_OPS_ADMIN`, and `ROLE_COMPLIANCE`.
* **Data Masking:** Ensure API responses automatically exclude internal database IDs, stack traces, and third-party API keys. Return standardized, sanitized DTOs (Data Transfer Objects).

## 4. Code Style & Architecture
* **Async All The Way:** Use asynchronous programming (`async`/`await`) for all I/O bound operations. Append `Async` to method names.
* **Dependency Injection:** Rely exclusively on constructor injection for services, repositories, and configurations.
* **Separation of Concerns:** Keep controllers thin. Controllers should only handle HTTP routing, payload validation, and delegating to the Application/Service layer.
* **Error Handling:** Do not use exceptions for standard control flow. Use standardized exception middleware to catch unhandled exceptions and return safe HTTP status codes (`400`, `403`, `404`, `500`).

## 5. Edge Cases & Resilience
* **Transactions:** Operations that write to multiple tables (e.g., updating a card status AND inserting an audit log) must be wrapped in an EF Core `IDbContextTransaction`. If the audit log fails, the card status update must roll back.
* **No Logging of Secrets:** Never log HTTP request/response bodies that might contain Personally Identifiable Information (PII) or secrets.
* **Concurrency:** Be defensive against race conditions. Use EF Core concurrency tokens or explicit isolation levels if multiple requests might mutate the same virtual card simultaneously.
* **Graceful Degradation:** If the external card provider API times out, fail securely and do not alter the internal database state.

## 6. Testing & Verification Expectations
* **Test Structure:** Follow the Arrange, Act, Assert (AAA) pattern explicitly in all test files.
* **Unit Tests:** Mock external dependencies (like the issuing provider API or the database context) to isolate business logic.
* **Integration Tests:** When testing data boundaries, use an in-memory database or Testcontainers. Do not mock `IDbContextTransaction` when verifying rollback behavior; execute the actual database commands to prove the rollback works.
* **Assertions:** Test for the exact expected HTTP status codes, error messages, and database state. Always include tests for the "unhappy path" (e.g., unauthorized access, invalid payloads, missing resources).
