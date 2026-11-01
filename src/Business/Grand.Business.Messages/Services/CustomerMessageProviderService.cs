using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
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
    ///     Send an order approval message; the recipient also provides the customer tokens
    /// </summary>
    protected virtual async Task<int> SendOrderApprovalMessage(OrderApproval approval, Customer buyer,
        Customer recipient, Store store, string languageId, string templateName, DomainHost host,
        string toEmail = null)
    {
        ArgumentNullException.ThrowIfNull(approval);
        ArgumentNullException.ThrowIfNull(buyer);
        ArgumentNullException.ThrowIfNull(recipient);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(templateName, store.Id);
        if (messageTemplate == null)
            return 0;

        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddCustomerTokens(recipient, store, host, language)
            .AddOrderApprovalTokens(approval, buyer, store, host, language);

        var liquidObject = await builder.BuildAsync();
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail ?? recipient.Email, recipient.GetFullName(), reference: Reference.Customer, objectId: buyer.Id);
    }

    /// <summary>
    ///     Sends an 'Order approval requested' message to the approver
    /// </summary>
    /// <param name="approval">Order approval</param>
    /// <param name="buyer">Customer who asked for the approval</param>
    /// <param name="approver">Customer who decides; the recipient</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendOrderApprovalRequestedMessage(OrderApproval approval, Customer buyer,
        Customer approver, Store store, string languageId, DomainHost host = null)
    {
        return await SendOrderApprovalMessage(approval, buyer, approver, store, languageId,
            MessageTemplateNames.OrderApprovalRequested, host);
    }

    /// <summary>
    ///     Sends an 'Order approval decided' message to the buyer
    /// </summary>
    /// <param name="approval">Order approval</param>
    /// <param name="buyer">Customer who asked for the approval; the recipient</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendOrderApprovalDecidedMessage(OrderApproval approval, Customer buyer,
        Store store, string languageId, DomainHost host = null)
    {
        return await SendOrderApprovalMessage(approval, buyer, buyer, store, languageId,
            MessageTemplateNames.OrderApprovalDecided, host);
    }

    /// <summary>
    ///     Sends an 'Order approval decided' message to the given address, for a buyer who has none of their own
    /// </summary>
    /// <param name="approval">Order approval</param>
    /// <param name="buyer">Customer who asked for the approval</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    /// <param name="toEmail">Recipient</param>
    public virtual async Task<int> SendOrderApprovalDecidedMessage(OrderApproval approval, Customer buyer,
        Store store, string languageId, DomainHost host, string toEmail)
    {
        return await SendOrderApprovalMessage(approval, buyer, buyer, store, languageId,
            MessageTemplateNames.OrderApprovalDecided, host, toEmail);
    }

    /// <summary>
    ///     Sends an 'Order approval requested' message to the store, when the store decides the request
    /// </summary>
    /// <param name="approval">Order approval</param>
    /// <param name="buyer">Customer who placed the order</param>
    /// <param name="store">Store</param>
    /// <param name="toEmail">Recipient; empty sends it to the e-mail account of the message template</param>
    /// <param name="toName">Recipient name</param>
    /// <param name="panelArea">The back office panel the recipient works in</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendOrderApprovalStaffMessage(OrderApproval approval, Customer buyer, Store store,
        string toEmail, string toName, string panelArea, string languageId)
    {
        ArgumentNullException.ThrowIfNull(approval);
        ArgumentNullException.ThrowIfNull(buyer);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(
            MessageTemplateNames.OrderApprovalStaffRequested, store.Id);
        if (messageTemplate == null)
            return 0;

        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddOrderApprovalTokens(approval, buyer, store, store.LinkHost(null), language, panelArea);

        var liquidObject = await builder.BuildAsync();
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var hasRecipient = !string.IsNullOrWhiteSpace(toEmail);
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            hasRecipient ? toEmail : emailAccount.Email, hasRecipient ? toName : emailAccount.DisplayName,
            reference: Reference.Order, objectId: approval.OrderId);
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

        var store = await _messageLookupService.GetStore(customer.StoreId);
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
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

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
