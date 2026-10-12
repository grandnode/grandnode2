namespace Authentication.OpenIdConnect.Infrastructure;

public sealed record OidcLogoutToken(bool IsValid, string Subject, string Sid, string Error)
{
    public static OidcLogoutToken Valid(string subject, string sid) => new(true, subject, sid, null);
    public static OidcLogoutToken Invalid(string error) => new(false, null, null, error);
}
