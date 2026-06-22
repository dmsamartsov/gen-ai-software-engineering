# Test Report — Batch 001

This report documents the unit tests added for the fixes implemented in Batch 001. All tests strictly adhere to the FIRST principles.

## Tests Added

### 1. [DocumentStoreTests.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/tests/WikiApi.Tests/DocumentStoreTests.cs)

* **`Search_TitleMatchIsCaseInsensitive_ReturnsMatchingDocument`** (Theory)
  * *Covers*: Bug #1 — Case-insensitive search on document titles. Verifies that querying with various casing styles (`onboarding`, `ONBOARDING`, `OnBoArDiNg`) successfully retrieves the seeded "Onboarding Guide".
* **`Search_ContentMatchIsCaseInsensitive_ReturnsMatchingDocument`** (Theory)
  * *Covers*: Bug #1 — Case-insensitive search on document content. Verifies that querying with different casings (`welcome`, `WELCOME`, `WeLcOmE`) correctly matches content in "Onboarding Guide".
* **`Search_TagMatchIsCaseInsensitive_ReturnsMatchingDocument`** (Theory)
  * *Covers*: Bug #1 — Case-insensitive search on document tags. Verifies that querying with different casings (`hr`, `HR`, `hR`) matches the "HR" tag on the "Onboarding Guide".
* **`Update_ExistingDocument_PreservesOriginalCreatedAt`** (Fact)
  * *Covers*: Bug #2 — Timestamp corruption. Asserts that when a document is updated, its original `CreatedAt` timestamp is strictly preserved.
* **`Update_ExistingDocument_AdvancesUpdatedAtToNow`** (Fact)
  * *Covers*: Bug #2 — Timestamp updates. Asserts that updating a document correctly advances its `UpdatedAt` timestamp to the simulated "now" using a mock time provider.
* **`Update_NonExistentDocument_ReturnsNull`** (Fact)
  * *Covers*: Edge case for the `Update` API. Verifies that updating a document that does not exist in the store returns `null`.

### 2. [AdminTokenValidatorTests.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/tests/WikiApi.Tests/AdminTokenValidatorTests.cs)

* **`IsValid_WrongOrEmptyToken_ReturnsFalse`** (Theory)
  * *Covers*: SEC-1 — Access control verification. Verifies that wrong, empty, or null tokens are rejected and return `false`.
* **`IsValid_ConfiguredToken_ReturnsTrue`** (Theory)
  * *Covers*: SEC-1 — Sourcing the token from configuration. Verifies that the validator validates correctly against any admin token supplied dynamically to its constructor (proves it is not tied to a hardcoded constant).

---

## FIRST Mapping

* **F — Fast**
  * Plain service classes (`DocumentStore` and `AdminTokenValidator`) are tested directly. No web host/server startup is performed, and there is no file or network I/O. The entire test suite runs in under 20 milliseconds.
* **I — Independent**
  * Each test instantiates its own `DocumentStore` and `AdminTokenValidator` from scratch. There is zero reliance on shared static mutable state or test ordering.
* **R — Repeatable**
  * A `FakeTimeProvider` is injected into `DocumentStore` constructor, enabling explicit pinning and advancement of the system time (using `.SetUtcNow` and `.Advance`). This removes any dependency on the system clock, ensuring tests are deterministic and reproducible.
* **S — Self-validating**
  * Assertions are explicitly handled via xUnit's native methods (`Assert.Equal`, `Assert.Single`, `Assert.NotNull`, `Assert.Null`, `Assert.True`, `Assert.False`).
* **T — Timely**
  * Tests are added immediately following the fix implementation to cover the specific changed behavior, including both security-critical logic and regression edge cases.

---

## Result

Final execution of `dotnet test WikiApi.sln`:
```text
Passed!  - Failed:     0, Passed:    19, Skipped:     0, Total:    19, Duration: 20 ms - WikiApi.Tests.dll (net10.0)
```

---

## References

* [DocumentStore.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Services/DocumentStore.cs)
* [AdminTokenValidator.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/src/WikiApi/Auth/AdminTokenValidator.cs)
* [DocumentStoreTests.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/tests/WikiApi.Tests/DocumentStoreTests.cs)
* [AdminTokenValidatorTests.cs](file:///Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering/homework-4/tests/WikiApi.Tests/AdminTokenValidatorTests.cs)
