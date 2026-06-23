---
name: validate-transactions
description: Validate all transactions in sample-transactions.json without processing them fully.
---

# Validate Transactions Skill

When the user types `/validate-transactions`, execute the following steps:

1. Read and parse `sample-transactions.json`.
2. Evaluate each transaction using the logic from the Validator agent:
   - Amount must be a valid, positive decimal.
   - Currency must be supported (e.g., USD, EUR, GBP, JPY).
3. Report the total count of transactions, valid count, and invalid count.
4. List the reasons for rejection for the invalid transactions.
5. Show a summary table of the results.
