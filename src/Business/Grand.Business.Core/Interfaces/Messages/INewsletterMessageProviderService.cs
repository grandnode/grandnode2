using Grand.Domain.Messages;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Messages;

public interface INewsletterMessageProviderService
{
    /// <summary>
    ///     Sends a newsletter subscription activation message
    /// </summary>
    /// <param name="subscription">Newsletter subscription</param>
    /// <param name="languageId">Language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendNewsLetterSubscriptionActivationMessage(NewsLetterSubscription subscription,
        string languageId, DomainHost host = null);

    /// <summary>
    ///     Sends a newsletter subscription deactivation message
    /// </summary>
    /// <param name="subscription">Newsletter subscription</param>
    /// <param name="languageId">Language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendNewsLetterSubscriptionDeactivationMessage(NewsLetterSubscription subscription,
        string languageId, DomainHost host = null);
}
