# Security Review Report

## Summary

### Overall Posture
The security posture of the modified application has improved significantly through the remediation of the seeded security vulnerabilities (**SEC-1** and **SEC-2**). The hardcoded administrative credential has been moved to external configuration and a timing-safe byte comparison was introduced. Basic length and validation checks were implemented for primary request properties (Title and Content) on creation and modification endpoints.

However, several residual security risks remain open. A **Medium** severity vulnerability exists due to concurrent shared-state mutations in the in-memory `DocumentStore` (thread safety/race condition). Additionally, **Low** and **Info** severity findings are identified around missing input validation for additional fields (`Author`, `Tags`) and lack of output/input sanitization for XSS mitigation.

### Vulnerability Counts by Severity

| Severity | Open | Resolved | Total |
| :--- | :---: | :---: | :---: |
| **Critical** | 0 | 1 | 1 |
| **High** | 0 | 1 | 1 |
| **Medium** | 1 | 0 | 1 |
| **Low** | 2 | 0 | 2 |
| **Info** | 1 | 0 | 1 |
| **Total** | **4** | **2** | **6** |

---

## Findings

### Finding 1: Seeded Issue SEC-1 - Hardcoded Admin Secret + Timing-Unsafe Comparison
* **Severity**: Critical (previously) -> **Resolved** (now)
* **Location**: [AdminTokenValidator.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Auth/AdminTokenValidator.cs#L19-L30) and [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs#L9)
* **Description**:
  The application previously defined a hardcoded administrative token (`private const string AdminToken = "admin-secret-123"`) and checked it using standard string equality (`==`). This combination exposed the administrative credential in the repository/binaries and allowed timing side-channel attacks due to the short-circuiting nature of standard string equality.
* **Remediation**:
  The secret is now loaded dynamically from application configuration (`builder.Configuration["WIKI_ADMIN_TOKEN"]`). In addition, comparisons are performed using a timing-safe operation:
  `CryptographicOperations.FixedTimeEquals(providedBytes, adminBytes)`
  which prevents timing leakage.
* **Status**: **Resolved**

### Finding 2: Seeded Issue SEC-2 - Missing Input Validation on Document Create/Update
* **Severity**: High (previously) -> **Resolved** (now)
* **Location**: [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs#L32-L43) (POST `/documents`) and [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs#L52-L63) (PUT `/documents/{id:guid}`)
* **Description**:
  The application allowed creation and updates of documents without verifying that titles were provided or restricting the maximum payload sizes for the document Title and Content, potentially enabling database spam or denial of service through out-of-memory errors.
* **Remediation**:
  Validations have been added to restrict `Title` to be non-empty/non-whitespace and at most 100 characters, and limit `Content` to at most 10,000 characters.
* **Status**: **Resolved**

### Finding 3: Thread-Safety and Race Conditions on Concurrent Document Updates
* **Severity**: **Medium**
* **Location**: [DocumentStore.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Services/DocumentStore.cs#L44-L61)
* **Description**:
  The `Update` method retrieves the reference of a `Document` object using `_documents.TryGetValue(id, out var existing)` and directly mutates its fields in place without any synchronization or locking mechanism. Because `DocumentStore` is registered as a singleton, concurrent updates or concurrent read operations (via `GetAll` or `GetById`) on the same document object from multiple HTTP requests can result in race conditions. For example, a reader might see a partially updated object, or one write could overwrite another write partially, leading to inconsistent state or corruption.
* **Remediation**:
  Use an immutable data structure approach, or clone/replace the document atomically. Since `_documents` is a `ConcurrentDictionary<Guid, Document>`, the `Document` object properties can be made read-only (or initialized via `init` properties), and updates should construct a new `Document` instance and perform a thread-safe replacement (e.g. using `_documents.TryUpdate(id, newDoc, existingDoc)` or a lock statement).
  Example:
  ```csharp
  // Clone and swap:
  var updatedDoc = new Document
  {
      Id = existing.Id,
      Title = request.Title,
      Slug = Slugify(request.Title),
      Content = request.Content,
      Author = existing.Author,
      CreatedAt = existing.CreatedAt,
      UpdatedAt = _timeProvider.GetUtcNow(),
      Tags = request.Tags ?? existing.Tags
  };
  _documents[id] = updatedDoc; // ConcurrentDictionary handles thread-safe dictionary assignment.
  ```
* **Status**: **Open**

### Finding 4: Missing Validation on `Author` Field in Create Request
* **Severity**: **Low**
* **Location**: [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs#L30)
* **Description**:
  The `Author` field in `CreateDocumentRequest` has no input validation. A client can supply an empty or whitespace string, or an extremely long string (leading to unnecessary memory usage or buffer allocations), or a string with malicious characters.
* **Remediation**:
  Add validation to the POST endpoint in `Program.cs` to ensure that `request.Author` is not null or whitespace and does not exceed a reasonable length limit (e.g., 100 characters):
  ```csharp
  if (string.IsNullOrWhiteSpace(request.Author))
  {
      return Results.BadRequest("Author cannot be empty or whitespace.");
  }
  if (request.Author.Length > 100)
  {
      return Results.BadRequest("Author cannot exceed 100 characters.");
  }
  ```
* **Status**: **Open**

### Finding 5: Missing Validation on `Tags` Collection in Create and Update Requests
* **Severity**: **Low**
* **Location**: [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs#L30) and [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs#L50)
* **Description**:
  The `Tags` collection is not validated. A client can send an array containing an excessive number of tags, or tags with extremely long lengths, or duplicate tags. Since the application stores these in-memory, an attacker could consume significant memory resources by posting documents with millions of tags, potentially leading to an Out of Memory (OOM) Denial of Service (DoS) vulnerability.
* **Remediation**:
  Enforce limits on both the number of tags and the length of each tag in the POST and PUT endpoints. For example:
  ```csharp
  if (request.Tags != null)
  {
      if (request.Tags.Count > 10)
      {
          return Results.BadRequest("A document cannot have more than 10 tags.");
      }
      if (request.Tags.Any(t => string.IsNullOrWhiteSpace(t) || t.Length > 30))
      {
          return Results.BadRequest("Tags must be non-empty and cannot exceed 30 characters.");
      }
  }
  ```
* **Status**: **Open**

### Finding 6: Missing HTML Sanitization / XSS Mitigation
* **Severity**: **Info**
* **Location**: [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs#L30) and [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs#L50)
* **Description**:
  The API stores and retrieves raw text (including `Title` and `Content`) without any validation or sanitization for HTML tags or script injection (Stored XSS). If the API is consumed by a web-based frontend that does not properly sanitize or escape content before rendering it, an attacker could inject malicious scripts.
* **Remediation**:
  Ensure consuming front-end applications display content using safe rendering bindings (e.g., escaping all text dynamically), or apply HTML sanitization (e.g., using `Ganss.XSS.HtmlSanitizer` or a similar package) on input on the API side if rich-text HTML rendering is desired.
* **Status**: **Open**

---

## Seeded Issue Verification

### SEC-1: Hardcoded admin secret + timing-unsafe `==` comparison
* **Resolved?** Yes.
* **Evidence**:
  * **Configuration Source Injection**: The hardcoded token `admin-secret-123` was removed from the codebase. The token is now dynamically loaded in [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs#L9):
    `builder.Services.AddSingleton(new AdminTokenValidator(builder.Configuration["WIKI_ADMIN_TOKEN"] ?? string.Empty));`
  * **Timing-Safe Equality Check**: The timing-unsafe string comparison operator (`==`) was replaced with a constant-time comparison in [AdminTokenValidator.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Auth/AdminTokenValidator.cs#L26-L29):
    ```csharp
    var providedBytes = Encoding.UTF8.GetBytes(providedToken);
    var adminBytes = Encoding.UTF8.GetBytes(_adminToken);

    return CryptographicOperations.FixedTimeEquals(providedBytes, adminBytes);
    ```
    This successfully prevents side-channel timing attacks from leaking character-by-character matches of the secret.

### SEC-2: Missing input validation on create/update
* **Resolved?** Yes.
* **Evidence**:
  * **POST Validation (Create)**: In [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs#L32-L43), validation logic was added to verify `Title` and `Content` properties:
    ```csharp
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
    ```
  * **PUT Validation (Update)**: Identical validation was introduced in [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs#L52-L63) for document update requests.
  * This successfully restricts the payload size and checks for non-null/non-empty document titles, eliminating the main vector for unbounded input or blank records.

---

## References
* [fix-summary.md](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/context/bugs/001/fix-summary.md)
* [bug-context.md](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/context/bugs/001/bug-context.md)
* [Program.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Program.cs)
* [AdminTokenValidator.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Auth/AdminTokenValidator.cs)
* [DocumentStore.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Services/DocumentStore.cs)
* [Document.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Models/Document.cs)
* [DocumentDtos.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Models/DocumentDtos.cs)
