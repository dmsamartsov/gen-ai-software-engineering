# Skill: Unit Tests — FIRST

The **unit-test-generator** MUST follow these FIRST principles when generating xUnit
tests for the changed code. FIRST = Fast, Independent, Repeatable, Self-validating,
Timely.

## The principles (and how to satisfy each here)

- **F — Fast.** Test the plain service classes (`DocumentStore`, `AdminTokenValidator`)
  directly. Do **not** spin up the web host or do I/O for unit tests. Each test should
  run in milliseconds. (A small number of `WebApplicationFactory` integration tests are
  allowed but kept separate and minimal.)
- **I — Independent.** Each test constructs its **own** `new DocumentStore(...)`. No
  shared mutable static state, no ordering dependencies, no relying on another test's
  side effects. Tests may run in any order or in parallel.
- **R — Repeatable.** No wall-clock, no randomness leaking into assertions. Inject a
  `FakeTimeProvider` (`Microsoft.Extensions.Time.Testing`) so timestamp behaviour
  (Bug #2: `CreatedAt`/`UpdatedAt`) is deterministic. Pin time explicitly; advance it
  with `SetUtcNow`/`Advance`.
- **S — Self-validating.** Assert with explicit xUnit assertions (`Assert.Equal`,
  `Assert.True`, `Assert.Empty`/`Assert.Single`). A test passes or fails on its own — no
  console output to eyeball, no manual inspection.
- **T — Timely.** Generate tests for the **changed code only**, right after the fix.
  Cover the behaviour the fix introduced (and its edge cases), not unrelated code.

## Required structure

- Arrange / Act / Assert, one logical assertion target per test.
- Descriptive names: `Method_StateUnderTest_ExpectedBehaviour`
  (e.g. `Search_IsCaseInsensitive_FindsUppercaseTitleFromLowercaseQuery`).
- Use `[Fact]` for single cases and `[Theory]`/`[InlineData]` for parameterised cases
  (e.g. several casings of the same query).

## Minimum test list to generate (changed code)

1. Search is case-insensitive for a Title match (Bug #1).
2. Search is case-insensitive for a Content match (Bug #1).
3. Search is case-insensitive for a Tag match (Bug #1).
4. Update **preserves** the original `CreatedAt` (Bug #2, `FakeTimeProvider`).
5. Update **advances** `UpdatedAt` to "now" (Bug #2, `FakeTimeProvider`).
6. Update of a missing id returns `null` (edge).
7. `AdminTokenValidator` returns **false** for a wrong/empty token (SEC-1).
8. `AdminTokenValidator` returns **true** for the configured token, proving the token is
   sourced from configuration rather than a hardcoded constant (SEC-1).

## Completion gate

After writing the tests, run `dotnet test WikiApi.sln` and **iterate until the suite
compiles and is green**. Record the final result in `test-report.md` with a section
mapping each test to the FIRST principle(s) it demonstrates.
