#!/bin/bash
# Pre-push hook to enforce 80% code coverage

echo "Running .NET tests and checking coverage threshold (80%)..."

# Resolve the repository root to make paths durable even when run as a symlink
REPO_ROOT="$(git rev-parse --show-toplevel)"

# Run the tests with coverlet threshold enforcement
dotnet test "$REPO_ROOT/homework-6/src/Tests/PipelineTests.csproj" /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line

if [ $? -ne 0 ]; then
    echo "❌ Error: Test coverage is below 80% or tests failed! Push blocked."
    exit 1
fi

echo "✅ Coverage check passed. Proceeding with push."
exit 0
