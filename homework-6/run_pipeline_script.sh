#!/bin/bash
cd "/Users/dmytrosamartsov/Documents/repos/gen-ai-software-engineering copy/homework-6"

echo "Cleaning directories..."
rm -f ../shared/input/*.json
rm -f ../shared/processing/*.json
rm -f ../shared/output/*.json
rm -f ../shared/results/*.json

echo "Running Integrator..."
dotnet run --project src/Integrator/Integrator.csproj

echo "Starting agents in background..."
dotnet run --project src/Validator/Validator.csproj &
VALIDATOR_PID=$!

dotnet run --project src/FraudDetector/FraudDetector.csproj &
FRAUD_PID=$!

dotnet run --project src/Settlement/Settlement.csproj &
SETTLE_PID=$!

echo "Waiting for Validator to finish..."
while [ "$(ls -A ../shared/input/*.json 2>/dev/null)" ]; do
    sleep 1
done

echo "Waiting for FraudDetector to finish..."
while [ "$(ls -A ../shared/output/*.json 2>/dev/null)" ]; do
    sleep 1
done

echo "Waiting for Settlement to finish..."
while [ "$(ls -A ../shared/processing/*.json 2>/dev/null)" ]; do
    sleep 1
done

echo "All agents have finished processing. Killing agents..."
kill $VALIDATOR_PID $FRAUD_PID $SETTLE_PID

echo "Running summarize.py..."
python3.12 summarize.py
