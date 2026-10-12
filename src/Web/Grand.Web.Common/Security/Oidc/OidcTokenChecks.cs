using System.Security.Claims;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     Per-resource checks of an access token whose signature, issuer and lifetime are already validated
/// </summary>
public static class OidcTokenChecks
{
    public const string OfflineAccess = "offline_access";

    /// <summary>
    ///     RFC 8707: the token must name this resource (or a configured alternative) in "aud"
    /// </summary>
    public static bool HasAudience(ClaimsPrincipal principal, string canonicalResource, IEnumerable<string> audiences)
    {
        var accepted = (audiences ?? []).Where(a => !string.IsNullOrWhiteSpace(a)).Append(canonicalResource).ToList();
        return principal.FindAll("aud").Any(aud => accepted.Any(a => OidcCanonicalUri.Equal(aud.Value, a)));
    }

    public static bool HasAuthorizedParty(ClaimsPrincipal principal, IList<string> parties)
    {
        if (parties == null || parties.Count == 0)
            return true;
        var party = principal.FindFirst("azp")?.Value ?? principal.FindFirst("client_id")?.Value;
        return party != null && parties.Contains(party, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Required scopes absent from "scope" (space-separated) and "scp" (string or one claim per scope);
    ///     comparison is exact
    /// </summary>
    public static IList<string> MissingScopes(ClaimsPrincipal principal, IEnumerable<string> required)
    {
        var requiredList = PublicScopes(required);
        if (requiredList.Length == 0)
            return [];

        var granted = principal.Claims
            .Where(c => c.Type is "scope" or "scp")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);

        return requiredList.Where(s => !granted.Contains(s)).ToList();
    }

    /// <summary>
    ///     The token is older than the resource accepts (or has no issue time while a limit is set)
    /// </summary>
    public static bool IsTooOld(DateTime issuedUtc, int? maxTokenAgeMinutes, int clockSkewSeconds, DateTime now)
    {
        if (maxTokenAgeMinutes is not > 0)
            return false;
        if (issuedUtc == DateTime.MinValue)
            return true;
        return now - issuedUtc > TimeSpan.FromMinutes(maxTokenAgeMinutes.Value) + TimeSpan.FromSeconds(clockSkewSeconds);
    }

    /// <summary>
    ///     Scopes that may be advertised or required: distinct, non-empty, never offline_access
    /// </summary>
    public static string[] PublicScopes(IEnumerable<string> scopes) =>
        (scopes ?? []).Select(s => s?.Trim()).Where(s => !string.IsNullOrEmpty(s) && s != OfflineAccess)
            .Distinct(StringComparer.Ordinal).ToArray();
}
