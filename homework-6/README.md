# Multi-Agent Banking Pipeline

**Created by: Dmytro Samartsov**
**Date Submitted**: 23.06.2026
**AI Tools Used**: Antigravity CLI, Claude Code

## Overview
This system is an AI-powered transaction processing pipeline built to simulate core banking operations. It processes raw financial transactions through a series of automated, asynchronous "agents" that validate data, detect fraud based on risk parameters, and simulate transaction settlement. The pipeline operates entirely on a file-based JSON messaging protocol, passing files through shared staging directories.

## Agent Responsibilities
- **Agent 1 (Specification):** Defines system requirements, constructs the data contracts, and generates automated specifications.
- **Agent 2 (Code Generation/Pipeline):** Implements the `.NET 10 Minimal API` workflow encompassing the Integrator, Validator, Fraud Detector, and Settlement components.
- **Agent 3 (Testing & Hooks):** Ensures system stability with an `xUnit` test suite boasting 100% code coverage. Configures Git pre-push hooks to block code lacking coverage.
- **Agent 4 (Documentation):** Authors system instructions, architecture diagrams, and the primary project documentation.

## Architecture Flow
```text
[ Integrator ]
      |
      v (Drops JSON files)
[ shared/input/ ]
      |
      v (Polls for new messages)
[ Transaction Validator ] ---> (Invalid?) ---> [ shared/results/ ]
      |
      v (Valid transactions)
[ shared/output/ ]
      |
      v (Polls for fraud_detector messages)
[ Fraud Detector ] ---> (High Risk?) ---> [ shared/results/ ]
      |
      v (Cleared transactions back to output)
[ shared/output/ ]
      |
      v (Polls for settlement_processor messages)
[ Settlement Processor ]
      |
      v (Marks as settled)
[ shared/results/ ]
```

## Tech Stack
| Component | Technology | Description |
|-----------|------------|-------------|
| **Core Framework** | `.NET 10` | The primary runtime for the minimal APIs and console apps. |
| **Worker Processing** | `BackgroundService` | Used to build periodic polling listeners for file drops. |
| **Testing** | `xUnit`, `coverlet` | Provides unit/integration testing and >80% coverage gating. |
| **Tooling/MCP** | `FastMCP` (Python) | A custom server to inspect the pipeline statuses externally. |
