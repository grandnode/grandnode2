namespace Grand.Infrastructure.Configuration;

/// <summary>
///     OAuth 2.1 / OpenID Connect resource server: accepts access tokens issued by an external identity provider
///     on the protected resources listed in <see cref="Resources" /> or registered by plugins
/// </summary>
public class OidcResourceServerConfig
{
    public bool Enabled { get; set; }

    /// <summary>
    ///     Issuer of the identity provider; discovery document at {Authority}/.well-known/openid-configuration
    /// </summary>
    public string Authority { get; set; }

    public bool RequireHttpsMetadata { get; set; } = true;
    public int ClockSkewSeconds { get; set; } = 60;
    public OidcClaimNames Claims { get; set; } = new();

    /// <summary>
    ///     True: a token whose e-mail is not verified never links to an account and never registers one. False: the
    ///     e-mail in the token is trusted as is, except that back-office accounts (admin panel access, vendor, store
    ///     staff) are never linked by it
    /// </summary>
    public bool RequireEmailVerified { get; set; } = true;

    /// <summary>
    ///     Register an unknown user (customer group Registered only)
    /// </summary>
    public bool AutoRegister { get; set; } = true;

    public List<OidcProtectedResource> Resources { get; set; } = [];
}

public class OidcClaimNames
{
    public string Subject { get; set; } = "sub";
    public string Email { get; set; } = "email";
    public string EmailVerified { get; set; } = "email_verified";
}
