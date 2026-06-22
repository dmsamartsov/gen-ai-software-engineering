using System.Security.Cryptography;
using System.Text;

namespace WikiApi.Auth;

/// <summary>
/// Authorizes privileged operations (e.g. deleting a wiki document) by checking the
/// <c>X-Admin-Token</c> request header.
/// </summary>
public class AdminTokenValidator
{
    private readonly string _adminToken;

    public AdminTokenValidator(string adminToken)
    {
        _adminToken = adminToken;
    }

    public bool IsValid(string? providedToken)
    {
        if (string.IsNullOrEmpty(providedToken) || string.IsNullOrEmpty(_adminToken))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var adminBytes = Encoding.UTF8.GetBytes(_adminToken);

        return CryptographicOperations.FixedTimeEquals(providedBytes, adminBytes);
    }
}
