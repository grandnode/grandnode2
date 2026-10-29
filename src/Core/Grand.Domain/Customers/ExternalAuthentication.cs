using Grand.Domain.Audit;

namespace Grand.Domain.Customers;

/// <summary>
///     Represents an external authentication
/// </summary>
public class ExternalAuthentication : BaseEntity
{
    /// <summary>
    ///     Gets or sets the customer identifier
    /// </summary>
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the external email
    /// </summary>
    [AuditName]
    public string Email { get; set; }

    /// <summary>
    ///     Gets or sets the external identifier
    /// </summary>
    public string ExternalIdentifier { get; set; }

    /// <summary>
    ///     Gets or sets the external display identifier
    /// </summary>
    public string ExternalDisplayIdentifier { get; set; }

    /// <summary>
    ///     Gets or sets the OAuthAccessToken
    /// </summary>
    public string OAuthAccessToken { get; set; }

    /// <summary>
    ///     Gets or sets the provider
    /// </summary>
    public string ProviderSystemName { get; set; }

    /// <summary>
    ///     Gets or sets the store of the linked customer when customers are registered per store; empty otherwise
    /// </summary>
    public string StoreId { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the link was made by the e-mail address alone, without the account's password
    /// </summary>
    public bool LinkedByEmail { get; set; }
}