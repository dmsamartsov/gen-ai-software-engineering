using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<FraudWorker>();

var app = builder.Build();
app.MapGet("/", () => "Fraud Detector Agent Running");
app.Run();

public class FraudWorker : BackgroundService
{
    private readonly ILogger<FraudWorker> _logger;
    private readonly string _outputDir;
    private readonly string _resultsDir;
    private readonly string _processingDir;

    public FraudWorker(ILogger<FraudWorker> logger)
    {
        _logger = logger;
        var sharedDir = Environment.GetEnvironmentVariable("TEST_SHARED_DIR");
        if (string.IsNullOrEmpty(sharedDir))
        {
            var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            sharedDir = Path.Combine(baseDir, "shared");
        }
        _outputDir = Path.Combine(sharedDir, "output");
        _resultsDir = Path.Combine(sharedDir, "results");
        _processingDir = Path.Combine(sharedDir, "processing");

        Directory.CreateDirectory(_outputDir);
        Directory.CreateDirectory(_resultsDir);
        Directory.CreateDirectory(_processingDir);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fraud Detector Worker starting.");
        while (!stoppingToken.IsCancellationRequested)
        {
            if (Directory.Exists(_outputDir))
            {
                var files = Directory.GetFiles(_outputDir, "*.json");
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
            if (message == null || message.TargetAgent != "fraud_detector") return;

            var processingPath = Path.Combine(_processingDir, Path.GetFileName(filePath));
            File.Move(filePath, processingPath, overwrite: true);

            _logger.LogInformation("Scoring transaction {txId}", message.Data.TransactionId);

            decimal.TryParse(message.Data.Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount);
            
            int riskScore = 0;
            if (amount > 10000) riskScore += 50;
            if (message.Data.ExtensionData.TryGetValue("metadata", out var meta) && 
                meta.TryGetProperty("country", out var country) && country.GetString() != "US")
            {
                riskScore += 30;
            }

            message.Data.ExtensionData["risk_score"] = JsonSerializer.SerializeToElement(riskScore);

            if (riskScore >= 80)
            {
                message.Data.Status = "rejected";
                message.Data.ExtensionData["reject_reason"] = JsonSerializer.SerializeToElement("High risk score.");
                message.TargetAgent = "results";
                message.SourceAgent = "fraud_detector";
                message.Timestamp = DateTime.UtcNow.ToString("O");
                
                var resPath = Path.Combine(_resultsDir, Path.GetFileName(filePath));
                await File.WriteAllTextAsync(resPath, JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                message.Data.Status = "cleared";
                message.TargetAgent = "settlement_processor";
                message.SourceAgent = "fraud_detector";
                message.Timestamp = DateTime.UtcNow.ToString("O");
                
                var outPath = Path.Combine(_outputDir, Path.GetFileName(filePath));
                await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true }));
            }

            File.Delete(processingPath);
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
