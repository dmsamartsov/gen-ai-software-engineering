# End-User Card Freezing Specification

> Ingest the information from this file, implement the Low-Level Tasks, and generate the code that will satisfy the High and Mid-Level Objectives.

## High-Level Objective
- Implement a secure API endpoint that allows authenticated cardholders (`ROLE_END_USER`) to freeze and unfreeze their own virtual cards in a strictly regulated environment.

## Mid-Level Objectives
- Enforce strict authorization boundaries to guarantee users can only modify cards they own.
- Securely update the `VirtualCard` status in the database (`ACTIVE` <-> `FROZEN`).
- Generate an immutable audit log entry for every status change to satisfy compliance requirements.
- Implement robust error handling, returning standardized HTTP status codes (`403 Forbidden`, `404 Not Found`, `400 Bad Request`, `409 Conflict`).
- Achieve strict FinTech performance and consistency targets (synchronous latency < 200ms, immediate read-after-write consistency).

## Implementation Notes
- **Data Privacy:** Never expose internal database stack traces or raw error messages. API responses must return sanitized data.
- **Transactionality:** The card status update and the `AuditLog` insertion *must* be executed within a single ACID-compliant database transaction (e.g., using Entity Framework Core's `IDbContextTransaction`) to prevent orphaned updates.
- **Expected Performance & Constraints:**
  - **API Latency:** The `PATCH` request must respond in **< 200ms (p95)** to provide immediate user feedback during suspected fraud scenarios.
  - **Time-to-Consistency:** Read-after-write consistency must be **immediate (0ms delay)**. Eventual consistency is unacceptable for security toggles.
  - **Rate Limiting:** Maximum **5 requests per minute per user** for this endpoint to prevent bot scraping and race condition exploitation.
- **Edge Cases & Failure Modes:**
  | Scenario | Expected User-Visible Outcome | Audit / Compliance Implication |
  | :--- | :--- | :--- |
  | **Concurrent Actions** (User double-clicks "Freeze") | First succeeds (`200 OK`). Second returns `200 OK` (idempotent) or `429 Too Many Requests`. | Only **one** `CARD_STATUS_CHANGED` entry is written to the `AuditLog`. |
  | **Partial Database Failure** | Returns `500 Internal Server Error`. | Transaction rollback ensures the card is **not** frozen if the audit log fails. |
  | **Stale Data / Invalid State** (Ops just hard-canceled card) | Returns `409 Conflict` or `422 Unprocessable Entity`. | Failed mutation is logged in standard app logs; no DB `AuditLog` generated. |
  | **Provider Timeout** (External API sync fails) | Returns `502 Bad Gateway` or `504 Gateway Timeout`. | Database transaction rolls back. DB state remains in sync with the provider. |

## Context

### Beginning context
- Database schema and EF Core models for `VirtualCard` and `AuditLog` exist.
- ASP.NET Core Authentication/JWT middleware is configured and sets the `ClaimsPrincipal` with `UserId` and `Role` claims.
- Base exception handling middleware is present.

### Ending context
- A fully functional `PATCH /api/v1/cards/{id}/status` endpoint in the Controllers layer.
- Updated Application Service layer containing the transactional business logic.
- Comprehensive Verification Strategy executed:
  - **RBAC Check:** E2E/Integration tests confirm `403 Forbidden` for cross-user state mutations.
  - **Audit Logging Check:** Unit tests confirm `actor_id`, `previous_state`, and `new_state` match database fixtures exactly.
  - **Reconciliation:** Manual/staging runbooks updated to verify external provider sync.

## Low-Level Tasks

### 1. Data Validation & Security Task

What prompt would you run to complete this task?
Implement request payload validation and the ownership authorization check for the card status endpoint using ASP.NET Core authorization policies or controller logic.

What file do you want to CREATE or UPDATE?
`src/Api/Authorization/CardOwnershipHandler.cs` (or handle within `src/Api/Controllers/CardsController.cs`)

What function do you want to CREATE or UPDATE?
`VerifyCardOwnershipAsync` and Payload validation (e.g., using FluentValidation).

What are details you want to add to drive the code changes?
- Create a validator enforcing the payload: `{ status: "ACTIVE" | "FROZEN" }`.
- Fetch the `VirtualCard` by the ID parameter. If null, return `404 Not Found`.
- Compare `card.UserId` with the `UserId` claim. If mismatched, return `403 Forbidden`.
- **Definition of Done (AC):** A request with a missing JWT or mismatched `UserId` is rejected within < 50ms without executing any mutating database queries.


### 2. Business Logic & Compliance Task

What prompt would you run to complete this task?
Implement the service logic to update the card's status and write the required compliance audit log within a single EF Core database transaction.

What file do you want to CREATE or UPDATE?
`src/Application/Services/CardService.cs`

What function do you want to CREATE or UPDATE?
`ToggleCardStatusAsync`

What are details you want to add to drive the code changes?
- Function signature: `Task<CardDto> ToggleCardStatusAsync(Guid cardId, string newStatus, Guid actorId)`
- Check if `card.Status == newStatus`. If true, return early (Idempotency check).
- Open `IDbContextTransaction`.
- Update `VirtualCard` record.
- Insert `AuditLog`: `ActorId` (the user), `Action` ("CARD_STATUS_CHANGED"), `ResourceId` (cardId), `PreviousState` (JSON), `NewState` (JSON).
- Commit transaction. Rollback on exception.
- **Definition of Done (AC):** The database transaction isolation level is set to `ReadCommitted` or `Serializable` to prevent race conditions during the status toggle and audit insert.


### 3. API Endpoint Task

What prompt would you run to complete this task?
Wire up the `PATCH /api/v1/cards/{id}/status` route, applying rate limiting, authorization, and invoking the service logic.

What file do you want to CREATE or UPDATE?
`src/Api/Controllers/CardsController.cs`

What function do you want to CREATE or UPDATE?
`PatchCardStatus`

What are details you want to add to drive the code changes?
- Route: `[HttpPatch("{id:guid}/status")]`
- Apply endpoint rate limiting attribute (max 5 requests/minute).
- Invoke validation and `ToggleCardStatusAsync`.
- **Definition of Done (AC):** The API response payload is strictly sanitized. It returns `{ id, status, last_4 }` and definitively excludes `provider_token`, internal DB sequential IDs, or raw error stack traces.


### 4. Security & Business Logic Testing

What prompt would you run to complete this task?
Create a comprehensive xUnit test suite verifying the success path, ownership constraints, and transaction rollback behavior for the card status endpoint.

What file do you want to CREATE or UPDATE?
`tests/Api.IntegrationTests/Controllers/CardsControllerTests.cs`

What function do you want to CREATE or UPDATE?
Test methods for `PatchCardStatus`

What are details you want to add to drive the code changes?
- Test 1 (Happy Path): Authenticated user successfully freezes their own active card. Assert `200 OK` and assert `AuditLog` creation in the test DB.
- Test 2 (Security): Authenticated user attempts to freeze another user's card. Assert `403 Forbidden` and assert database state remains unchanged.
- Test 3 (Validation): Invalid status payload (e.g., "DELETED"). Assert `400 Bad Request`.
- Test 4 (Idempotency): User freezes already-frozen card. Assert successful response without duplicate audit logs.
- **Definition of Done (AC):** Code coverage for the controller and service exceeds 90%, including the explicit simulation of an EF Core `DbUpdateException` to trigger and verify the transaction rollback.
