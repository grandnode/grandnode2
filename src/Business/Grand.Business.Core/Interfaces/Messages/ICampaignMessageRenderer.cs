using Grand.Business.Core.Utilities.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Domain.Tax;

namespace Grand.Business.Core.Interfaces.Messages;

/// <summary>
///     Renders the subject and body of a newsletter campaign with the message tokens
/// </summary>
public interface ICampaignMessageRenderer
{
    /// <summary>
    ///     Renders the campaign for one recipient
    /// </summary>
    /// <param name="campaign">Campaign</param>
    /// <param name="store">Store whose tokens are rendered</param>
    /// <param name="language">Language</param>
    /// <param name="emailAccount">Email account</param>
    /// <param name="host">Store address used for the links in the email</param>
    /// <param name="customer">Recipient's customer account; null renders no customer and shopping cart tokens</param>
    /// <param name="currency">Currency the shopping cart prices are rendered in</param>
    /// <param name="taxDisplayType">Whether the shopping cart prices include tax</param>
    /// <param name="subscription">Newsletter subscription; null renders no subscription tokens</param>
    /// <param name="queuedEmailId">Identifier of the queued email; null renders no email tokens</param>
    Task<CampaignMessage> Render(Campaign campaign, Store store, Language language, EmailAccount emailAccount,
        DomainHost host, Customer customer, Currency currency, TaxDisplayType taxDisplayType,
        NewsLetterSubscription subscription = null, string queuedEmailId = null);
}
