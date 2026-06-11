using System.Collections.Concurrent;
using BankingApi.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var remoteIpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: remoteIpAddress,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new { Error = "Too many requests. Please try again later." }, token);
    };
});

var app = builder.Build();

// Exception Handler
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        await context.Response.WriteAsJsonAsync(new { Error = "An unexpected error occurred.", Details = exception?.Message });
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();
app.UseRateLimiter();

// In-memory data store
var transactions = new ConcurrentDictionary<Guid, Transaction>();

// Endpoints

// 1. Create a new transaction
app.MapPost("/transactions", ([FromBody] CreateTransactionDto dto) =>
{
    var errors = new List<object>();

    // 1. Amount validation
    if (dto.Amount <= 0)
    {
        errors.Add(new { field = "amount", message = "Amount must be a positive number" });
    }
    else if (Math.Round(dto.Amount, 2) != dto.Amount)
    {
        errors.Add(new { field = "amount", message = "Amount cannot have more than 2 decimal places" });
    }

    // 2. Currency validation
    var validCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "SEK", "NZD" };
    if (string.IsNullOrWhiteSpace(dto.Currency) || !validCurrencies.Contains(dto.Currency))
    {
        errors.Add(new { field = "currency", message = "Invalid currency code" });
    }

    // 3. Account validation
    bool IsValidAccount(string? account) => !string.IsNullOrWhiteSpace(account) && System.Text.RegularExpressions.Regex.IsMatch(account, @"^ACC-[a-zA-Z0-9]+$");

    if (dto.Type == TransactionType.Deposit)
    {
        if (!IsValidAccount(dto.ToAccount))
            errors.Add(new { field = "toAccount", message = "Account numbers should follow format ACC-XXXXX" });
    }
    else if (dto.Type == TransactionType.Withdrawal)
    {
        if (!IsValidAccount(dto.FromAccount))
            errors.Add(new { field = "fromAccount", message = "Account numbers should follow format ACC-XXXXX" });
    }
    else if (dto.Type == TransactionType.Transfer)
    {
        if (!IsValidAccount(dto.FromAccount))
            errors.Add(new { field = "fromAccount", message = "Account numbers should follow format ACC-XXXXX" });
        if (!IsValidAccount(dto.ToAccount))
            errors.Add(new { field = "toAccount", message = "Account numbers should follow format ACC-XXXXX" });
    }

    if (errors.Any())
    {
        return Results.BadRequest(new { error = "Validation failed", details = errors });
    }

    var transaction = new Transaction
    {
        Id = Guid.NewGuid(),
        FromAccount = dto.FromAccount,
        ToAccount = dto.ToAccount,
        Amount = dto.Amount,
        Currency = dto.Currency,
        Type = dto.Type,
        Timestamp = DateTimeOffset.UtcNow,
        Status = TransactionStatus.Completed // Defaulting to completed for simplicity
    };

    transactions.TryAdd(transaction.Id, transaction);

    return Results.Created($"/transactions/{transaction.Id}", transaction);
});

// 2. List all transactions (with filtering)
app.MapGet("/transactions", (
    [FromQuery] string? accountId,
    [FromQuery] string? type,
    [FromQuery] DateTimeOffset? from,
    [FromQuery] DateTimeOffset? to) =>
{
    var query = transactions.Values.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(accountId))
    {
        query = query.Where(t => t.FromAccount == accountId || t.ToAccount == accountId);
    }

    if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<TransactionType>(type, true, out var transactionType))
    {
        query = query.Where(t => t.Type == transactionType);
    }

    if (from.HasValue)
    {
        query = query.Where(t => t.Timestamp >= from.Value);
    }

    if (to.HasValue)
    {
        query = query.Where(t => t.Timestamp <= to.Value);
    }

    return Results.Ok(query.ToList());
});

// 3. Get a specific transaction by ID
app.MapGet("/transactions/{id:guid}", (Guid id) =>
{
    if (transactions.TryGetValue(id, out var transaction))
    {
        return Results.Ok(transaction);
    }

    return Results.NotFound(new { Error = "Transaction not found." });
});

// 4. Calculate and return total balance for an account
app.MapGet("/accounts/{accountId}/balance", (string accountId) =>
{
    var accountTransactions = transactions.Values
        .Where(t => t.FromAccount == accountId || t.ToAccount == accountId)
        .ToList();

    if (!accountTransactions.Any())
    {
        return Results.NotFound(new { Error = "Account not found or no transactions exist." });
    }

    decimal balance = 0;

    foreach (var t in accountTransactions)
    {
        if (t.Status == TransactionStatus.Completed)
        {
            if (t.ToAccount == accountId)
            {
                balance += t.Amount;
            }
            if (t.FromAccount == accountId)
            {
                balance -= t.Amount;
            }
        }
    }

    return Results.Ok(new { AccountId = accountId, Balance = balance });
});

// 5. Get transaction summary for an account
app.MapGet("/accounts/{accountId}/summary", (string accountId) =>
{
    var accountTransactions = transactions.Values
        .Where(t => t.FromAccount == accountId || t.ToAccount == accountId)
        .ToList();

    if (!accountTransactions.Any())
    {
        return Results.NotFound(new { Error = "Account not found or no transactions exist." });
    }

    var totalDeposits = accountTransactions
        .Where(t => t.ToAccount == accountId && t.Status == TransactionStatus.Completed)
        .Sum(t => t.Amount);

    var totalWithdrawals = accountTransactions
        .Where(t => t.FromAccount == accountId && t.Status == TransactionStatus.Completed)
        .Sum(t => t.Amount);

    var numberOfTransactions = accountTransactions.Count;
    var mostRecentTransactionDate = accountTransactions.Max(t => t.Timestamp);

    return Results.Ok(new
    {
        AccountId = accountId,
        TotalDeposits = totalDeposits,
        TotalWithdrawals = totalWithdrawals,
        NumberOfTransactions = numberOfTransactions,
        MostRecentTransactionDate = mostRecentTransactionDate
    });
});

app.Run();
