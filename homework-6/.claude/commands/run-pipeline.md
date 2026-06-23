---
name: run-pipeline
description: Run the multi-agent banking pipeline end-to-end.
---

# Run Pipeline Skill

When the user types `/run-pipeline`, execute the following steps to run the multi-agent banking pipeline:

1. **Verify Input:** Check that `sample-transactions.json` exists in the workspace.
2. **Clean Directories:** Delete all JSON files inside `shared/input`, `shared/processing`, `shared/output`, and `shared/results`.
3. **Start Integrator:** Run `dotnet run --project src/Integrator/Integrator.csproj` to drop the initial messages.
4. **Run Agents:** The agents are background services. To process the pipeline:
   - Run `dotnet run --project src/Validator/Validator.csproj` in the background and wait a few seconds until `shared/input` is empty, then stop it.
   - Run `dotnet run --project src/FraudDetector/FraudDetector.csproj` in the background and wait until it processes files in `shared/output`, then stop it.
   - Run `dotnet run --project src/Settlement/Settlement.csproj` in the background and wait until it finalizes the transactions into `shared/results`, then stop it.
5. **Summarize Results:** Read the JSON files in `shared/results/` and show a summary table of the processed transactions (ID, Final Status, Amount).
6. **Report Rejections:** List any transactions that were rejected and provide their `reject_reason`.
