using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
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
using Microsoft.Extensions.Logging;

namespace Grand.Business.Messages.Services;

public class OrderMessageProviderService : IOrderMessageProviderService
{
    #region Ctor

    public OrderMessageProviderService(
        IMessageLookupService messageLookupService,
        IMessageProviderService messageProviderService,
        IDownloadService downloadService,
        IMediator mediator,
        ILogger<OrderMessageProviderService> logger)
    {
        _messageLookupService = messageLookupService;
        _messageProviderService = messageProviderService;
        _downloadService = downloadService;
        _mediator = mediator;
        _logger = logger;
    }

    #endregion

    #region Fields

    private readonly IMessageLookupService _messageLookupService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IDownloadService _downloadService;
    private readonly IMediator _mediator;
    private readonly ILogger<OrderMessageProviderService> _logger;

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
    /// <param name="invoiceDownloadId">
    ///     Download of the invoice to attach. The queued email takes ownership of it; it is deleted when no email is queued
    /// </param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    public virtual async Task<int> SendOrderPlacedCustomerMessage(Order order, Customer customer, string languageId,
        string invoiceDownloadId = null, DomainHost host = null)
    {
        return await SendOrderInvoiceMessage(MessageTemplateNames.SendOrderPlacedCustomerMessage, order, customer,
            languageId, invoiceDownloadId, host);
    }

    /// <summary>
    ///     Sends an order paid notification to a customer
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="invoiceDownloadId">
    ///     Download of the invoice to attach. The queued email takes ownership of it; it is deleted when no email is queued
    /// </param>
    public virtual async Task<int> SendOrderPaidCustomerMessage(Order order, Customer customer, string languageId,
        string invoiceDownloadId = null)
    {
        return await SendOrderInvoiceMessage(MessageTemplateNames.SendOrderPaidCustomerMessage, order, customer,
            languageId, invoiceDownloadId);
    }

    /// <summary>
    ///     Sends an order completed notification to a customer
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="invoiceDownloadId">
    ///     Download of the invoice to attach. The queued email takes ownership of it; it is deleted when no email is queued
    /// </param>
    public virtual async Task<int> SendOrderCompletedCustomerMessage(Order order, Customer customer, string languageId,
        string invoiceDownloadId = null)
    {
        return await SendOrderInvoiceMessage(MessageTemplateNames.SendOrderCompletedCustomerMessage, order, customer,
            languageId, invoiceDownloadId);
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
    ///     Sends an order notification with an invoice to a customer
    /// </summary>
    private async Task<int> SendOrderInvoiceMessage(string message, Order order, Customer customer, string languageId,
        string invoiceDownloadId, DomainHost host = null)
    {
        var queuedEmails = 0;
        try
        {
            queuedEmails = await SendOrderCustomerMessage(message, order, customer, languageId, invoiceDownloadId,
                host: host);
            return queuedEmails;
        }
        finally
        {
            //inactive template, no recipient or an error - no email owns the invoice, so nothing would ever delete it
            if (queuedEmails == 0)
                await DeleteUnusedInvoice(invoiceDownloadId);
        }
    }

    private async Task DeleteUnusedInvoice(string invoiceDownloadId)
    {
        try
        {
            await _downloadService.DeleteDownloadById(invoiceDownloadId);
        }
        catch (Exception ex)
        {
            //a broken file must not stop the rest of the order processing
            _logger.LogError(ex, "Error deleting the invoice download {InvoiceDownloadId}", invoiceDownloadId);
        }
    }

    /// <summary>
    ///     Sends an order notification to a customer
    /// </summary>
    /// <param name="message">Message template</param>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="invoiceDownloadId">Download of the invoice to attach</param>
    /// <param name="refundedAmount"></param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    private async Task<int> SendOrderCustomerMessage(string message, Order order, Customer customer, string languageId,
        string invoiceDownloadId = null, double refundedAmount = 0, DomainHost host = null)
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
            reference: Reference.Order, objectId: order.Id, invoiceDownloadId: invoiceDownloadId);
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
