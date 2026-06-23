---
name: write-spec
description: Generates a technical specification for the banking pipeline.
---

# Write Specification Skill

When the user types `/write-spec`, you must generate a comprehensive technical specification following the template below and save it as `specification.md` in the current working directory.

## Steps to Execute:

1. Create a new file named `specification.md`.
2. Populate the file with the following template structure:

```markdown
# [Project Name] Specification

> Ingest the information from this file, implement the Low-Level Tasks, and generate the code that will satisfy the High and Mid-Level Objectives.

## High-Level Objective
- [Clear description]

## Mid-Level Objectives
- [Objective 1]
- [Objective 2]

## Implementation Notes
- [Requirement 1]
- [Requirement 2]

## Context
### Beginning state
- [State]
### Ending state
- [State]

## Low-Level Tasks
### Task 1
What prompt would you run to complete this task?
[Prompt]

What file do you want to CREATE or UPDATE?
[File]

What function do you want to CREATE or UPDATE?
[Function]

What are details you want to add to drive the code changes?
[Details]
```

3. Automatically fill in the details for a .NET 10 Minimal API Multi-Agent Banking Pipeline.
4. Output a summary message: "Created `specification.md` based on the Banking template."
