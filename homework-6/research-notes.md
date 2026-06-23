# Research Notes

## Query 1: Decimal parsing in .NET Minimal APIs
- **Search:** "C# decimal parsing invariant culture"
- **context7 library ID:** `/dotnet/decimal`
- **Applied:** Used `decimal.TryParse` with `NumberStyles.Any` and `CultureInfo.InvariantCulture` in the `ValidatorWorker` to reliably handle monetary amounts passed as strings from JSON.

## Query 2: Watching folders with .NET BackgroundService
- **Search:** "FileSystemWatcher vs Periodic Polling in BackgroundService ASP.NET Core"
- **context7 library ID:** `/dotnet/hosting`
- **Applied:** Selected a periodic polling approach (`Task.Delay` in `ExecuteAsync`) instead of `FileSystemWatcher` to avoid complex file lock handling when multiple agent processes rapidly create and delete files in the shared `output` directory.
