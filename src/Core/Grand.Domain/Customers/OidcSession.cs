namespace Grand.Domain.Customers;

/// <summary>
///     A browser session started by a sign-in at the OpenID Connect identity provider; the main authentication cookie
///     carries its id, so the session can be ended on the server (sign-out, back-channel logout, staff policy)
/// </summary>
public class OidcSession : BaseEntity
{
    public string CustomerId { get; set; }

    /// <summary>The store of the sign-in</summary>
    public string StoreId { get; set; }

    /// <summary>Provider subject (sub)</summary>
    public string Subject { get; set; }

    /// <summary>Provider session id (sid) from the ID token; empty when the provider sends none</summary>
    public string Sid { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastActivityUtc { get; set; }

    /// <summary>Last successful check of the account at the provider</summary>
    public DateTime LastValidatedUtc { get; set; }

    /// <summary>Refresh token protected with Data Protection; kept only for back-office accounts</summary>
    public string ProtectedRefreshToken { get; set; }

    public DateTime? EndedUtc { get; set; }
    public OidcSessionEndReason? EndReason { get; set; }
}

public enum OidcSessionEndReason
{
    SignOut = 1,
    BackChannel = 2,
    Idle = 3,
    MaxLifetime = 4,
    RevalidationRefused = 5,
    RevalidationUnavailable = 6,
    NoRefreshToken = 7
}
