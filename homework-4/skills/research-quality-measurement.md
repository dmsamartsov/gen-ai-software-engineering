# Skill: Research Quality Measurement

A rubric the **research-verifier** applies when validating a Bug Researcher report and
writing `verified-research.md`. It turns "the research looks fine" into a defensible,
repeatable grade.

## How to apply

1. Enumerate every **checkable claim** in `codebase-research.md`. A claim is checkable
   if it asserts a `file:line` reference or quotes a source snippet.
2. For each claim, open the cited file and verify:
   - the file exists and the line number points at the referenced code, **and**
   - any quoted snippet matches the source **verbatim** (ignoring leading/trailing
     whitespace only).
3. Compute `verifiedRatio = verifiedClaims / totalCheckableClaims`.
4. Count **blocking discrepancies** — a wrong file, a wrong line off by more than ±2, a
   misquoted snippet, or a claimed bug that does not exist in the code.
5. Assign exactly one quality level from the table below and record the reasoning.

## Quality levels (highest → lowest)

| Level | Label | Criteria |
|------:|-------|----------|
| L4 | **Verified** | `verifiedRatio == 1.0` and **0** blocking discrepancies. Every reference and snippet is exact. Safe for the planner to act on as-is. |
| L3 | **Mostly Verified** | `verifiedRatio >= 0.85` and **0** blocking discrepancies (only cosmetic issues such as ±1–2 line drift or whitespace). Planner may proceed; note the minor drift. |
| L2 | **Partially Verified** | `verifiedRatio >= 0.6`, or **1** blocking discrepancy. Usable but the planner must re-confirm the flagged items before changing code. |
| L1 | **Unverified** | `verifiedRatio < 0.6`, or **2+** blocking discrepancies, or any fabricated reference. Send back to the researcher; do **not** plan from it. |

## Verdict mapping

- **PASS** → level is **Verified** or **Mostly Verified**.
- **FAIL** → level is **Partially Verified** or **Unverified**.

## Required output sections in `verified-research.md`

The research-verifier MUST produce these sections, in order:

1. **Verification Summary** — overall PASS/FAIL and the **Research Quality** level
   (L4–L1 + label) per this skill, plus `verifiedRatio` (e.g. `7/7`).
2. **Verified Claims** — table of each checked reference: claim, `file:line`, snippet
   match (✓/✗), notes.
3. **Discrepancies Found** — every blocking and cosmetic discrepancy, or "None".
4. **Research Quality Assessment** — the chosen level, the ratio/discrepancy counts that
   justify it, and one or two sentences of reasoning.
5. **References** — the files inspected (path + the lines checked).
