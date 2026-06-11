# 🏦 Homework 1: Banking Transactions API

> **Student Name**: Dmytro Samartsov
> **Date Submitted**: 30.04.2026
> **AI Tools Used**: Antigravity, Gemini 3.1 Pro

---

## 📋 Project Overview

This project is a RESTful API built with **.NET 10 Minimal APIs** that processes and manages simple banking transactions. It leverages an in-memory thread-safe data store (`ConcurrentDictionary`) to provide high-performance operations without relying on an external database.

### 🌟 Key Features
- **Core Banking Operations**: Supports `Deposit`, `Withdrawal`, and `Transfer` transaction types, with dedicated endpoints for transaction creation and retrieving dynamically calculated account balances.
- **Robust Validation System**: Thoroughly validates incoming requests, guaranteeing that amounts are strictly positive (max 2 decimal places), account IDs follow an alphanumeric `ACC-XXXXX` format, and currencies are strictly valid ISO 4217 codes. Validation errors are batched and returned in a structured `400 Bad Request` JSON payload.
- **Flexible Transaction Filtering**: The transaction history endpoint (`GET /transactions`) supports stacking multiple query parameters, allowing developers to filter simultaneously by `accountId`, transaction `type`, and specific date boundaries (`from`/`to`).
- **Account Summary Engine (Task 4 Option A)**: Exposes a dedicated `GET /accounts/{accountId}/summary` endpoint calculating key metrics such as total incoming deposits, outgoing withdrawals, raw transaction counts, and recent activity timestamps.
- **IP-Based Rate Limiting (Task 4 Option D)**: Secures the API using ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting` middleware. A Fixed Window algorithm restricts clients to **100 requests per minute per IP**, shielding the app with `429 Too Many Requests` responses when exceeded.
- **Demo Test Suite**: Includes a fully featured `demo/api-tests.http` script covering successful states, boundary cases, complex filters, and validations, runnable directly from within modern IDEs.


<div align="center">

*This project was completed as part of the AI-Assisted Development course.*

</div>
