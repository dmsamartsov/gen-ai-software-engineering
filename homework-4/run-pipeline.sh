#!/usr/bin/env bash
#
# Single-command 4-agent bug-fix pipeline for the Internal Wiki API.
#
# Run order (per TASKS.md):
#   Bug Researcher -> Research Verifier -> Bug Planner -> Bug Fixer
#                  -> Security Verifier (changed code) -> Unit Test Generator (changed code)
#
# Each stage is a real headless `claude -p` run. The agent's *.agent.md body is injected
# as the system prompt; its model and allowed tools are parsed from the frontmatter (the
# script is the enforcement point, since --append-system-prompt injects text only).
# Skill files are appended to the prompt for the stages that use them, so skills load
# automatically with no manual step.
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

SLN="$ROOT/WikiApi.sln"
BUGDIR="$ROOT/context/bugs/001"
AGENTS="$ROOT/agents"
SKILLS="$ROOT/skills"
LOGDIR="$ROOT/.pipeline-logs"
mkdir -p "$BUGDIR/research" "$LOGDIR"

# ---------------------------------------------------------------- helpers --
frontmatter() {            # $1 = agent file, $2 = key  -> trimmed value
  grep -E "^$2:" "$1" | head -1 | cut -d: -f2- | tr -d ' '
}
tools_csv() {              # "tools: Read, Grep, Glob" -> "Read,Grep,Glob"
  grep -E '^tools:' "$1" | head -1 | cut -d: -f2- | tr -d ' '
}
banner() {
  echo
  echo "=================================================================="
  echo ">>> $1"
  echo "=================================================================="
}
build_sys() {              # $1 = agent file, $2 = optional skill file  -> stdout
  cat "$1"
  if [ -n "${2:-}" ]; then
    printf '\n\n# ===== Appended skill: %s =====\n' "$(basename "$2")"
    cat "$2"
  fi
}

command -v claude  >/dev/null 2>&1 || { echo "ERROR: 'claude' CLI not found on PATH." >&2; exit 1; }
command -v dotnet  >/dev/null 2>&1 || { echo "ERROR: 'dotnet' SDK not found on PATH."  >&2; exit 1; }

# report-only stage: the agent's final message (stdout) becomes the artifact ----
run_report_stage() {       # $1 agent  $2 prompt  $3 outfile  [$4 skill]
  local agent="$1" prompt="$2" outfile="$3" skill="${4:-}"
  local model tools
  model="$(frontmatter "$agent" model)"
  tools="$(tools_csv "$agent")"
  echo "  agent=$(basename "$agent")  model=$model  tools=$tools  ->  ${outfile#$ROOT/}"
  claude -p "$prompt" \
    --model "$model" \
    --append-system-prompt "$(build_sys "$agent" "$skill")" \
    --allowedTools "$tools" \
    --permission-mode default \
    --add-dir "$ROOT" \
    --output-format text \
    > "$outfile"
}

# editing stage: the agent edits source and writes its own artifact; stdout -> log --
run_edit_stage() {         # $1 agent  $2 prompt  $3 logfile  [$4 skill]
  local agent="$1" prompt="$2" logfile="$3" skill="${4:-}"
  local model tools
  model="$(frontmatter "$agent" model)"
  tools="$(tools_csv "$agent")"
  echo "  agent=$(basename "$agent")  model=$model  tools=$tools  (edits source)  ->  log ${logfile#$ROOT/}"
  claude -p "$prompt" \
    --model "$model" \
    --append-system-prompt "$(build_sys "$agent" "$skill")" \
    --allowedTools "$tools" \
    --permission-mode bypassPermissions \
    --add-dir "$ROOT" \
    --output-format text \
    | tee "$logfile"
}

# --------------------------------------------------------------- pipeline --
banner "Stage 0 — Preflight: build the (buggy) baseline"
dotnet build "$SLN"

banner "Stage 1 — Bug Researcher"
run_report_stage "$AGENTS/bug-researcher.agent.md" \
  "Research the seeded defects described in context/bugs/001/bug-context.md against the source in src/WikiApi. Produce the codebase-research.md document exactly as your agent instructions specify." \
  "$BUGDIR/research/codebase-research.md"

banner "Stage 2 — Research Verifier (research-quality-measurement skill)"
run_report_stage "$AGENTS/research-verifier.agent.md" \
  "Verify every reference and snippet in context/bugs/001/research/codebase-research.md against the source. Produce verified-research.md using the appended Research Quality Measurement skill." \
  "$BUGDIR/research/verified-research.md" \
  "$SKILLS/research-quality-measurement.md"

banner "Stage 3 — Bug Planner"
run_report_stage "$AGENTS/bug-planner.agent.md" \
  "Using context/bugs/001/research/verified-research.md and bug-context.md, produce implementation-plan.md that fixes Bug #1, Bug #2, SEC-1 and SEC-2 with before/after snippets." \
  "$BUGDIR/implementation-plan.md"

banner "Stage 4 — Bug Fixer (applies plan, runs tests)"
run_edit_stage "$AGENTS/bug-fixer.agent.md" \
  "Apply context/bugs/001/implementation-plan.md to src/WikiApi. Run 'dotnet build WikiApi.sln' then 'dotnet test WikiApi.sln'. Write context/bugs/001/fix-summary.md." \
  "$LOGDIR/bug-fixer.log"

banner "Stage 5 — Security Verifier (report only, on changed code)"
run_report_stage "$AGENTS/security-verifier.agent.md" \
  "Review the changed files referenced by context/bugs/001/fix-summary.md for vulnerabilities, confirm SEC-1 and SEC-2 are resolved, and produce security-report.md. Do not edit any code." \
  "$BUGDIR/security-report.md"

banner "Stage 6 — Unit Test Generator (unit-tests-FIRST skill, runs tests)"
run_edit_stage "$AGENTS/unit-test-generator.agent.md" \
  "Generate FIRST-compliant xUnit tests for the changed code into tests/WikiApi.Tests, run 'dotnet test WikiApi.sln' until the suite is green, and write context/bugs/001/test-report.md." \
  "$LOGDIR/unit-test-generator.log" \
  "$SKILLS/unit-tests-FIRST.md"

banner "Stage 7 — Postflight: full test run"
dotnet test "$SLN"

banner "Pipeline complete"
echo "Artifacts:"
ls -1 "$BUGDIR" "$BUGDIR/research" | sed 's/^/  /'
