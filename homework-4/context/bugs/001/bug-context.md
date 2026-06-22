# Bug Context — Batch 001 (Internal Wiki API)

This file is the **seed of truth** for the pipeline. It documents the defects that were
intentionally introduced into the sample application (`src/WikiApi`) so the 4-agent
pipeline has concrete, demonstrable work. Line numbers refer to the **buggy baseline**
(git tag `before-pipeline`) and may shift once fixes are applied.

## Application under test

- **Project:** `src/WikiApi` — a .NET 10 minimal-API internal wiki.
- **Run:** `dotnet run --project src/WikiApi --no-launch-profile --urls http://localhost:5099`
- **Test:** `dotnet test WikiApi.sln`
- **Storage:** in-memory (`DocumentStore`), seeded with 3 pages: *Onboarding Guide*,
  *VPN Setup*, *Expense Policy*.

## Work set (files in scope for this batch)

The researcher → planner → fixer chain may modify any of these; the security-verifier
must treat all of them as changed code:

- `src/WikiApi/Services/DocumentStore.cs`  (Bug #1, Bug #2)
- `src/WikiApi/Auth/AdminTokenValidator.cs` (Security issue SEC-1)
- `src/WikiApi/Program.cs`                  (Security issue SEC-2 — missing validation)

---

## Bug #1 — Case-sensitive search *(functional)*

- **Location:** `src/WikiApi/Services/DocumentStore.cs`, `Search(string query)` ~line 76.
- **Symptom:** `string.Contains(query)` uses an ordinal, case-sensitive comparison, so a
  search for `vpn` does **not** match the *VPN Setup* page.
- **Reproduction (buggy baseline):**
  ```
  GET /documents/search?q=vpn   ->  [] (0 results)   # WRONG
  GET /documents/search?q=VPN   ->  ["VPN Setup"]    # only exact case matches
  ```
- **Expected after fix:** `q=vpn` returns *VPN Setup*. Comparison should be
  case-insensitive across Title, Content, and Tags (e.g.
  `Contains(query, StringComparison.OrdinalIgnoreCase)`).

## Bug #2 — Update corrupts timestamps *(functional)*

- **Location:** `src/WikiApi/Services/DocumentStore.cs`, `Update(...)` ~line 58.
- **Symptom:** an update sets `existing.CreatedAt = now` (overwriting the original
  creation date) and never advances `UpdatedAt`. The two timestamps end up inverted —
  `CreatedAt` moves forward on each edit while `UpdatedAt` is frozen at creation.
- **Reproduction (buggy baseline):** create a doc, then `PUT` an edit; `CreatedAt`
  changes and `UpdatedAt` stays at the original value.
- **Expected after fix:** `CreatedAt` is preserved across updates; `UpdatedAt` is set to
  the current time on every update.

## SEC-1 — Hardcoded admin secret + timing-unsafe comparison *(security)*

- **Location:** `src/WikiApi/Auth/AdminTokenValidator.cs`, lines 13 and 26.
- **Symptom:**
  - `private const string AdminToken = "admin-secret-123";` — the privileged-delete
    secret is committed in source control; it cannot be rotated without a redeploy and
    is exposed to anyone with repo or binary access.
  - `return providedToken == AdminToken;` — ordinary string equality short-circuits on
    the first mismatching character, leaking how many leading characters were correct
    (timing side channel).
- **Reproduction (buggy baseline):** `DELETE /documents/{id}` with header
  `X-Admin-Token: admin-secret-123` returns `204`; the secret is visible in the repo.
- **Expected after fix:** read the token from configuration / environment
  (`WIKI_ADMIN_TOKEN`); compare in constant time
  (`CryptographicOperations.FixedTimeEquals` over UTF-8 bytes). The security-verifier
  confirms remediation on the changed file.

## SEC-2 — Missing input validation *(security, lower severity)*

- **Location:** `src/WikiApi/Program.cs`, `POST /documents` (~line 30) and
  `PUT /documents/{id}`.
- **Symptom:** no validation — an empty/whitespace `Title` or an arbitrarily large
  `Content` is accepted, enabling junk/oversized documents.
- **Expected after fix:** reject empty `Title` and cap `Title`/`Content` length,
  returning `400 Bad Request`.

---

## Pipeline expectations

1. **bug-researcher** records exact `file:line` + snippets for each item above.
2. **research-verifier** confirms every reference against source and rates research
   quality using `skills/research-quality-measurement.md`.
3. **bug-planner** turns verified research into `implementation-plan.md` (before/after
   per file, test command). Plan covers Bug #1, Bug #2, SEC-1, SEC-2.
4. **bug-fixer** applies the plan, runs `dotnet build` + `dotnet test`, writes
   `fix-summary.md`.
5. **security-verifier** reviews the changed files (report only), confirms SEC-1/SEC-2
   are resolved, and flags any residual issues with severity + remediation.
6. **unit-test-generator** adds FIRST-compliant xUnit tests for the changed code and
   writes `test-report.md`.
