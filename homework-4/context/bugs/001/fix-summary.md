# Fix Summary — Batch 001

## Changes Made

### 1. Bug #1 — Case-sensitive search
* **File**: [DocumentStore.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Services/DocumentStore.cs)
* **Location**: `Search` method (lines 75-81)
* **Before**:
  ```csharp
  return _documents.Values
      .Where(d => d.Title.Contains(query)
                  || d.Content.Contains(query)
                  || d.Tags.Any(t => t.Contains(query)))
      .OrderBy(d => d.Title)
      .ToList();
  ```
* **After**:
  ```csharp
  return _documents.Values
      .Where(d => d.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                  || d.Content.Contains(query, StringComparison.OrdinalIgnoreCase)
                  || d.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
      .OrderBy(d => d.Title)
      .ToList();
  ```
* **Test Result**: Successfully verified via build and smoke tests.

---

### 2. Bug #2 — Update corrupts timestamps
* **File**: [DocumentStore.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Services/DocumentStore.cs)
* **Location**: `Update` method (line 58)
* **Before**:
  ```csharp
  existing.CreatedAt = _timeProvider.GetUtcNow();
  ```
* **After**:
  ```csharp
  existing.UpdatedAt = _timeProvider.GetUtcNow();
  ```
* **Test Result**: Successfully verified via build and smoke tests.

---

### 3. SEC-1 — Hardcoded admin secret + timing-unsafe comparison
* **Files**: [AdminTokenValidator.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Auth/AdminTokenValidator.cs) and [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs)
* **Location**: Constructor dependency injection in `AdminTokenValidator` and configuration injection in `Program.cs` (line 9).
* **Before (`AdminTokenValidator.cs`)**:
  ```csharp
  private const string AdminToken = "admin-secret-123";

  public bool IsValid(string? providedToken)
  {
      if (string.IsNullOrEmpty(providedToken))
      {
          return false;
      }
      return providedToken == AdminToken;
  }
  ```
* **After (`AdminTokenValidator.cs`)**:
  ```csharp
  private readonly string _adminToken;

  public AdminTokenValidator(string adminToken)
  {
      _adminToken = adminToken;
  }

  public bool IsValid(string? providedToken)
  {
      if (string.IsNullOrEmpty(providedToken) || string.IsNullOrEmpty(_adminToken))
      {
          return false;
      }

      var providedBytes = Encoding.UTF8.GetBytes(providedToken);
      var adminBytes = Encoding.UTF8.GetBytes(_adminToken);

      return CryptographicOperations.FixedTimeEquals(providedBytes, adminBytes);
  }
  ```
* **Before (`Program.cs`)**:
  ```csharp
  builder.Services.AddSingleton<AdminTokenValidator>();
  ```
* **After (`Program.cs`)**:
  ```csharp
  builder.Services.AddSingleton(new AdminTokenValidator(builder.Configuration["WIKI_ADMIN_TOKEN"] ?? string.Empty));
  ```
* **Test Result**: Successfully verified via build and smoke tests.

---

### 4. SEC-2 — Missing input validation
* **File**: [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs)
* **Location**: `MapPost` (POST `/documents`) and `MapPut` (PUT `/documents/{id}`) endpoints.
* **Before (POST)**:
  ```csharp
  // Create a new document.
  docs.MapPost("/", (CreateDocumentRequest request, IDocumentStore store) =>
  {
      // NOTE: no input validation here — an empty Title or arbitrarily large Content is
      // accepted as-is (seeded weakness for the security review to flag).
      var created = store.Create(request);
      return Results.Created($"/documents/{created.Id}", created);
  });
  ```
* **After (POST)**:
  ```csharp
  // Create a new document.
  docs.MapPost("/", (CreateDocumentRequest request, IDocumentStore store) =>
  {
      if (string.IsNullOrWhiteSpace(request.Title))
      {
          return Results.BadRequest("Title cannot be empty or whitespace.");
      }
      if (request.Title.Length > 100)
      {
          return Results.BadRequest("Title cannot exceed 100 characters.");
      }
      if (request.Content != null && request.Content.Length > 10000)
      {
          return Results.BadRequest("Content cannot exceed 10000 characters.");
      }

      var created = store.Create(request);
      return Results.Created($"/documents/{created.Id}", created);
  });
  ```
* **Before (PUT)**:
  ```csharp
  // Update an existing document.
  docs.MapPut("/{id:guid}", (Guid id, UpdateDocumentRequest request, IDocumentStore store) =>
  {
      var updated = store.Update(id, request);
      return updated is null ? Results.NotFound() : Results.Ok(updated);
  });
  ```
