using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<ValidatorWorker>();

var app = builder.Build();
app.MapGet("/", () => "Validator Agent Running");
app.Run();

public class ValidatorWorker : BackgroundService
{
    private readonly ILogger<ValidatorWorker> _logger;
    private readonly string _inputDir;
    private readonly string _outputDir;
    private readonly string _resultsDir;

    public ValidatorWorker(ILogger<ValidatorWorker> logger)
    {
        _logger = logger;
        var sharedDir = Environment.GetEnvironmentVariable("TEST_SHARED_DIR");
        if (string.IsNullOrEmpty(sharedDir))
        {
            var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            sharedDir = Path.Combine(baseDir, "shared");
        }
        _inputDir = Path.Combine(sharedDir, "input");
        _outputDir = Path.Combine(sharedDir, "output");
        _resultsDir = Path.Combine(sharedDir, "results");
        
        Directory.CreateDirectory(_inputDir);
        Directory.CreateDirectory(_outputDir);
        Directory.CreateDirectory(_resultsDir);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Validator Worker starting.");
        while (!stoppingToken.IsCancellationRequested)
        {
            if (Directory.Exists(_inputDir))
            {
                var files = Directory.GetFiles(_inputDir, "*.json");
                foreach (var file in files)
                {
                    await ProcessFileAsync(file);
                }
            }
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessFileAsync(string filePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var message = JsonSerializer.Deserialize<AgentMessage>(content);
            if (message == null || message.TargetAgent != "transaction_validator") return;

            _logger.LogInformation("Validating transaction {txId}", message.Data.TransactionId);
            
            bool isValid = true;
            string rejectReason = "";

            if (!decimal.TryParse(message.Data.Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                isValid = false;
                rejectReason = "Invalid amount format.";
            }
            else if (amount <= 0)
            {
                isValid = false;
                rejectReason = "Amount must be positive.";
            }

            var validCurrencies = new[] { "USD", "EUR", "GBP", "JPY" };
            if (!validCurrencies.Contains(message.Data.Currency.ToUpper()))
            {
                isValid = false;
                rejectReason = "Unsupported currency.";
            }

            if (isValid)
            {
                message.Data.Status = "validated";
                message.TargetAgent = "fraud_detector";
                message.SourceAgent = "transaction_validator";
                message.Timestamp = DateTime.UtcNow.ToString("O");
                
                var outPath = Path.Combine(_outputDir, Path.GetFileName(filePath));
                await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                message.Data.Status = "rejected";
                message.Data.ExtensionData["reject_reason"] = JsonSerializer.SerializeToElement(rejectReason);
                message.TargetAgent = "results";
                message.SourceAgent = "transaction_validator";
                message.Timestamp = DateTime.UtcNow.ToString("O");
                
                var resPath = Path.Combine(_resultsDir, Path.GetFileName(filePath));
                await File.WriteAllTextAsync(resPath, JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true }));
            }

            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {file}", filePath);
        }
    }
}

public class AgentMessage
{
    [JsonPropertyName("message_id")] public string MessageId { get; set; } = "";
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = "";
    [JsonPropertyName("source_agent")] public string SourceAgent { get; set; } = "";
    [JsonPropertyName("target_agent")] public string TargetAgent { get; set; } = "";
    [JsonPropertyName("message_type")] public string MessageType { get; set; } = "";
    [JsonPropertyName("data")] public TransactionData Data { get; set; } = new();
}

public class TransactionData
{
    [JsonPropertyName("transaction_id")] public string TransactionId { get; set; } = "";
    [JsonPropertyName("amount")] public string Amount { get; set; } = "";
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "new";
    [JsonExtensionData] public Dictionary<string, JsonElement> ExtensionData { get; set; } = new();
}
