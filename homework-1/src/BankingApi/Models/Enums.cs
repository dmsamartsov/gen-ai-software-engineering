namespace BankingApi.Models;

public enum TransactionType
{
    Deposit,
    Withdrawal,
    Transfer
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed
}
