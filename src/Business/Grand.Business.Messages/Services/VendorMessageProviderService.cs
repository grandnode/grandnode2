using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Messages;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Mediator;
using Grand.SharedKernel.Extensions;
using System.Net;

namespace Grand.Business.Messages.Services;

public class VendorMessageProviderService : IVendorMessageProviderService
{
    #region Ctor

    public VendorMessageProviderService(
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
    ///     Sends 'New vendor account submitted' message to a store owner
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="vendor">Vendor</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendNewVendorAccountApplyStoreOwnerMessage(Customer customer, Vendor vendor,
        Store store, string languageId, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(vendor);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("VendorAccountApply.StoreOwnerNotification", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator).AddStoreTokens(store, language, emailAccount)
            .AddCustomerTokens(customer, store, host, language)
            .AddVendorTokens(vendor, language);

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
    ///     Sends 'Vendor information changed' message to a store owner
    /// </summary>
    /// <param name="vendor">Vendor</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendVendorInformationChangeMessage(Vendor vendor, Store store, string languageId)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("VendorInformationChange.StoreOwnerNotification", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddVendorTokens(vendor, language);
        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = emailAccount.Email;
        var toName = emailAccount.DisplayName;

        return await _messageProviderService.SendNotification(messageTemplate, emailAccount, languageId, liquidObject, toEmail, toName,
            reference: Reference.Vendor, objectId: vendor.Id);
    }

    /// <summary>
    ///     Sends a vendor review notification message to a store owner
    /// </summary>
    /// <param name="vendorReview">Vendor review</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendVendorReviewMessage(VendorReview vendorReview, Store store,
        string languageId, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(vendorReview);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("Vendor.VendorReview", store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);
        //customer
        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = vendorReview.CustomerId });
        //vendor
        var vendor = await _mediator.Send(new GetVendorByIdQuery { Id = vendorReview.VendorId });

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddVendorReviewTokens(vendor, vendorReview);
        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        builder.AddVendorTokens(vendor, language);
        var liquidObject = await builder.BuildAsync();

        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = vendor.Email;
        var toName = vendor.Name;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Customer, objectId: customer?.Id);
    }

    /// <summary>
    ///     Sends "contact vendor" message
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="vendor">Vendor</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="senderEmail">Sender email</param>
    /// <param name="senderName">Sender name</param>
    /// <param name="subject">Email subject. Pass null if you want a message template subject to be used.</param>
    /// <param name="body">Email body</param>
    /// <param name="ipaddress">Ip address</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendContactVendorMessage(Customer customer, Store store, Vendor vendor,
        string languageId, string senderEmail,
        string senderName, string subject, string body, string ipaddress, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("Service.ContactVendor", store.Id);
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
        liquidObject.ContactUs = new LiquidContactUs(senderEmail, senderName, body, "");
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = vendor.Email;
        var toName = vendor.Name;

        //store in database
        if (_commonSettings.StoreInDatabaseContactUsForm)
            await _mediator.Send(new InsertContactUsCommand {
                CustomerId = customer.Id,
                StoreId = store.Id,
                VendorId = vendor.Id,
                Email = senderEmail,
                Enquiry = body,
                RemoteIpAddress = ipaddress,
                FullName = senderName,
                Subject = string.IsNullOrEmpty(subject) ? "Contact Us (form)" : subject,
                EmailAccountId = emailAccount.Id
            });

        return await _messageProviderService.SendNotification(messageTemplate, emailAccount, languageId, liquidObject, toEmail, toName,
            fromEmail: fromEmail,
            fromName: fromName,
            subject: subject,
            replyToEmailAddress: senderEmail,
            replyToName: senderName,
            reference: Reference.Vendor, objectId: vendor.Id);
    }

    #endregion
}
