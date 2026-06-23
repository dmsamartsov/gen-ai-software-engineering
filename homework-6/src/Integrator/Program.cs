using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Integrator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            var sharedDir = Path.Combine(baseDir, "shared");

            Directory.CreateDirectory(Path.Combine(sharedDir, "input"));
            Directory.CreateDirectory(Path.Combine(sharedDir, "processing"));
            Directory.CreateDirectory(Path.Combine(sharedDir, "output"));
            Directory.CreateDirectory(Path.Combine(sharedDir, "results"));

            var sampleFile = Path.Combine(baseDir, "sample-transactions.json");
            if (!File.Exists(sampleFile))
            {
                Console.WriteLine($"Error: {sampleFile} not found.");
                return;
            }

            var content = await File.ReadAllTextAsync(sampleFile);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            var transactions = JsonSerializer.Deserialize<List<TransactionData>>(content, options);
            if (transactions == null) return;

            Console.WriteLine($"Loaded {transactions.Count} transactions. Dispatching to shared/input...");

            foreach (var tx in transactions)
            {
                var message = new AgentMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow.ToString("O"),
                    SourceAgent = "integrator",
                    TargetAgent = "transaction_validator",
                    MessageType = "transaction",
                    Data = tx
                };

                var outPath = Path.Combine(sharedDir, "input", $"{message.MessageId}.json");
                var jsonStr = JsonSerializer.Serialize(message, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(outPath, jsonStr);
                Console.WriteLine($"Dropped message {message.MessageId}.json for {tx.TransactionId}");
            }

            Console.WriteLine("Integration complete. Start the agents to process the messages.");
        }
    }

    public class AgentMessage
    {
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = "";
        
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "";
        
        [JsonPropertyName("source_agent")]
        public string SourceAgent { get; set; } = "";
        
        [JsonPropertyName("target_agent")]
        public string TargetAgent { get; set; } = "";
        
        [JsonPropertyName("message_type")]
        public string MessageType { get; set; } = "";
        
        [JsonPropertyName("data")]
        public TransactionData Data { get; set; } = new();
    }

    public class TransactionData
    {
        [JsonPropertyName("transaction_id")]
        public string TransactionId { get; set; } = "";

        [JsonPropertyName("amount")]
        public string Amount { get; set; } = "";

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "new";

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; set; } = new();
    }
}
