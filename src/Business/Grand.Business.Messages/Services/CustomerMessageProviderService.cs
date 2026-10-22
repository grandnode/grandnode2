using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Mediator;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Messages.Services;

public class CustomerMessageProviderService : ICustomerMessageProviderService
{
    #region Ctor

    public CustomerMessageProviderService(
        IMessageLookupService messageLookupService,
        IMessageProviderService messageProviderService,
        IMediator mediator,
        CommonSettings commonSettings)
    {
        _messageLookupService = messageLookupService;
        _messageProviderService = messageProviderService;
        _mediator = mediator;
        _commonSettings = commonSettings;
    }

    #endregion

    #region Fields

    private readonly IMessageLookupService _messageLookupService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IMediator _mediator;
    private readonly CommonSettings _commonSettings;

    #endregion

    #region Methods

    /// <summary>
    ///     Send a message to a customer
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="templateName">Message template name</param>
    /// <param name="toEmailAccount">Send email to email account</param>
    /// <param name="customerNote">Customer note</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    protected virtual async Task<int> SendCustomerMessage(Customer customer, Store store, string languageId,
        string templateName, bool toEmailAccount = false, CustomerNote customerNote = null, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(templateName, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddCustomerTokens(customer, store, host, language, customerNote);

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = toEmailAccount ? emailAccount.Email : customer.Email;
        var toName = toEmailAccount ? emailAccount.DisplayName : customer.GetFullName();
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName, reference: Reference.Customer, objectId: customer.Id);
    }

    /// <summary>
    ///     Sends 'New customer' notification message to a store owner
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store identifier</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendCustomerRegisteredMessage(Customer customer, Store store, string languageId,
        DomainHost host = null)
    {
        return await SendCustomerMessage(customer, store, languageId, MessageTemplateNames.CustomerRegistered, true,
            host: host);
    }

    /// <summary>
    ///     Sends a welcome message to a customer
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendCustomerWelcomeMessage(Customer customer, Store store, string languageId,
        DomainHost host = null)
    {
        return await SendCustomerMessage(customer, store, languageId, MessageTemplateNames.CustomerWelcome, host: host);
    }

    /// <summary>
    ///     Sends an email validation message to a customer
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendCustomerEmailValidationMessage(Customer customer, Store store, string languageId,
        DomainHost host = null)
    {
        return await SendCustomerMessage(customer, store, languageId, MessageTemplateNames.CustomerEmailValidation,
            host: host);
    }

    /// <summary>
    ///     Sends password recovery message to a customer
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendCustomerPasswordRecoveryMessage(Customer customer, Store store,
        string languageId, DomainHost host = null)
    {
        return await SendCustomerMessage(customer, store, languageId, MessageTemplateNames.CustomerPasswordRecovery,
            host: host);
    }

    /// <summary>
    ///     Sends a new customer note added notification to a customer
    /// </summary>
    /// <param name="customerNote">Customer note</param>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    public virtual async Task<int> SendNewCustomerNoteMessage(CustomerNote customerNote, Customer customer, Store store,
        string languageId)
    {
        return await SendCustomerMessage(customer, store, languageId, MessageTemplateNames.CustomerNewCustomerNote,
            customerNote: customerNote);
    }

    /// <summary>
    ///     Send an email token validation message to a customer
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendCustomerEmailTokenValidationMessage(Customer customer, Store store,
        string languageId, DomainHost host = null)
    {
        return await SendCustomerMessage(customer, store, languageId,
            MessageTemplateNames.CustomerEmailTokenValidationMessage, host: host);
    }

    /// <summary>
    ///     Sends a "new VAT" notification to a store owner
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendCustomerDeleteStoreOwnerMessage(Customer customer, string languageId,
        DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var store = await _messageLookupService.GetStore("");
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("CustomerDelete.StoreOwnerNotification", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddCustomerTokens(customer, store, host, language);

        var liquidObject = await builder.BuildAsync();

        var toEmail = emailAccount.Email;
        var toName = emailAccount.DisplayName;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Customer, objectId: customer.Id);
    }

    /// <summary>
    ///     Sends "contact us" message
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="senderEmail">Sender email</param>
    /// <param name="senderName">Sender name</param>
    /// <param name="subject">Email subject. Pass null if you want a message template subject to be used.</param>
    /// <param name="body">Email body</param>
    /// <param name="attrInfo">Attr info</param>
    /// <param name="customAttributes">CustomAttributes</param>
    /// <param name="ipaddress">Ip address</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendContactUsMessage(Customer customer, Store store, string languageId,
        string senderEmail,
        string senderName, string subject, string body, string attrInfo, IList<CustomAttribute> customAttributes,
        string ipaddress, DomainHost host = null)
    {
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);
        var messageTemplate = await _messageLookupService.GetMessageTemplate("Service.ContactUs", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        string fromEmail;
        string fromName;
        senderName = WebUtility.HtmlEncode(senderName);
        senderEmail = WebUtility.HtmlEncode(senderEmail);
        //required for some SMTP servers
        if (_commonSettings.UseSystemEmailForContactUsForm)
        {
            fromEmail = emailAccount.Email;
            fromName = emailAccount.DisplayName;
        }
        else
        {
            fromEmail = senderEmail;
            fromName = senderName;
        }

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddCustomerTokens(customer, store, host, language);

        var liquidObject = await builder.BuildAsync();
        liquidObject.ContactUs = new LiquidContactUs(senderEmail, senderName, body, attrInfo);
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = emailAccount.Email;
        var toName = emailAccount.DisplayName;

        //store in database
        if (_commonSettings.StoreInDatabaseContactUsForm)
            await _mediator.Send(new InsertContactUsCommand {
                CustomerId = customer.Id,
                StoreId = store.Id,
                VendorId = "",
                Email = senderEmail,
                Enquiry = body,
                FullName = senderName,
                Subject = string.IsNullOrEmpty(subject) ? "Contact Us (form)" : subject,
                ContactAttributeDescription = attrInfo,
                ContactAttributes = customAttributes,
                EmailAccountId = emailAccount.Id,
                RemoteIpAddress = ipaddress
            });
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount, languageId, liquidObject, toEmail, toName,
            fromEmail: fromEmail,
            fromName: fromName,
            subject: subject,
            replyToEmailAddress: senderEmail,
            replyToName: senderName, reference: Reference.Customer, objectId: customer?.Id);
    }

    #endregion
}
