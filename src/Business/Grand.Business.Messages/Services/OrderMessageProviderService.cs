using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Messages;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Mediator;

namespace Grand.Business.Messages.Services;

public class OrderMessageProviderService : IOrderMessageProviderService
{
    #region Ctor

    public OrderMessageProviderService(
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
    ///     Sends an order placed notification to a store owner
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendOrderPlacedStoreOwnerMessage(Order order, Customer customer, string languageId,
        DomainHost host = null)
    {
        return await SendOrderStoreOwnerMessage(MessageTemplateNames.SendOrderPlacedStoreOwnerMessage, order, customer,
            languageId, host: host);
    }

    /// <summary>
    ///     Sends an order paid notification to a store owner
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendOrderPaidStoreOwnerMessage(Order order, Customer customer, string languageId)
    {
        return await SendOrderStoreOwnerMessage(MessageTemplateNames.SendOrderPaidStoreOwnerMessage, order, customer,
            languageId);
    }

    /// <summary>
    ///     Sends an order cancelled notification to an admin
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    public virtual async Task<int> SendOrderCancelledStoreOwnerMessage(Order order, Customer customer,
        string languageId)
    {
        return await SendOrderStoreOwnerMessage(MessageTemplateNames.SendOrderCancelledStoreOwnerMessage, order,
            customer, languageId);
    }

    /// Sends an order refunded notification to a store owner
    /// <param name="order">Order instance</param>
    /// <param name="refundedAmount">Amount refunded</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendOrderRefundedStoreOwnerMessage(Order order, double refundedAmount,
        string languageId)
    {
        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = order.CustomerId });
        return await SendOrderStoreOwnerMessage(MessageTemplateNames.SendOrderRefundedStoreOwnerMessage, order,
            customer, languageId, refundedAmount);
    }

    /// <summary>
    ///     Sends an order notification to a store owner
    /// </summary>
    /// <param name="template">Message template</param>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="refundedAmount"></param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    private async Task<int> SendOrderStoreOwnerMessage(string template, Order order, Customer customer,
        string languageId, double refundedAmount = 0, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(order);

        var store = await _messageLookupService.GetStore(order.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(template, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var liquidBuilder = new LiquidObjectBuilder(_mediator);
        liquidBuilder.AddStoreTokens(store, language, emailAccount)
            .AddOrderTokens(order, customer, store, host);
        liquidBuilder.AddCustomerTokensIfNotNull(customer, store, host, language);

        var liquidObject = await liquidBuilder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = emailAccount.Email;
        var toName = emailAccount.DisplayName;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Order, objectId: order.Id);
    }

    /// <summary>
    ///     Sends an order placed notification to a customer
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer"></param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="attachmentFilePath">Attachment file path</param>
    /// <param name="attachmentFileName">
    ///     Attachment file name. If specified, then this file name will be sent to a recipient.
    ///     Otherwise, "AttachmentFilePath" name will be used.
    /// </param>
    /// <param name="attachments">Attachments</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendOrderPlacedCustomerMessage(Order order, Customer customer, string languageId,
        string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null,
            DomainHost host = null)
    {
        return await SendOrderCustomerMessage(MessageTemplateNames.SendOrderPlacedCustomerMessage, order, customer,
            languageId, attachmentFilePath, attachmentFileName, attachments, host: host);
    }

    /// <summary>
    ///     Sends an order paid notification to a customer
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="attachmentFilePath">Attachment file path</param>
    /// <param name="attachmentFileName">
    ///     Attachment file name. If specified, then this file name will be sent to a recipient.
    ///     Otherwise, "AttachmentFilePath" name will be used.
    /// </param>
    /// <param name="attachments">Attachments ident</param>
    public virtual async Task<int> SendOrderPaidCustomerMessage(Order order, Customer customer, string languageId,
        string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null)
    {
        return await SendOrderCustomerMessage(MessageTemplateNames.SendOrderPaidCustomerMessage, order, customer,
            languageId, attachmentFilePath, attachmentFileName, attachments);
    }

    /// <summary>
    ///     Sends an order completed notification to a customer
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="attachmentFilePath">Attachment file path</param>
    /// <param name="attachmentFileName">
    ///     Attachment file name. If specified, then this file name will be sent to a recipient.
    ///     Otherwise, "AttachmentFilePath" name will be used.
    /// </param>
    /// <param name="attachments">Attachments ident</param>
    public virtual async Task<int> SendOrderCompletedCustomerMessage(Order order, Customer customer, string languageId,
        string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null)
    {
        return await SendOrderCustomerMessage(MessageTemplateNames.SendOrderCompletedCustomerMessage, order, customer,
            languageId, attachmentFilePath, attachmentFileName, attachments);
    }

    /// <summary>
    ///     Sends an order cancelled notification to a customer
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendOrderCancelledCustomerMessage(Order order, Customer customer, string languageId)
    {
        return await SendOrderCustomerMessage(MessageTemplateNames.SendOrderCancelledCustomerMessage, order, customer,
            languageId);
    }

    /// <summary>
    ///     Sends an order refunded notification to a customer
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="refundedAmount">Amount refunded</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    public virtual async Task<int> SendOrderRefundedCustomerMessage(Order order, double refundedAmount,
        string languageId)
    {
        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = order.CustomerId });
        return await SendOrderCustomerMessage(MessageTemplateNames.SendOrderRefundedCustomerMessage, order, customer,
            languageId, refundedAmount: refundedAmount);
    }

    /// <summary>
    ///     Sends an order notification to a customer
    /// </summary>
    /// <param name="message">Message template</param>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="attachmentFilePath">Attachment file path</param>
    /// <param name="attachmentFileName">
    ///     Attachment file name. If specified, then this file name will be sent to a recipient.
    ///     Otherwise, "AttachmentFilePath" name will be used.
    /// </param>
    /// <param name="attachments">Attachments ident</param>
    /// <param name="refundedAmount"></param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    private async Task<int> SendOrderCustomerMessage(string message, Order order, Customer customer, string languageId,
        string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null,
        double refundedAmount = 0, DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(order);

        var store = await _messageLookupService.GetStore(order.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(message, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddOrderTokens(order, customer, store, host);
        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = order.BillingAddress.Email;
        var toName = $"{order.BillingAddress.FirstName} {order.BillingAddress.LastName}";
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            attachmentFilePath,
            attachmentFileName,
            attachments,
            reference: Reference.Order, objectId: order.Id);
    }

    /// <summary>
    ///     Sends an order placed notification to a vendor
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="vendor">Vendor instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendOrderPlacedVendorMessage(Order order, Customer customer, Vendor vendor,
        string languageId, DomainHost host = null)
    {
        return await SendOrderVendorMessage(MessageTemplateNames.SendOrderPlacedVendorMessage, order, vendor,
            languageId, host: host);
    }

    /// <summary>
    ///     Sends an order paid notification to a vendor
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="vendor">Vendor instance</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendOrderPaidVendorMessage(Order order, Vendor vendor, string languageId)
    {
        return await SendOrderVendorMessage(MessageTemplateNames.SendOrderPaidVendorMessage, order, vendor, languageId);
    }

    /// <summary>
    ///     Sends an order cancel notification to a vendor
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="vendor">Vendor instance</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendOrderCancelledVendorMessage(Order order, Vendor vendor, string languageId)
    {
        return await SendOrderVendorMessage(MessageTemplateNames.SendOrderCancelledVendorMessage, order, vendor,
            languageId);
    }

    /// <summary>
    ///     Sends an order notification to a vendor
    /// </summary>
    /// <param name="message">Message template</param>
    /// <param name="order">Order instance</param>
    /// <param name="vendor">Vendor instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    private async Task<int> SendOrderVendorMessage(string message, Order order, Vendor vendor, string languageId,
        DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(vendor);

        var store = await _messageLookupService.GetStore(order.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(message, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = order.CustomerId });

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddOrderTokens(order, customer, store, host, vendor: vendor);

        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = vendor.Email;
        var toName = vendor.Name;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName,
            reference: Reference.Order, objectId: order.Id);
    }

    /// <summary>
    ///     Sends a shipment sent notification to a customer
    /// </summary>
    /// <param name="shipment">Shipment</param>
    /// <param name="order">Order</param>
    public virtual async Task<int> SendShipmentSentCustomerMessage(Shipment shipment, Order order)
    {
        return await SendShipmentCustomerMessage(MessageTemplateNames.SendShipmentSentCustomerMessage, shipment, order);
    }

    /// <summary>
    ///     Sends a shipment delivered notification to a customer
    /// </summary>
    /// <param name="shipment">Shipment</param>
    /// <param name="order">Order</param>
    public virtual async Task<int> SendShipmentDeliveredCustomerMessage(Shipment shipment, Order order)
    {
        return await SendShipmentCustomerMessage(MessageTemplateNames.SendShipmentDeliveredCustomerMessage, shipment,
            order);
    }

    /// <summary>
    ///     Send a shipment notification to a customer
    /// </summary>
    /// <param name="message">Message template</param>
    /// <param name="shipment">Shipment</param>
    /// <param name="order">Order</param>
    private async Task<int> SendShipmentCustomerMessage(string message, Shipment shipment, Order order)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        if (order == null)
            throw new Exception("Order cannot be loaded");

        var store = await _messageLookupService.GetStore(order.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(order.CustomerLanguageId, store.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate(message, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = order.CustomerId });

        var host = store.PrimaryHost();
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddShipmentTokens(shipment, order, store, host, language)
            .AddOrderTokens(order, customer, store, host);

        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = order.BillingAddress.Email;
        var toName = $"{order.BillingAddress.FirstName} {order.BillingAddress.LastName}";
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            language.Id, liquidObject,
            toEmail, toName,
            reference: Reference.Shipment, objectId: shipment.Id);
    }

    /// <summary>
    ///     Sends a new order note added notification to a customer
    /// </summary>
    /// <param name="order">Order</param>
    /// <param name="orderNote">Order note</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendNewOrderNoteAddedCustomerMessage(Order order, OrderNote orderNote,
        DomainHost host = null)
    {
        ArgumentNullException.ThrowIfNull(orderNote);
        ArgumentNullException.ThrowIfNull(order);

        var store = await _messageLookupService.GetStore(order.StoreId);
        var language = await _messageLookupService.EnsureLanguageIsActive(order.CustomerLanguageId, store.Id);

        var messageTemplate =
            await _messageLookupService.GetMessageTemplate(MessageTemplateNames.SendNewOrderNoteAddedCustomerMessage, store.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = order.CustomerId });

        host = store.LinkHost(host);
        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddOrderTokens(order, customer, store, host, orderNote);

        builder.AddCustomerTokensIfNotNull(customer, store, host, language);

        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);

        var toEmail = order.BillingAddress.Email;
        var toName = $"{order.BillingAddress.FirstName} {order.BillingAddress.LastName}";
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            language.Id, liquidObject,
            toEmail, toName,
            reference: Reference.Order, objectId: order.Id);
    }

    /// <summary>
    ///     Sends a gift voucher notification
    /// </summary>
    /// <param name="giftVoucher">Gift voucher</param>
    /// <param name="order">Order</param>
    /// <param name="languageId">Message language identifier</param>
    public virtual async Task<int> SendGiftVoucherMessage(GiftVoucher giftVoucher, Order order, string languageId)
    {
        ArgumentNullException.ThrowIfNull(giftVoucher);

        var store = await _messageLookupService.GetStore(order?.StoreId);

        var language = await _messageLookupService.EnsureLanguageIsActive(languageId, store?.Id);

        var messageTemplate = await _messageLookupService.GetMessageTemplate("GiftVoucher.Notification", store?.Id);
        if (messageTemplate == null)
            return 0;

        //email account
        var emailAccount = await _messageLookupService.GetEmailAccountOfMessageTemplate(messageTemplate, language.Id);

        var builder = new LiquidObjectBuilder(_mediator);
        builder.AddStoreTokens(store, language, emailAccount)
            .AddGiftVoucherTokens(giftVoucher, language);
        var liquidObject = await builder.BuildAsync();
        //event notification
        await _mediator.MessageTokensAdded(messageTemplate, liquidObject);
        var toEmail = giftVoucher.RecipientEmail;
        var toName = giftVoucher.RecipientName;
        return await _messageProviderService.SendNotification(messageTemplate, emailAccount,
            languageId, liquidObject,
            toEmail, toName);
    }

    #endregion
}
