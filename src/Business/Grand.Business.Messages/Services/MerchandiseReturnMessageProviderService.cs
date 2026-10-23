using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Business.Messages.Services;

public class MerchandiseReturnMessageProviderService : IMerchandiseReturnMessageProviderService
{
    #region Ctor

    public MerchandiseReturnMessageProviderService(
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
    ///     Sends 'New Merchandise Return' message to a store owner
    /// </summary>
    /// <param name="merchandiseReturn">Merchandise return</param>
    /// <param name="order">Order</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendNewMerchandiseReturnStoreOwnerMessage(MerchandiseReturn merchandiseReturn,
        Order order, string languageId, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(merchandiseReturn);

        var store = await _messageLookupService.GetStore(order.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate =
            await _messageLookupService.GetMessageTemplate(MessageTemplateNames.SendNewMerchandiseReturnStoreOwnerMessage, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = merchandiseReturn.CustomerId });

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount);

        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        builder.AddMerchandiseReturnTokens(merchandiseReturn, store, host, order, language);

        var liquidObject = await builder.BuildAsync();

        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        if (!string.IsNullOrEmpty(merchandiseReturn.VendorId))
        {
            var vendor = await _mediator.Send(new GetVendorByIdQuery { Id = merchandiseReturn.VendorId });
            if (vendor != null)
            {
                var vendorEmail = vendor.Email;
                var vendorName = vendor.Name;
                await _messageProviderService.SendNotification(messageTemplate, emailAccount,
                    languageId, liquidObject,
                    vendorEmail, vendorName);
            }
        }

        var toEmail = emailAccount.Email;
        var toName = emailAccount.DisplayName;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.MerchandiseReturn, objectId: merchandiseReturn.Id);
    }

    /// <summary>
    ///     Sends 'Merchandise Return status changed' message to a customer
    /// </summary>
    /// <param name="merchandiseReturn">Merchandise return</param>
    /// <param name="order">Order</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendMerchandiseReturnStatusChangedCustomerMessage(
        MerchandiseReturn merchandiseReturn, Order order, string languageId)
    {
        ArgumentNullException.ThrowIfNull(merchandiseReturn);

        var store = await _messageLookupService.GetStore(order.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate =
            await _messageLookupService.GetMessageTemplate(MessageTemplateNames.SendMerchandiseReturnStatusChangedCustomerMessage, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = merchandiseReturn.CustomerId });

        var host = store.PrimaryHost();
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount);

        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        builder.AddMerchandiseReturnTokens(merchandiseReturn, store, host, order, language);
        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = string.IsNullOrEmpty(customer?.Email) ? order.BillingAddress.Email : customer.Email;
        var toName = string.IsNullOrEmpty(customer?.Email) ? order.BillingAddress.FirstName : customer.GetFullName();
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.MerchandiseReturn, objectId: merchandiseReturn.Id);
    }

    /// <summary>
    ///     Sends 'New Merchandise Return' message to a customer
    /// </summary>
    /// <param name="merchandiseReturn">Merchandise return</param>
    /// <param name="order">Order</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendNewMerchandiseReturnCustomerMessage(MerchandiseReturn merchandiseReturn,
        Order order, string languageId, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(merchandiseReturn);

        var store = await _messageLookupService.GetStore(order.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate =
            await _messageLookupService.GetMessageTemplate(MessageTemplateNames.SendNewMerchandiseReturnCustomerMessage, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = merchandiseReturn.CustomerId });

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount);
        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        builder.AddMerchandiseReturnTokens(merchandiseReturn, store, host, order, language);
        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = string.IsNullOrEmpty(customer?.Email) ? order.BillingAddress.Email : customer.Email;
        var toName = string.IsNullOrEmpty(customer?.Email) ? order.BillingAddress.FirstName : customer.GetFullName();
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.MerchandiseReturn, objectId: merchandiseReturn.Id);
    }

    /// <summary>
    ///     Sends a new merchandise return note added notification to a customer
    /// </summary>
    /// <param name="merchandiseReturn">Merchandise return</param>
    /// <param name="merchandiseReturnNote">Merchandise return note</param>
    /// <param name="order">Order</param>
    public virtual async Task<int> SendNewMerchandiseReturnNoteAddedCustomerMessage(MerchandiseReturn merchandiseReturn,
        MerchandiseReturnNote merchandiseReturnNote, Order order)
    {
        ArgumentNullException.ThrowIfNull(merchandiseReturnNote);
        ArgumentNullException.ThrowIfNull(merchandiseReturn);

        var store = await _messageLookupService.GetStore(order.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(order.CustomerLanguageId, store.Id);

        var messageTemplate =
            await _messageLookupService.GetMessageTemplate(MessageTemplateNames.SendNewMerchandiseReturnNoteAddedCustomerMessage, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = merchandiseReturn.CustomerId });

        var host = store.PrimaryHost();
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount);
        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        builder.AddMerchandiseReturnTokens(merchandiseReturn, store, host, order, language,
            merchandiseReturnNote);
        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = string.IsNullOrEmpty(customer?.Email) ? order.BillingAddress.Email : customer.Email;
        var toName = string.IsNullOrEmpty(customer?.Email) ? order.BillingAddress.FirstName : customer.GetFullName();
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            language.Id, liquidObject,
            toEmail, toName,
            reference: Reference.MerchandiseReturn, objectId: merchandiseReturn.Id);
    }

    #endregion
}
