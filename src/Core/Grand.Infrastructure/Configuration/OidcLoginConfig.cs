namespace Grand.Infrastructure.Configuration;

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
    ///     Back-office accounts connected to the identity provider with their password sign in only through the provider;
    ///     an account without such a connection keeps its password (break-glass)
    /// </summary>
    public bool StaffSsoOnly { get; set; }

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
