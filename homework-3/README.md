# Home work 3 submition

**Student Name**: Dmytro Samartsov
**Date Submitted**: 2026-06-16

## Task Summary

This homework is a **specification package** for a finance-oriented application — no
implementation required. The graded artifact is the specification itself: how clearly the
problem is decomposed, how traceable requirements are from goals down to tasks, and how well
the spec anticipates failure modes, verification, and non-functional expectations.

The chosen feature is **end-user virtual card freezing / unfreezing** (`ACTIVE` ↔ `FROZEN`)
in a regulated FinTech environment, designed so that an engineering team *and* an AI coding
agent could execute it without guessing.

## Rationale

The specification is organized as **layers** so intent stays traceable from the north-star goal
down to executable tasks. The table below maps each layer to its purpose and the concrete
choices made for the card-freeze feature — and *why* those choices are reasonable for FinTech.

| Layer | Purpose | How this spec fills it — and why |
|-------|---------|----------------------------------|
| **High-level objective** | North star | • `ROLE_END_USER` freezes/unfreezes **their own** cards<br>• Scope bounded to `ACTIVE ↔ FROZEN` — keeps it executable |
| **Mid-level objectives** | Testable "what" | • Ownership enforced; status flips `ACTIVE ↔ FROZEN`<br>• Immutable audit per change<br>• Standardized HTTP codes (`403/404/400/409`) |
| **Non-functional & policy** | How well / how safely | • `< 200ms (p95)` + **immediate** read-after-write — fraud response<br>• **5 req/min per user** — curbs scraping/race abuse<br>• PCI-DSS, RBAC, audit logging mandated |
| **Implementation notes** | Guardrails for builders | • Money as integer cents (no `float`/`double`)<br>• Status update + audit in one ACID `IDbContextTransaction`<br>• Idempotent re-freeze; sanitized DTOs (no PAN/CVV, stack traces, DB IDs) |
| **Context (beginning / ending)** | Agent workspace | • *Before:* EF models, JWT middleware, exception middleware<br>• *After:* `PATCH /api/v1/cards/{id}/status`, transactional service, verification suite |
| **Low-level tasks** | Executable slices | • 4 tied-back tasks: validation+ownership, service+audit, controller+rate-limit, tests<br>• Each with a definition of done — incl. *real* `DbUpdateException` to prove rollback |

## Industry Best Practices & Where They Appear

| Practice | Where in the spec |
|----------|-------------------|
| **PCI-DSS scope minimization** (never store/log full PAN or CVV; only `Last4` + `ProviderToken`) | `agents.md` §3; `.claude/rules/rules.md` §4; sanitized-output AC in `feature-specification.md` Task 3 |
| **Money as integer minor units** (cents, never `float`/`double`) | `agents.md` §2; `.claude/rules/rules.md` §4 |
| **Immutable audit trail** (`ActorId`, `PreviousState`, `NewState` on every mutation) | Mid-level objectives + Task 2 in `feature-specification.md`; `agents.md` §2; rules §4 |
| **ACID transactionality** (status update + audit log in one `IDbContextTransaction`, roll back together) | `feature-specification.md` Implementation Notes + Task 2; `agents.md` §5; rules §5 |
| **RBAC & ownership enforcement** (`ROLE_END_USER`; users mutate only their own cards) | Mid-level objectives + Task 1 in `feature-specification.md`; `agents.md` §3 |
| **Idempotency** (re-freeze returns existing state, single audit entry) | Edge-case table + Task 2/Task 4 in `feature-specification.md`; `agents.md` §2 |
| **Soft deletes** (`Status = Canceled`, never hard `DELETE`) | `agents.md` §2; rules §5 |
| **Sanitized error handling** (no stack traces, no sequential DB IDs; DTOs only) | `feature-specification.md` Implementation Notes + Task 3; `agents.md` §3/§4; rules §5 |
| **Concurrency / graceful degradation** (concurrency tokens, secure provider-timeout handling) | Edge-case table in `feature-specification.md`; `agents.md` §5 |
