Act as an expert .NET developer. Create a Simple Banking Transactions REST API using a .NET 10 Web API project (using Minimal APIs). 

**Technical Constraints & Storage:**
- Use in-memory collection  — no database.

**Endpoints:**
1. `POST /transactions` - Create a new transaction.
2. `GET /transactions` - List all transactions.
3. `GET /transactions/{id}` - Get a specific transaction by ID.
4. `GET /accounts/{accountId}/balance` - Calculate and return the total balance for an account based on its transaction history.

**Transaction Model (C# Record/Class):**
- `Id`: string/Guid (auto-generated)
- `FromAccount`: string (can be null for deposits)
- `ToAccount`: string (can be null for withdrawals)
- `Amount`: decimal
- `Currency`: string (ISO 4217: USD, EUR, GBP, etc.)
- `Type`: enum (Deposit, Withdrawal, Transfer)
- `Timestamp`: DateTimeOffset (ISO 8601)
- `Status`: enum (Pending, Completed, Failed)

**Validation & Error Handling Rules:**
- Amount must be strictly greater than 0.
- Return `400 Bad Request` if validation fails (e.g., negative amount, missing required fields).
- Return `404 Not Found` if an ID or Account is not found.
- Return `201 Created` on successful POST, and `200 OK` for successful GETs.
- Include a basic exception handler to catch unhandled errors and return a clean 500 response.