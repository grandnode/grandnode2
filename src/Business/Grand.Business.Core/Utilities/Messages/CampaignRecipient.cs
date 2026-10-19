using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Domain.Tax;

namespace Grand.Business.Core.Utilities.Messages;

/// <summary>
///     The recipient of a newsletter campaign and the context its message is rendered in
/// </summary>
public sealed record CampaignRecipient
{
    /// <summary>
    ///     Store whose tokens are rendered
    /// </summary>
    public required Store Store { get; init; }

    /// <summary>
    ///     Language
    /// </summary>
    public required Language Language { get; init; }

    /// <summary>
    ///     Email account
    /// </summary>
    public required EmailAccount EmailAccount { get; init; }

    /// <summary>
    ///     Store address used for the links in the email
    /// </summary>
    public required DomainHost Host { get; init; }

    /// <summary>
    ///     Recipient's customer account; null renders no customer and shopping cart tokens
    /// </summary>
    public Customer Customer { get; init; }

    /// <summary>
    ///     Currency the shopping cart prices are rendered in
    /// </summary>
    public Currency Currency { get; init; }

    /// <summary>
    ///     Whether the shopping cart prices include tax
    /// </summary>
    public TaxDisplayType TaxDisplayType { get; init; }

    /// <summary>
    ///     Newsletter subscription; null renders no subscription tokens
    /// </summary>
    public NewsLetterSubscription Subscription { get; init; }

    /// <summary>
    ///     Identifier of the queued email the message is sent as; null renders no email tokens
    /// </summary>
    public string QueuedEmailId { get; init; }
}
