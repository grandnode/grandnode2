using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Services;

public class NewsletterMessageProviderService : INewsletterMessageProviderService
{
    #region Ctor

    public NewsletterMessageProviderService(
        IMessageLookupService messageLookupService,
        IMessageProviderService messageProviderService,
        IMediator mediator)
    {
        _messageLookupService = messageLookupService;
        _messageProviderService = messageProviderService;
        _mediator = mediator;
    }

    #endregion

    #region Fields

    private readonly IMessageLookupService _messageLookupService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IMediator _mediator;

    #endregion

    #region Methods

    /// <summary>
    ///     Sends a newsletter subscription activation message
    /// </summary>
    /// <param name="subscription">Newsletter subscription</param>
    /// <param name="languageId">Language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendNewsLetterSubscriptionActivationMessage(NewsLetterSubscription subscription,
        string languageId, DomainHost host = null)
    {
        return await SendNewsLetterSubscriptionMessage(MessageTemplateNames.SendNewsLetterSubscriptionActivationMessage,
            subscription, languageId, host: host);
    }

    /// <summary>
    ///     Sends a newsletter subscription deactivation message
    /// </summary>
    /// <param name="subscription">Newsletter subscription</param>
    /// <param name="languageId">Language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendNewsLetterSubscriptionDeactivationMessage(NewsLetterSubscription subscription,
        string languageId, DomainHost host = null)
    {
        return await SendNewsLetterSubscriptionMessage(
            MessageTemplateNames.SendNewsLetterSubscriptionDeactivationMessage, subscription, languageId, host: host);
    }

    /// <summary>
    ///     Send a newsletter subscription message
    /// </summary>
    /// <param name="message">Message template</param>
    /// <param name="subscription">Newsletter subscription</param>
    /// <param name="languageId">Language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    private async Task<int> SendNewsLetterSubscriptionMessage(string message, NewsLetterSubscription subscription,
        string languageId, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var store = await _messageLookupService.GetStore(subscription.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(message, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddNewsLetterSubscriptionTokens(subscription, store, host);

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = subscription.Email;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, string.Empty,
            reference: Reference.Customer, objectId: subscription.CustomerId);
    }

    #endregion
}
