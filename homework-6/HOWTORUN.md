# How to Run the Banking Pipeline

This guide will walk you through running the `.NET 10` Multi-Agent Banking Pipeline.

## 1. Setup
Make sure you have the following installed:
- **.NET 10 SDK**: To build and run the C# agents.
- **Python 3**: To run the custom MCP server.
- **pip install mcp**: For the `FastMCP` dependency.

## 2. Running the Pipeline Manually

**Step A: Run the Integrator**
Open a terminal in the `homework-6` directory and run:
```bash
dotnet run --project src/Integrator/Integrator.csproj
```
This will parse `sample-transactions.json` and stage the transactions in the `shared/input/` directory.

**Step B: Start the Agents**
Start each agent in its own terminal window (or run them in the background):
```bash
# Terminal 1
dotnet run --project src/Validator/Validator.csproj

# Terminal 2
dotnet run --project src/FraudDetector/FraudDetector.csproj

# Terminal 3
dotnet run --project src/Settlement/Settlement.csproj
```
Watch the console output as transactions are validated, scored for fraud, and finally settled.

**Step C: View Results**
Navigate to `shared/results/` to view the finalized JSON transaction records.

## 3. Running with AI Skills
You can automate the entire execution by using the provided custom skill in the Antigravity CLI. Type `/run-pipeline` in your chat. The AI will start the integrator, run the agents, and present you with a formatted summary of all transactions.

## 4. Running the Tests
To verify code coverage (which is strictly enforced at > 80%), run:
```bash
dotnet test src/Tests/PipelineTests.csproj /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:Exclude="[Integrator]*"
```

## 5. Running the MCP Server
To interact with the pipeline via MCP tools:
```bash
python3 mcp/server.py
```
This server provides tools like `get_transaction_status` and `list_pipeline_results`.
