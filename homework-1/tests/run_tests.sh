#!/bin/bash

BASE_URL="http://localhost:5000"

echo "🏦 Banking API Detailed Automated Test Suite"
echo "=================================="
echo "Ensure the API is running at $BASE_URL before continuing!"
echo ""
sleep 1

echo "--- VALIDATION & EDGE CASES ---"

echo "🔴 1.1 Validation: Negative Amount & Invalid Currency"
curl -s -X POST "$BASE_URL/transactions" -H "Content-Type: application/json" -d '{"toAccount": "ACC-123", "amount": -50.00, "currency": "XYZ", "type": 0}' | jq .
echo -e "\n----------------------------------\n"

echo "🔴 1.2 Validation: Too many decimal places"
curl -s -X POST "$BASE_URL/transactions" -H "Content-Type: application/json" -d '{"toAccount": "ACC-123", "amount": 100.123, "currency": "USD", "type": 0}' | jq .
echo -e "\n----------------------------------\n"

echo "🔴 1.3 Validation: Missing ToAccount for Deposit"
curl -s -X POST "$BASE_URL/transactions" -H "Content-Type: application/json" -d '{"amount": 100.00, "currency": "USD", "type": 0}' | jq .
echo -e "\n----------------------------------\n"

echo "🔴 1.4 Validation: Missing FromAccount for Withdrawal"
curl -s -X POST "$BASE_URL/transactions" -H "Content-Type: application/json" -d '{"amount": 100.00, "currency": "USD", "type": 1}' | jq .
echo -e "\n----------------------------------\n"

echo "🔴 1.5 Validation: Missing Both Accounts for Transfer"
curl -s -X POST "$BASE_URL/transactions" -H "Content-Type: application/json" -d '{"amount": 100.00, "currency": "USD", "type": 2}' | jq .
echo -e "\n----------------------------------\n"

echo "🔴 1.6 Validation: Invalid Account Format"
curl -s -X POST "$BASE_URL/transactions" -H "Content-Type: application/json" -d '{"fromAccount": "BADFORMAT", "toAccount": "123-ACC", "amount": 100.00, "currency": "USD", "type": 2}' | jq .
echo -e "\n----------------------------------\n"

echo "--- NOT FOUND SCENARIOS ---"

echo "🔴 2.1 Not Found: Fake Transaction ID"
curl -s -X GET "$BASE_URL/transactions/00000000-0000-0000-0000-000000000000" | jq .
echo -e "\n----------------------------------\n"

echo "🔴 2.2 Not Found: Balance for Non-Existent Account"
curl -s -X GET "$BASE_URL/accounts/NONEXISTENT-999/balance" | jq .
echo -e "\n----------------------------------\n"

echo "🔴 2.3 Not Found: Summary for Non-Existent Account"
curl -s -X GET "$BASE_URL/accounts/NONEXISTENT-999/summary" | jq .
echo -e "\n----------------------------------\n"

echo "--- SUCCESS PATHS ---"

echo "🟢 3.1 Creating a Deposit for ACC-TEST ($1000)"
TX_JSON=$(curl -s -X POST "$BASE_URL/transactions" -H "Content-Type: application/json" -d '{"toAccount": "ACC-TEST", "amount": 1000.00, "currency": "USD", "type": 0}')
echo $TX_JSON | jq .
TX_ID=$(echo $TX_JSON | jq -r '.id')
echo -e "\n----------------------------------\n"

echo "🟢 3.2 Fetching the created transaction by ID"
curl -s -X GET "$BASE_URL/transactions/$TX_ID" | jq .
echo -e "\n----------------------------------\n"

echo "🟢 3.3 Creating a Withdrawal from ACC-TEST ($200)"
curl -s -X POST "$BASE_URL/transactions" -H "Content-Type: application/json" -d '{"fromAccount": "ACC-TEST", "amount": 200.00, "currency": "USD", "type": 1}' | jq .
echo -e "\n----------------------------------\n"

echo "🟢 3.4 Creating a Transfer from ACC-TEST to ACC-RECEIVER ($150)"
curl -s -X POST "$BASE_URL/transactions" -H "Content-Type: application/json" -d '{"fromAccount": "ACC-TEST", "toAccount": "ACC-RECEIVER", "amount": 150.00, "currency": "USD", "type": 2}' | jq .
echo -e "\n----------------------------------\n"

echo "--- CALCULATIONS & QUERIES ---"

echo "🔍 4.1 Fetching Account Balance for ACC-TEST (Expected: 1000 - 200 - 150 = 650)"
curl -s -X GET "$BASE_URL/accounts/ACC-TEST/balance" | jq .
echo -e "\n----------------------------------\n"

echo "📊 4.2 Fetching Account Summary for ACC-TEST"
curl -s -X GET "$BASE_URL/accounts/ACC-TEST/summary" | jq .
echo -e "\n----------------------------------\n"

echo "🔎 4.3 Filtering Transactions (Combined: accountId=ACC-TEST & type=deposit)"
curl -s -X GET "$BASE_URL/transactions?accountId=ACC-TEST&type=deposit" | jq .
echo -e "\n----------------------------------\n"

echo "🚨 5. Testing Rate Limiter (Sending 110 rapid requests...)"
for i in {1..110}
do
   STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/transactions")
   if [ "$STATUS" -eq 429 ]; then
      echo "Request $i: 🔴 429 Too Many Requests (Rate limit hit!)"
      break
   fi
done

echo ""
echo "✅ Detailed Test Suite Complete!"
