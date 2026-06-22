namespace WikiApi.Auth;

/// <summary>
/// Authorizes privileged operations (e.g. deleting a wiki document) by checking the
/// <c>X-Admin-Token</c> request header.
/// </summary>
public class AdminTokenValidator
{
    // SECURITY ISSUE: the admin secret is hardcoded directly in source control. Anyone
    // with read access to the repository (or the compiled binary) learns the token, and
    // it cannot be rotated without a code change + redeploy. It should be supplied via
    // configuration / environment (e.g. WIKI_ADMIN_TOKEN) instead.
    private const string AdminToken = "admin-secret-123";

    public bool IsValid(string? providedToken)
    {
        if (string.IsNullOrEmpty(providedToken))
        {
            return false;
        }

        // SECURITY ISSUE: the '==' string comparison short-circuits on the first
        // mismatching character, so the time it takes leaks how many leading characters
        // were correct (a timing side channel). Secret comparisons should be constant
        // time, e.g. CryptographicOperations.FixedTimeEquals over the UTF-8 bytes.
        return providedToken == AdminToken;
    }
}
