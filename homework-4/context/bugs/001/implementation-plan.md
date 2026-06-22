# Implementation Plan — Batch 001

This plan details the code changes needed to remediate the four seeded defects (Bug #1, Bug #2, SEC-1, and SEC-2) in the WikiApi application.

All paths in this document are relative to the repository root.

## Defect 1: Bug #1 — Case-sensitive search
- **File**: `src/WikiApi/Services/DocumentStore.cs`
- **Description**: Modify `Search` method to execute a case-insensitive search across Title, Content, and Tags using `StringComparison.OrdinalIgnoreCase`.

### Before
```csharp
        return _documents.Values
            .Where(d => d.Title.Contains(query)
                        || d.Content.Contains(query)
                        || d.Tags.Any(t => t.Contains(query)))
            .OrderBy(d => d.Title)
            .ToList();
```

### After
```csharp
        return _documents.Values
            .Where(d => d.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || d.Content.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || d.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(d => d.Title)
            .ToList();
```

---

## Defect 2: Bug #2 — Update corrupts timestamps
- **File**: `src/WikiApi/Services/DocumentStore.cs`
- **Description**: Update the `Update` method to preserve the original `CreatedAt` timestamp and set the `UpdatedAt` timestamp to the current time.

### Before
```csharp
        // BUG #2: an update stamps "now" onto CreatedAt (destroying the original
        // creation date) and never advances UpdatedAt. The two timestamps end up
        // inverted: CreatedAt moves forward on every edit while UpdatedAt is frozen.
        existing.CreatedAt = _timeProvider.GetUtcNow();

        return existing;
```

### After
```csharp
        // BUG #2: an update stamps "now" onto CreatedAt (destroying the original
        // creation date) and never advances UpdatedAt. The two timestamps end up
        // inverted: CreatedAt moves forward on every edit while UpdatedAt is frozen.
        existing.UpdatedAt = _timeProvider.GetUtcNow();

        return existing;
```

---

## Defect 3: SEC-1 — Hardcoded admin secret + timing-unsafe comparison
- **Files**: `src/WikiApi/Auth/AdminTokenValidator.cs` and `src/WikiApi/Program.cs`
- **Description**:
  1. Make `AdminTokenValidator` receive the expected token through its constructor.
  2. Implement constant-time comparison using `CryptographicOperations.FixedTimeEquals` over UTF-8 bytes.
  3. Register `AdminTokenValidator` in Dependency Injection inside `Program.cs` by retrieving `WIKI_ADMIN_TOKEN` from Configuration.

### AdminTokenValidator.cs

#### Before
```csharp
namespace WikiApi.Auth;

/// <summary>
/// Authorizes privileged operations (e.g. deleting a wiki document) by checking the
/// <c>X-Admin-Token</c> request header.
/// </summary>
public class AdminTokenValidator
{
    // SECURITY ISSUE: the admin secret is hardcoded directly in source control. Anyone
    // with read access to the repository (or the compiled binary) learns the token, and
    // it cannot be rotated without a code change + redeploy. It should be supplied via
    // configuration / environment (e.g. WIKI_ADMIN_TOKEN) instead.
    private const string AdminToken = "admin-secret-123";

    public bool IsValid(string? providedToken)
    {
        if (string.IsNullOrEmpty(providedToken))
        {
            return false;
        }

        // SECURITY ISSUE: the '==' string comparison short-circuits on the first
        // mismatching character, so the time it takes leaks how many leading characters
        // were correct (a timing side channel). Secret comparisons should be constant
        // time, e.g. CryptographicOperations.FixedTimeEquals over the UTF-8 bytes.
        return providedToken == AdminToken;
    }
}
```

#### After
```csharp
using System.Security.Cryptography;
using System.Text;

namespace WikiApi.Auth;

/// <summary>
/// Authorizes privileged operations (e.g. deleting a wiki document) by checking the
/// <c>X-Admin-Token</c> request header.
/// </summary>
public class AdminTokenValidator
{
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
}
```

### Program.cs

#### Before
```csharp
builder.Services.AddSingleton<AdminTokenValidator>();
```

#### After
```csharp
builder.Services.AddSingleton(new AdminTokenValidator(builder.Configuration["WIKI_ADMIN_TOKEN"] ?? string.Empty));
```

---

## Defect 4: SEC-2 — Missing input validation
- **File**: `src/WikiApi/Program.cs`
- **Description**: Add input validation for POST (`/documents`) and PUT (`/documents/{id}`) endpoints. Reject empty/whitespace Title, and cap Title and Content length (e.g. Title <= 100 characters, Content <= 10000 characters). Return a 400 Bad Request if validation fails.

### POST Endpoint

#### Before
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

#### After
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

### PUT Endpoint

#### Before
```csharp
// Update an existing document.
docs.MapPut("/{id:guid}", (Guid id, UpdateDocumentRequest request, IDocumentStore store) =>
{
    var updated = store.Update(id, request);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});
```

#### After
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

---

## Test Verification
Run the existing and newly generated unit test suite with the following command:

```bash
dotnet test WikiApi.sln
```
