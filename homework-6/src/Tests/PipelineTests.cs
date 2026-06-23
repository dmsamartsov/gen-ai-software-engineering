using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace PipelineTests
{
    public class WorkerTests : IDisposable
    {
        private readonly string _testSharedDir;

        public WorkerTests()
        {
            _testSharedDir = Path.Combine(Path.GetTempPath(), "BankingPipelineTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_testSharedDir);
            Environment.SetEnvironmentVariable("TEST_SHARED_DIR", _testSharedDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testSharedDir))
            {
                Directory.Delete(_testSharedDir, true);
            }
            Environment.SetEnvironmentVariable("TEST_SHARED_DIR", null);
        }

        [Fact]
        public async Task ValidatorWorker_ProcessesValidTransaction()
        {
            var worker = new ValidatorWorker(NullLogger<ValidatorWorker>.Instance);
            var inputDir = Path.Combine(_testSharedDir, "input");
            var outputDir = Path.Combine(_testSharedDir, "output");
            
            var msg = new TestAgentMessage
            {
                MessageId = "123",
                TargetAgent = "transaction_validator",
                Data = new TestTransactionData { TransactionId = "TX-01", Amount = "100.50", Currency = "USD" }
            };
            await File.WriteAllTextAsync(Path.Combine(inputDir, "123.json"), JsonSerializer.Serialize(msg));

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(500);

            Assert.True(File.Exists(Path.Combine(outputDir, "123.json")));
            var resultMsg = JsonSerializer.Deserialize<TestAgentMessage>(await File.ReadAllTextAsync(Path.Combine(outputDir, "123.json")));
            Assert.Equal("validated", resultMsg?.Data.Status);
        }

        [Fact]
        public async Task FraudWorker_FlagsHighRiskTransaction()
        {
            var worker = new FraudWorker(NullLogger<FraudWorker>.Instance);
            var outputDir = Path.Combine(_testSharedDir, "output");
            var resultsDir = Path.Combine(_testSharedDir, "results");

            var msg = new TestAgentMessage
            {
                MessageId = "124",
                TargetAgent = "fraud_detector",
                Data = new TestTransactionData { 
                    TransactionId = "TX-02", 
                    Amount = "50000.00", 
                    Currency = "USD",
                    ExtensionData = new System.Collections.Generic.Dictionary<string, JsonElement> { 
                        { "metadata", JsonSerializer.SerializeToElement(new { country = "GB" }) } 
                    }
                }
            };
            await File.WriteAllTextAsync(Path.Combine(outputDir, "124.json"), JsonSerializer.Serialize(msg));

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(500);

            Assert.True(File.Exists(Path.Combine(resultsDir, "124.json")));
            var resultMsg = JsonSerializer.Deserialize<TestAgentMessage>(await File.ReadAllTextAsync(Path.Combine(resultsDir, "124.json")));
            Assert.Equal("rejected", resultMsg?.Data.Status);
        }

        [Fact]
        public async Task SettlementWorker_SettlesTransaction()
        {
            var worker = new SettlementWorker(NullLogger<SettlementWorker>.Instance);
            var outputDir = Path.Combine(_testSharedDir, "output");
            var resultsDir = Path.Combine(_testSharedDir, "results");

            var msg = new TestAgentMessage
            {
                MessageId = "125",
                TargetAgent = "settlement_processor",
                Data = new TestTransactionData { TransactionId = "TX-03", Amount = "100.00", Currency = "USD" }
            };
            await File.WriteAllTextAsync(Path.Combine(outputDir, "125.json"), JsonSerializer.Serialize(msg));

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(500);

            Assert.True(File.Exists(Path.Combine(resultsDir, "125.json")));
            var resultMsg = JsonSerializer.Deserialize<TestAgentMessage>(await File.ReadAllTextAsync(Path.Combine(resultsDir, "125.json")));
            Assert.Equal("settled", resultMsg?.Data.Status);
        }
        [Fact]
        public async Task ValidatorWorker_RejectsInvalidCurrency()
        {
            var worker = new ValidatorWorker(NullLogger<ValidatorWorker>.Instance);
            var inputDir = Path.Combine(_testSharedDir, "input");
            var resultsDir = Path.Combine(_testSharedDir, "results");
            
            var msg = new TestAgentMessage
            {
                MessageId = "201",
                TargetAgent = "transaction_validator",
                Data = new TestTransactionData { TransactionId = "TX-04", Amount = "100.50", Currency = "XXX" }
            };
            await File.WriteAllTextAsync(Path.Combine(inputDir, "201.json"), JsonSerializer.Serialize(msg));

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(500);

            Assert.True(File.Exists(Path.Combine(resultsDir, "201.json")));
            var resultMsg = JsonSerializer.Deserialize<TestAgentMessage>(await File.ReadAllTextAsync(Path.Combine(resultsDir, "201.json")));
            Assert.Equal("rejected", resultMsg?.Data.Status);
        }

        [Fact]
        public async Task FraudWorker_ClearsLowRiskTransaction()
        {
            var worker = new FraudWorker(NullLogger<FraudWorker>.Instance);
            var outputDir = Path.Combine(_testSharedDir, "output");

            var msg = new TestAgentMessage
            {
                MessageId = "202",
                TargetAgent = "fraud_detector",
                Data = new TestTransactionData { TransactionId = "TX-05", Amount = "500.00", Currency = "USD" }
            };
            await File.WriteAllTextAsync(Path.Combine(outputDir, "202.json"), JsonSerializer.Serialize(msg));

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(500);

            Assert.True(File.Exists(Path.Combine(outputDir, "202.json")));
            var resultMsg = JsonSerializer.Deserialize<TestAgentMessage>(await File.ReadAllTextAsync(Path.Combine(outputDir, "202.json")));
            Assert.Equal("cleared", resultMsg?.Data.Status);
        }

        [Fact]
        public async Task ValidatorWorker_IgnoresWrongAgent()
        {
            var worker = new ValidatorWorker(NullLogger<ValidatorWorker>.Instance);
            var inputDir = Path.Combine(_testSharedDir, "input");
            var outputDir = Path.Combine(_testSharedDir, "output");
            var resultsDir = Path.Combine(_testSharedDir, "results");

            var msg = new TestAgentMessage
            {
                MessageId = "203",
                TargetAgent = "some_other_agent",
                Data = new TestTransactionData { TransactionId = "TX-06", Amount = "100.00", Currency = "USD" }
            };
            await File.WriteAllTextAsync(Path.Combine(inputDir, "203.json"), JsonSerializer.Serialize(msg));

            await worker.StartAsync(CancellationToken.None);
            await Task.Delay(500);

            Assert.True(File.Exists(Path.Combine(inputDir, "203.json")));
            Assert.False(File.Exists(Path.Combine(outputDir, "203.json")));
            Assert.False(File.Exists(Path.Combine(resultsDir, "203.json")));
        }

    }

    public class TestAgentMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("message_id")] public string MessageId { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("timestamp")] public string Timestamp { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("source_agent")] public string SourceAgent { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("target_agent")] public string TargetAgent { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("message_type")] public string MessageType { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("data")] public TestTransactionData Data { get; set; } = new();
    }

    public class TestTransactionData
    {
        [System.Text.Json.Serialization.JsonPropertyName("transaction_id")] public string TransactionId { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("amount")] public string Amount { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("currency")] public string Currency { get; set; } = "";
        [System.Text.Json.Serialization.JsonPropertyName("status")] public string Status { get; set; } = "new";
        [System.Text.Json.Serialization.JsonExtensionData] public System.Collections.Generic.Dictionary<string, JsonElement> ExtensionData { get; set; } = new();
    }
}