* **After (PUT)**:
  ```csharp
  // Update an existing document.
  docs.MapPut("/{id:guid}", (Guid id, UpdateDocumentRequest request, IDocumentStore store) =>
  {
      if (string.IsNullOrWhiteSpace(request.Title))
      {
          return Results.BadRequest("Title cannot be empty or whitespace.");
      }
      if (request.Title.Length > 100)
      {
          return Results.BadRequest("Title cannot exceed 100 characters.");
      }
      if (request.Content != null && request.Content.Length > 10000)
      {
          return Results.BadRequest("Content cannot exceed 10000 characters.");
      }

      var updated = store.Update(id, request);
      return updated is null ? Results.NotFound() : Results.Ok(updated);
  });
  ```
* **Test Result**: Successfully verified via build and smoke tests.

---

## Overall Status
* **Status**: **PASS**
* **Final Test Output**: `Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 6 ms - WikiApi.Tests.dll (net10.0)`

---

## Manual Verification

Start the WikiApi application locally:
```bash
WIKI_ADMIN_TOKEN="my-secure-token" dotnet run --project src/WikiApi --no-launch-profile --urls http://localhost:5099
```

### 1. Verify Case-insensitive search (Bug #1)
Run a search querying `vpn` (lowercase):
```bash
curl -s "http://localhost:5099/documents/search?q=vpn"
```
*Expected response*: A list containing the "VPN Setup" page, verifying that the search query `vpn` correctly matches the title "VPN Setup" and the tag "VPN".

### 2. Verify Update timestamps (Bug #2)
1. Fetch all documents to obtain a document's ID (e.g. for "Onboarding Guide"):
   ```bash
   curl -s "http://localhost:5099/documents"
   ```
   Note the `id`, `createdAt`, and `updatedAt` timestamps.

2. Perform a PUT request updating that document:
   ```bash
   curl -s -X PUT -H "Content-Type: application/json" \
     -d '{"title": "Onboarding Guide V2", "content": "Updated content.", "author": "hr-team"}' \
     "http://localhost:5099/documents/<id-from-step-1>"
   ```
   Check the response. The `createdAt` must match the original creation date, and `updatedAt` must be updated to the current time.

### 3. Verify Admin token security (SEC-1)
1. Try deleting a document with no token header (should fail):
   ```bash
   curl -i -X DELETE "http://localhost:5099/documents/<id>"
   ```
   *Expected response*: `HTTP/1.1 401 Unauthorized`

2. Try deleting a document with the old hardcoded token header (should fail):
   ```bash
   curl -i -X DELETE -H "X-Admin-Token: admin-secret-123" "http://localhost:5099/documents/<id>"
   ```
   *Expected response*: `HTTP/1.1 401 Unauthorized`

3. Try deleting a document with the correct token configured (should succeed):
   ```bash
   curl -i -X DELETE -H "X-Admin-Token: my-secure-token" "http://localhost:5099/documents/<id>"
   ```
   *Expected response*: `HTTP/1.1 204 NoContent` (or `HTTP/1.1 404 NotFound` if the ID is dummy/non-existent, but not `401 Unauthorized`).

### 4. Verify Input validation (SEC-2)
1. POST a document with an empty/whitespace title (should fail):
   ```bash
   curl -i -X POST -H "Content-Type: application/json" \
     -d '{"title": "   ", "content": "Body", "author": "me"}' \
     "http://localhost:5099/documents"
   ```
   *Expected response*: `HTTP/1.1 400 BadRequest` ("Title cannot be empty or whitespace.")

2. POST a document with a title longer than 100 characters (should fail):
   ```bash
   curl -i -X POST -H "Content-Type: application/json" \
     -d "{\"title\": \"$(printf 'a%.0s' {1..101})\", \"content\": \"Body\", \"author\": \"me\"}" \
     "http://localhost:5099/documents"
   ```
   *Expected response*: `HTTP/1.1 400 BadRequest` ("Title cannot exceed 100 characters.")

3. POST a document with content longer than 10000 characters (should fail):
   ```bash
   curl -i -X POST -H "Content-Type: application/json" \
     -d "{\"title\": \"Valid Title\", \"content\": \"$(printf 'a%.0s' {1..10001})\", \"author\": \"me\"}" \
     "http://localhost:5099/documents"
   ```
   *Expected response*: `HTTP/1.1 400 BadRequest` ("Content cannot exceed 10000 characters.")

---

## References
* [Implementation Plan](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/context/bugs/001/implementation-plan.md)
* [DocumentStore.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Services/DocumentStore.cs)
* [AdminTokenValidator.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Auth/AdminTokenValidator.cs)
* [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs)
