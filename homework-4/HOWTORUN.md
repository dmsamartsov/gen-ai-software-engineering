# HOWTORUN — Homework 4

## Prerequisites

- **.NET SDK 10** — verify with `dotnet --version` (any `10.x`).
- **Claude Code CLI** — `claude --version` (used by the pipeline). Only needed for
  *reproduce mode* below; *inspect mode* needs only .NET.

All commands below are run from the `homework-4/` directory.

---

## Run the app

```bash
dotnet run --project src/WikiApi --no-launch-profile --urls http://localhost:5099
```

Then, in another terminal:

```bash
# list
curl -s http://localhost:5099/documents

# search (after the fix, case-insensitive)
curl -s "http://localhost:5099/documents/search?q=vpn"

# create
curl -s -X POST http://localhost:5099/documents \
  -H 'Content-Type: application/json' \
  -d '{"title":"New Page","content":"hello","author":"me","tags":["Guide"]}'

# delete (privileged — admin token)
curl -i -X DELETE http://localhost:5099/documents/<id> -H "X-Admin-Token: <token>"
```

### Admin token

- **Buggy baseline:** the token is hardcoded as `admin-secret-123` (the seeded SEC-1
  vulnerability).
- **After the fix:** the token comes from the `WIKI_ADMIN_TOKEN` environment variable:

  ```bash
  WIKI_ADMIN_TOKEN="choose-a-strong-token" \
    dotnet run --project src/WikiApi --no-launch-profile --urls http://localhost:5099
  ```

---

## Run the tests

```bash
dotnet test WikiApi.sln
```

---

## Run the 4-agent pipeline (single command)

```bash
./run-pipeline.sh
```

This runs all stages in order (Researcher → Verifier → Planner → Fixer → Security
Verifier → Test Generator), parsing each agent's model/tools from its `*.agent.md`
frontmatter and loading the relevant skill automatically. Outputs land in
`context/bugs/001/` (and per-stage logs in `.pipeline-logs/`).

> The pipeline runs **real** `claude -p` stages, so it consumes Claude API tokens.
> Editing stages (Bug Fixer, Unit Test Generator) run with
> `--permission-mode bypassPermissions` so they can edit files and run `dotnet`
> non-interactively; read-only stages are restricted to `Read,Grep,Glob`.

---

## Two ways to evaluate

### Inspect mode (no Claude tokens)
The repository already contains the fixed `src/`, the generated `tests/`, and all agent
outputs in `context/bugs/001/`. To verify without running any agent:

```bash
dotnet test WikiApi.sln      # green
dotnet run --project src/WikiApi --no-launch-profile --urls http://localhost:5099
curl -s "http://localhost:5099/documents/search?q=vpn"   # now finds "VPN Setup"
```

### Reproduce mode (spends tokens)
Reset the app to the seeded-buggy baseline, then re-run the pipeline:

```bash
git checkout before-pipeline -- src/ tests/
./run-pipeline.sh
```

The `before-pipeline` git tag marks the committed buggy baseline; the difference between
that tag and the post-pipeline commit is the before/after evidence (also captured in
`docs/screenshots/`).
