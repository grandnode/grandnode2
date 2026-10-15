namespace Grand.Domain.Customers;

/// <summary>
///     The time before which the identity provider's access tokens of a subject are refused (provider sign-out, or the
///     provider refused the account). Kept apart from the subject's links, so removing a link does not bring its revoked
///     tokens back; the identifier is derived from the subject (one record per subject, read by its key)
/// </summary>
public class OidcTokenRevocation : BaseEntity
{
    /// <summary>
    ///     Gets or sets the subject (sub claim) at the identity provider
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    ///     Gets or sets the time before which the subject's access tokens are refused
    /// </summary>
    public DateTime RevokedBeforeUtc { get; set; }
}
