using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;

namespace Grand.Business.Core.Interfaces.Messages;

public interface IOrderMessageProviderService
{
    /// <summary>
    ///     Sends an order placed notification to a vendor
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="vendor">Vendor instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendOrderPlacedVendorMessage(Order order, Customer customer, Vendor vendor, string languageId,
        DomainHost host = null);

    /// <summary>
    ///     Sends an order placed notification to a store owner
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendOrderPlacedStoreOwnerMessage(Order order, Customer customer, string languageId,
        DomainHost host = null);

    /// <summary>
    ///     Sends an order paid notification to a store owner
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderPaidStoreOwnerMessage(Order order, Customer customer, string languageId);

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
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderPaidCustomerMessage(Order order, Customer customer, string languageId,
        string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null);

    /// <summary>
    ///     Sends an order paid notification to a vendor
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="vendor">Vendor instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderPaidVendorMessage(Order order, Vendor vendor, string languageId);

    /// <summary>
    ///     Sends an order placed notification to a customer
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="attachmentFilePath">Attachment file path</param>
    /// <param name="attachmentFileName">
    ///     Attachment file name. If specified, then this file name will be sent to a recipient.
    ///     Otherwise, "AttachmentFilePath" name will be used.
    /// </param>
    /// <param name="attachments">Attachments ident</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendOrderPlacedCustomerMessage(Order order, Customer customer, string languageId,
        string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null,
        DomainHost host = null);

    /// <summary>
    ///     Sends a shipment sent notification to a customer
    /// </summary>
    /// <param name="shipment">Shipment</param>
    /// <param name="order">Order</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendShipmentSentCustomerMessage(Shipment shipment, Order order);

    /// <summary>
    ///     Sends a shipment delivered notification to a customer
    /// </summary>
    /// <param name="shipment">Shipment</param>
    /// <param name="order">Order</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendShipmentDeliveredCustomerMessage(Shipment shipment, Order order);

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
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderCompletedCustomerMessage(Order order, Customer customer, string languageId,
        string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null);

    /// <summary>
    ///     Sends an order cancelled notification to a customer
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderCancelledCustomerMessage(Order order, Customer customer, string languageId);

    /// <summary>
    ///     Sends an order cancelled notification to a store owner
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="customer">Customer instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderCancelledStoreOwnerMessage(Order order, Customer customer, string languageId);

    /// <summary>
    ///     Sends an order cancel notification to a vendor
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="vendor">Vendor instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderCancelledVendorMessage(Order order, Vendor vendor, string languageId);

    /// <summary>
    ///     Sends an order refunded notification to a store owner
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="refundedAmount">Amount refunded</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderRefundedStoreOwnerMessage(Order order, double refundedAmount, string languageId);

    /// <summary>
    ///     Sends an order refunded notification to a customer
    /// </summary>
    /// <param name="order">Order instance</param>
    /// <param name="refundedAmount">Amount refunded</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderRefundedCustomerMessage(Order order, double refundedAmount, string languageId);

    /// <summary>
    ///     Sends a new order note added notification to a customer
    /// </summary>
    /// <param name="order">Order</param>
    /// <param name="orderNote">Order note</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendNewOrderNoteAddedCustomerMessage(Order order, OrderNote orderNote, DomainHost host = null);

    /// <summary>
    ///     Sends a gift voucher notification
    /// </summary>
    /// <param name="giftVoucher">Gift voucher</param>
    /// <param name="order">Order</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendGiftVoucherMessage(GiftVoucher giftVoucher, Order order, string languageId);
}
