namespace Grand.Web.Common.Security.Oidc;

/// <param name="SigningKeyNotFound">The token is signed with a key the metadata does not list (yet): the provider may have rotated its keys</param>
public sealed record OidcLogoutToken(bool IsValid, string Subject, string Sid, string Error, string ReplayKey = null,
    bool SigningKeyNotFound = false)
{
    public static OidcLogoutToken Valid(string subject, string sid, string replayKey = null) =>
        new(true, subject, sid, null, replayKey);
    public static OidcLogoutToken Invalid(string error, bool signingKeyNotFound = false) =>
        new(false, null, null, error, SigningKeyNotFound: signingKeyNotFound);
}
