# 🤖 Homework 4 — 4-Agent Bug-Fix Pipeline

> **Student Name**: Dmytro Samartsov
> **Date Submitted**: 2026-06-22
> **AI Tools Used**: Claude Code (Anthropic) — Opus 4.8, Sonnet 4.6, Haiku 4.5

A single-command, **4-agent pipeline** (plus two supporting agents) that researches,
fixes, security-reviews, and test-covers seeded defects in a small **.NET 10 minimal-API
internal wiki**. Every stage is a real headless `claude -p` run with an explicit,
task-appropriate model.

---

## The pipeline

```mermaid
flowchart LR
  R["Bug Researcher<br/>(sonnet)"] --> V["Research Verifier<br/>(opus)"]
  V --> P["Bug Planner<br/>(sonnet)"]
  P --> F["Bug Fixer<br/>(sonnet)"]
  F --> S["Security Verifier<br/>(opus)"]
  F --> T["Unit Test Generator<br/>(haiku)"]
```

**Run order:** Bug Researcher → Research Verifier → Bug Planner → Bug Fixer → Security
Verifier (on changed code) → Unit Test Generator (on changed code).

Run it all with one command:

```bash
cd homework-4
./run-pipeline.sh
```

The four **required** agents are the Research Verifier, Bug Fixer, Security Verifier, and
Unit Test Generator (Tasks 1–4). Bug Researcher and Bug Planner are supporting roles that
make the pipeline self-contained and runnable end-to-end.

---

## Agents and model selection

Each agent is a `*.agent.md` file with YAML frontmatter (`name`, `description`, `model`,
`tools`). The orchestrator **parses the model and tools from the frontmatter** and passes
them to `claude -p` as `--model` / `--allowedTools` — because `--append-system-prompt`
injects the agent body as *text*, the frontmatter is documentation and the script is the
enforcement point.

| Agent | Model | Why this model |
|-------|-------|----------------|
| `bug-researcher` | **Sonnet 4.6** | Reading code and enumerating references is well within Sonnet; cheaper than Opus for a high-volume read pass. |
| `research-verifier` ⭐ | **Opus 4.8** | Fact-checking every `file:line` and snippet is precision-critical; the strongest reasoning model is justified. |
| `bug-planner` | **Sonnet 4.6** | Producing before/after diffs needs solid reasoning but not Opus-tier. |
| `bug-fixer` ⭐ | **Sonnet 4.6** | Mechanical, well-specified application of a plan with reliable tool use. |
| `security-verifier` ⭐ | **Opus 4.8** | Highest-stakes judgement — severity calibration and security reasoning on changed code. |
| `unit-test-generator` ⭐ | **Haiku 4.5** | Fast, routine xUnit scaffolding against a small, well-specified surface. |

> ⭐ = one of the four required agents. If Haiku-generated tests prove flaky on a given
> run, bump `unit-test-generator`'s `model:` to `claude-sonnet-4-6` and re-run — the
> orchestrator picks the model straight from the frontmatter.

**Model IDs used:** `claude-opus-4-8`, `claude-sonnet-4-6`, `claude-haiku-4-5-20251001`.

---

## Skills

Two skills are loaded **automatically** — the orchestrator appends the skill file to the
relevant agent's system prompt at run time, so there is no manual step:

- **`skills/research-quality-measurement.md`** (Task 1.2) — a rubric of research-quality
  levels (Verified / Mostly Verified / Partially Verified / Unverified) that the Research
  Verifier applies when writing `verified-research.md`.
- **`skills/unit-tests-FIRST.md`** (Task 4.2) — the **FIRST** principles (Fast,
  Independent, Repeatable, Self-validating, Timely) the Unit Test Generator follows.

---

## The sample application (`src/WikiApi`)

A .NET 10 minimal API for an internal wiki, backed by a thread-safe in-memory store
seeded with three pages (*Onboarding Guide*, *VPN Setup*, *Expense Policy*).

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/documents` | list all |
| GET | `/documents/search?q=` | search title/content/tags |
| GET | `/documents/{id}` | fetch one |
| POST | `/documents` | create |
| PUT | `/documents/{id}` | update |
| DELETE | `/documents/{id}` | delete (requires `X-Admin-Token` header) |

Business logic lives in plain classes (`Services/DocumentStore.cs`,
`Auth/AdminTokenValidator.cs`) so it is fast to unit-test and is exactly the "changed
code" the verifiers inspect. `Program.cs` handlers are thin.

### Seeded defects (the pipeline's work — see `context/bugs/001/bug-context.md`)

- **Bug #1 — case-sensitive search.** `Search` uses ordinal `Contains`, so `?q=vpn` never
  finds *VPN Setup*. → fix: `StringComparison.OrdinalIgnoreCase` across title/content/tags.
- **Bug #2 — update corrupts timestamps.** `Update` overwrites `CreatedAt` with "now" and
  never advances `UpdatedAt`. → fix: preserve `CreatedAt`, set `UpdatedAt = now`.
- **SEC-1 — hardcoded secret + timing-unsafe compare** in `AdminTokenValidator`. → fix:
  read `WIKI_ADMIN_TOKEN` from config; compare with `CryptographicOperations.FixedTimeEquals`.
- **SEC-2 — missing input validation** on create/update. → fix: reject empty `Title`, cap
  lengths, return `400`.

**Security review vs. "resolved after":** Task 3 makes the Security Verifier **report
only**. So the security remediation is scoped into the Planner → Fixer stages (the auth
file becomes *changed code*), and the Security Verifier then **confirms** SEC-1/SEC-2 are
resolved on the changed file and scans for residual issues — it verifies, never edits.

---

## Agent outputs (`context/bugs/001/`)

Produced by a real pipeline run and committed as evidence:

| File | Author agent |
|------|--------------|
| `research/codebase-research.md` | Bug Researcher |
| `research/verified-research.md` | Research Verifier (uses quality skill) |
| `implementation-plan.md` | Bug Planner |
| `fix-summary.md` | Bug Fixer |
| `security-report.md` | Security Verifier |
| `test-report.md` | Unit Test Generator (uses FIRST skill) |

---

## Project structure

```
homework-4/
├── README.md / HOWTORUN.md / run-pipeline.sh / WikiApi.sln / .gitignore
├── agents/         # 6 *.agent.md (4 required + 2 supporting)
├── skills/         # research-quality-measurement.md, unit-tests-FIRST.md
├── src/WikiApi/    # the .NET 10 minimal-API wiki (Models / Services / Auth / Program)
├── tests/WikiApi.Tests/   # xUnit (scaffold + generated tests)
├── context/bugs/001/      # bug-context.md (seed) + agent outputs
└── docs/screenshots/      # pipeline run, fixes, security scan, tests
```

See **[HOWTORUN.md](./HOWTORUN.md)** to run the app, the tests, and the pipeline.
