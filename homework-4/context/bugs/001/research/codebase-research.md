# Codebase Research — Batch 001

This document outlines the raw research findings for the seeded defects in the `WikiApi` codebase. All paths are relative to the repository root.

## Defect Findings

### Bug #1 — Case-sensitive search (Functional)
- **File**: `src/WikiApi/Services/DocumentStore.cs`
- **Lines**: 75-80
- **Root Cause**: The search query comparison utilizes the parameterless `string.Contains(query)` which defaults to a case-sensitive ordinal comparison, failing to match differently cased strings (e.g., query "vpn" does not match "VPN Setup").
- **Verbatim Snippet**:
```csharp
        return _documents.Values
            .Where(d => d.Title.Contains(query)
                        || d.Content.Contains(query)
                        || d.Tags.Any(t => t.Contains(query)))
            .OrderBy(d => d.Title)
            .ToList();
```

### Bug #2 — Update corrupts timestamps (Functional)
- **File**: `src/WikiApi/Services/DocumentStore.cs`
- **Lines**: 58-58
- **Root Cause**: During document updates, the store overwrites the original creation timestamp (`existing.CreatedAt`) with the current timestamp and leaves `UpdatedAt` unassigned.
- **Verbatim Snippet**:
```csharp
        existing.CreatedAt = _timeProvider.GetUtcNow();
```

### SEC-1 — Hardcoded admin secret + timing-unsafe comparison (Security)
- **File**: `src/WikiApi/Auth/AdminTokenValidator.cs`
- **Lines**: 13-13 and 26-26
- **Root Cause**: 
  1. The administrative bypass token is a hardcoded private constant string in the codebase.
  2. The string validation compares tokens using standard equality (`==`), which is timing-unsafe as it short-circuits.
- **Verbatim Snippet (Line 13)**:
```csharp
    private const string AdminToken = "admin-secret-123";
```
- **Verbatim Snippet (Line 26)**:
```csharp
        return providedToken == AdminToken;
```

### SEC-2 — Missing input validation (Security)
- **File**: `src/WikiApi/Program.cs`
- **Lines**: 30-36 (POST) and 39-43 (PUT)
- **Root Cause**: The endpoints for creating and updating documents map incoming request models directly to the store operations without verifying that the title is non-empty and non-whitespace, or that the title/content lengths do not exceed limits.
- **Verbatim Snippet (Lines 30-36)**:
```csharp
docs.MapPost("/", (CreateDocumentRequest request, IDocumentStore store) =>
{
    // NOTE: no input validation here — an empty Title or arbitrarily large Content is
    // accepted as-is (seeded weakness for the security review to flag).
    var created = store.Create(request);
    return Results.Created($"/documents/{created.Id}", created);
});
```
- **Verbatim Snippet (Lines 39-43)**:
```csharp
docs.MapPut("/{id:guid}", (Guid id, UpdateDocumentRequest request, IDocumentStore store) =>
{
    var updated = store.Update(id, request);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});
```
