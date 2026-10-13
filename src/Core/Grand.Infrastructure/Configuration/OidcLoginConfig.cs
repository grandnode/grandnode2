namespace Grand.Infrastructure.Configuration;

/// <summary>How accounts sign in: Mixed (passwords and providers, stage B rules) or ProviderOnly (the identity provider only)</summary>
public enum OidcSignInMode
{
    Mixed,
    ProviderOnly
}

/// <summary>
///     appsettings section OidcLogin: the client of this store at the identity provider configured in OidcResourceServer,
///     and the policy for sessions started by a provider sign-in
/// </summary>
public class OidcLoginConfig
{
    private const int DefaultStaffIdleTimeoutMinutes = 30;
    private const int DefaultStaffMaxSessionHours = 10;
    private const int DefaultRevalidationMinutes = 10;

    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Scopes { get; set; } = "openid profile email offline_access";

    /// <summary>A back-office session ends after this much inactivity (sliding)</summary>
    public int StaffIdleTimeoutMinutes { get; set; } = DefaultStaffIdleTimeoutMinutes;

    /// <summary>A back-office session ends this long after the sign-in</summary>
    public int StaffMaxSessionHours { get; set; } = DefaultStaffMaxSessionHours;

    /// <summary>
    ///     Every session started by a provider sign-in confirms the account at the identity provider this often (refresh
    ///     token exchange): an account disabled or deleted there loses GrandNode within this time
    /// </summary>
    public int RevalidationMinutes { get; set; } = DefaultRevalidationMinutes;

    /// <summary>
    ///     Accounts connected to the identity provider with their password sign in only through the provider (storefront,
    ///     panels, Frontend API) and get no password recovery e-mail; an account without such a connection keeps its
    ///     password (break-glass)
    /// </summary>
    public bool SsoOnly { get; set; }

    /// <summary>
    ///     ProviderOnly: the identity provider is the only way to get an account and to sign in; GrandNode passwords work only for
    ///     <see cref="BreakGlassAccounts" /> on the Admin login page. Implies <see cref="SsoOnly" /> for every account
    /// </summary>
    public OidcSignInMode SignInMode { get; set; } = OidcSignInMode.Mixed;

    /// <summary>E-mails of accounts that keep password sign-in and recovery on the Admin login page in ProviderOnly</summary>
    public string[] BreakGlassAccounts { get; set; } = [];

    public bool ProviderOnly => SignInMode == OidcSignInMode.ProviderOnly;

    public bool IsBreakGlass(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || BreakGlassAccounts == null)
            return false;
        var trimmed = email.Trim();
        return BreakGlassAccounts.Any(x =>
            !string.IsNullOrWhiteSpace(x) && string.Equals(x.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
    }

    //zero or less is a configuration mistake, never "check on every request" or "end every session at once"
    public TimeSpan StaffIdleTimeout =>
        TimeSpan.FromMinutes(StaffIdleTimeoutMinutes > 0 ? StaffIdleTimeoutMinutes : DefaultStaffIdleTimeoutMinutes);

    public TimeSpan StaffMaxSession =>
        TimeSpan.FromHours(StaffMaxSessionHours > 0 ? StaffMaxSessionHours : DefaultStaffMaxSessionHours);

    public TimeSpan RevalidationInterval =>
        TimeSpan.FromMinutes(RevalidationMinutes > 0 ? RevalidationMinutes : DefaultRevalidationMinutes);

    public bool IsConfigured(OidcResourceServerConfig resourceServer) =>
        !string.IsNullOrWhiteSpace(resourceServer?.Authority) && !string.IsNullOrWhiteSpace(ClientId);
}
