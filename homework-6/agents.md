# Project Agents

This document defines the roles of the four meta-agents used to build the Multi-Agent Banking Pipeline.

## Agent 1 — Specification
- **Role:** Creates detailed technical specifications for the transaction processing system.
- **Responsibilities:** Defining requirements, data contracts, file-based communication protocols, and writing `specification.md`.
- **Plus:** Implements a slash command skill (`/write-spec`) to automate specification generation.

## Agent 2 — Code Generation
- **Role:** Generates the transaction processing pipeline code.
- **Responsibilities:** Setting up the .NET 10 solution, implementing the Integrator, Validator, Fraud Detector, and Settlement Processor using Minimal APIs and background services.
- **Plus:** Uses `context7` MCP server to look up specific .NET 10 framework documentation, recording queries in `research-notes.md`.

## Agent 3 — Unit Tests & Automation
- **Role:** Creates unit tests and automation hooks.
- **Responsibilities:** Writing `xUnit` unit tests, ensuring code quality, and configuring automation.
- **Plus:** Creates custom skills (`/run-pipeline`, `/validate-transactions`) and a coverage gate hook to block pushes if test coverage falls below 80%.

## Agent 4 — Documentation
- **Role:** Generates project documentation.
- **Responsibilities:** Creating `README.md`, `HOWTORUN.md`, and system architecture diagrams.
- **Plus:** Ensures the README includes the student's name and maintains the overall documentation structure.
