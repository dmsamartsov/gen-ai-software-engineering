Add validation logic for transactions:

- **Amount validation**: Must be positive, maximum 2 decimal places
- **Account validation**: Account numbers should follow format `ACC-XXXXX` (where X is alphanumeric)
- **Currency validation**: Only accept valid ISO 4217 currency codes (USD, EUR, GBP, JPY, etc.)
- Return meaningful error messages for invalid requests

**Example validation error response:**
```json
{
  "error": "Validation failed",
  "details": [
    {"field": "amount", "message": "Amount must be a positive number"},
    {"field": "currency", "message": "Invalid currency code"}
  ]
}
```