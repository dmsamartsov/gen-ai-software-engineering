namespace BankingApi.Models;

public record Transaction
{
    public Guid Id { get; init; }
    public string? FromAccount { get; init; }
    public string? ToAccount { get; init; }
    public decimal Amount { get; init; }
    public required string Currency { get; init; }
    public TransactionType Type { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public TransactionStatus Status { get; init; }
}

public record CreateTransactionDto(
    string? FromAccount,
    string? ToAccount,
    decimal Amount,
    string Currency,
    TransactionType Type
);
