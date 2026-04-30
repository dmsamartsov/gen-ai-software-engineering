#!/bin/bash

BASE_URL="http://localhost:5000"

echo "🏦 Banking API Automated Demo Suite"
echo "=================================="
echo "Ensure the API is running at $BASE_URL before continuing!"
echo ""
sleep 2

echo "🔴 1. Testing Validation (Negative amount & Invalid Currency)"
curl -s -X POST "$BASE_URL/transactions" \
     -H "Content-Type: application/json" \
     -d '{
           "toAccount": "INVALID",
           "amount": -50.00,
           "currency": "XYZ",
           "type": 0
         }' | jq .
echo -e "\n----------------------------------\n"
sleep 1

echo "🟢 2. Creating a Deposit for ACC-123 ($1000)"
curl -s -X POST "$BASE_URL/transactions" \
     -H "Content-Type: application/json" \
     -d '{
           "toAccount": "ACC-123",
           "amount": 1000.00,
           "currency": "USD",
           "type": 0
         }' | jq .
echo -e "\n----------------------------------\n"
sleep 1

echo "🟢 3. Creating a Withdrawal from ACC-123 ($200)"
curl -s -X POST "$BASE_URL/transactions" \
     -H "Content-Type: application/json" \
     -d '{
           "fromAccount": "ACC-123",
           "amount": 200.00,
           "currency": "USD",
           "type": 1
         }' | jq .
echo -e "\n----------------------------------\n"
sleep 1

echo "🟢 4. Creating a Transfer from ACC-123 to ACC-456 ($150)"
curl -s -X POST "$BASE_URL/transactions" \
     -H "Content-Type: application/json" \
     -d '{
           "fromAccount": "ACC-123",
           "toAccount": "ACC-456",
           "amount": 150.00,
           "currency": "USD",
           "type": 2
         }' | jq .
echo -e "\n----------------------------------\n"
sleep 1

echo "🔍 5. Fetching Account Balance for ACC-123 (Expected: 650)"
curl -s -X GET "$BASE_URL/accounts/ACC-123/balance" | jq .
echo -e "\n----------------------------------\n"
sleep 1

echo "📊 6. Fetching Account Summary for ACC-123"
curl -s -X GET "$BASE_URL/accounts/ACC-123/summary" | jq .
echo -e "\n----------------------------------\n"
sleep 1

echo "🔎 7. Filtering Transactions (Type = Transfer)"
curl -s -X GET "$BASE_URL/transactions?type=transfer" | jq .
echo -e "\n----------------------------------\n"
sleep 1

echo "🚨 8. Testing Rate Limiter (Sending 110 rapid requests...)"
for i in {1..110}
do
   STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/transactions")
   if [ "$STATUS" -eq 429 ]; then
      echo "Request $i: 🔴 429 Too Many Requests (Rate limit hit!)"
      break
   fi
done

echo ""
echo "✅ Demo Complete!"
