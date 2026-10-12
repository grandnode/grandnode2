namespace Authentication.OpenIdConnect.Infrastructure;

public sealed record OidcLogoutToken(bool IsValid, string Subject, string Sid, string Error, string ReplayKey = null)
{
    public static OidcLogoutToken Valid(string subject, string sid, string replayKey = null) =>
        new(true, subject, sid, null, replayKey);
    public static OidcLogoutToken Invalid(string error) => new(false, null, null, error);
}
