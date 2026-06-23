# Multi-Agent Banking Pipeline Specification

> Ingest the information from this file, implement the Low-Level Tasks, and generate the code that will satisfy the High and Mid-Level Objectives.

## High-Level Objective
- Build a robust, file-based multi-agent transaction processing pipeline using .NET 10 Minimal APIs and Background Services.

## Mid-Level Objectives
- Transactions above $10,000 are flagged for fraud review with a risk score.
- Rejected transactions are written to `shared/results/` with a reason field.
- All agent operations are logged with ISO 8601 timestamps.
- Implement at least 3 agent components (Integrator, Transaction Validator, Fraud Detector, Settlement Processor) using .NET 10 Minimal APIs.

## Implementation Notes
- Monetary values: use precise decimal/numeric types for amounts (`decimal` in C# — never `float` or `double`).
- Currency codes: ISO 4217 (USD, EUR, GBP, JPY…).
- Logging: audit trail with timestamp, agent name, transaction ID, and outcome.
- PII: treat account numbers and names as sensitive — no plaintext logging.
- Agent Communication: File-based JSON protocol passing through `shared/input`, `shared/processing`, `shared/output`, and `shared/results`.

## Context

### Beginning state
- A `sample-transactions.json` file with raw transaction records.
- Shared directories setup for file passing.

### Ending state
- Processed results in `shared/results/`.
- A pipeline summary report.
- Test coverage ≥ 80% (ideally ≥ 90%).
- .NET 10 solution and projects.

## Low-Level Tasks

### Task 1: Agent 1 - Integrator Setup
What prompt would you run to complete this task?
"Create a .NET 10 console app named Integrator. It should read `sample-transactions.json` and split it into individual JSON files placed in `shared/input/`."

What file do you want to CREATE or UPDATE?
`src/Integrator/Program.cs`

What function do you want to CREATE or UPDATE?
`Main` (Top-level statements)

What are details you want to add to drive the code changes?
Use `System.Text.Json` and `System.IO`. Create directories if they do not exist.

### Task 2: Agent 2 - Transaction Validator
What prompt would you run to complete this task?
"Create a .NET 10 Minimal API with a BackgroundService that watches `shared/input/`. It should validate the transaction (ISO 4217, positive `decimal` amount). Valid transactions go to `shared/output/`, invalid to `shared/results/`."

What file do you want to CREATE or UPDATE?
`src/Validator/Program.cs` and `src/Validator/ValidatorWorker.cs`

What function do you want to CREATE or UPDATE?
`ExecuteAsync`

What are details you want to add to drive the code changes?
Use `FileSystemWatcher` or periodic polling. Ensure thread-safe file handling.

### Task 3: Agent 3 - Fraud Detector
What prompt would you run to complete this task?
"Create a .NET 10 Minimal API with a BackgroundService watching `shared/output/`. Assign a risk score (e.g., amount > 10,000 is high risk). Move processed files to a final staging directory or next step."

What file do you want to CREATE or UPDATE?
`src/FraudDetector/Program.cs` and `src/FraudDetector/FraudWorker.cs`

What function do you want to CREATE or UPDATE?
`ExecuteAsync`

What are details you want to add to drive the code changes?
Add logging and basic risk score computation.

### Task 4: Agent 4 - Settlement Processor
What prompt would you run to complete this task?
"Create a .NET 10 Minimal API with a BackgroundService that finalizes transactions and writes them to `shared/results/`."

What file do you want to CREATE or UPDATE?
`src/Settlement/Program.cs` and `src/Settlement/SettlementWorker.cs`

What function do you want to CREATE or UPDATE?
`ExecuteAsync`

What are details you want to add to drive the code changes?
Finalize the transaction status (approved/settled) and log the outcome.
