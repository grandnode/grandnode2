namespace Grand.Business.Core.Utilities.Authentication;

public enum OidcRefreshOutcome
{
    /// <summary>The provider accepted the refresh token: the account is still usable there</summary>
    Refreshed,

    /// <summary>A definitive refusal (invalid_grant, invalid_request, any 400/401 of the token endpoint)</summary>
    Refused,

    /// <summary>No answer: network error, timeout, 5xx</summary>
    Unavailable
}

/// <param name="Outcome">The answer of the provider</param>
/// <param name="RefreshToken">The rotated refresh token, or null when the provider keeps the old one</param>
public sealed record OidcRefreshResult(OidcRefreshOutcome Outcome, string RefreshToken = null)
{
    public static OidcRefreshResult Refreshed(string refreshToken) => new(OidcRefreshOutcome.Refreshed, refreshToken);
    public static OidcRefreshResult Refused() => new(OidcRefreshOutcome.Refused);
    public static OidcRefreshResult Unavailable() => new(OidcRefreshOutcome.Unavailable);
}
