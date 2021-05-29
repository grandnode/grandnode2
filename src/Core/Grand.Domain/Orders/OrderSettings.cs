using Grand.Domain.Configuration;

namespace Grand.Domain.Orders
{
    public class OrderSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether customer can make re-order
        /// </summary>
        public bool IsReOrderAllowed { get; set; }

        /// <summary>
        /// Gets or sets a minimum order subtotal amount
        /// </summary>
        public double MinOrderSubtotalAmount { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether 'inimum order subtotal amount' option
        /// should be evaluated over 'X' value including tax or not
        /// </summary>
        public bool MinOrderSubtotalAmountIncludingTax { get; set; }

        /// <summary>
        /// Gets or sets a minimum order total amount
        /// </summary>
        public double MinOrderTotalAmount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether anonymous checkout allowed
        /// </summary>
        public bool AnonymousCheckoutAllowed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether 'Terms of service' enabled on the shopping cart page
        /// </summary>
        public bool TermsOfServiceOnShoppingCartPage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether 'Terms of service' enabled on the order confirmation page
        /// </summary>
        public bool TermsOfServiceOnOrderConfirmPage { get; set; }       

        /// <summary>
        /// Gets or sets a value indicating whether "Order completed" page should be skipped
        /// </summary>
        public bool DisableOrderCompletedPage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating we should attach PDF invoice to "Order placed" email
        /// </summary>
        public bool AttachPdfInvoiceToOrderPlacedEmail { get; set; }
        /// <summary>
        /// Gets or sets a value indicating we should attach PDF invoice to "Order paid" email
        /// </summary>
        public bool AttachPdfInvoiceToOrderPaidEmail { get; set; }    
        /// <summary>
        /// Gets or sets a value indicating we should attach PDF invoice to "Order completed" email
        /// </summary>
        public bool AttachPdfInvoiceToOrderCompletedEmail { get; set; }
        /// <summary>
        /// Gets or sets a value indicating we should attach PDF invoice to binary field
        /// </summary>
        public bool AttachPdfInvoiceToBinary { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether "Merchandise returns" are allowed
        /// </summary>
        public bool MerchandiseReturnsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether merchandise return pickup address can be specified by customer
        /// </summary>
        public bool MerchandiseReturns_AllowToSpecifyPickupAddress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether merchandise return pickup date can be specified by customer
        /// </summary>
        public bool MerchandiseReturns_AllowToSpecifyPickupDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether merchandise return pickup date has to be specified by customer
        /// </summary>
        public bool MerchandiseReturns_PickupDateRequired { get; set; }

        /// <summary>
        /// Gets or sets a number of days that the Merchandise Return Link will be available for customers after order placing.
        /// </summary>
        public int NumberOfDaysMerchandiseReturnAvailable { get; set; }

        /// <summary>
        ///  Gift vouchers are activated when the order status is
        /// </summary>
        public int GiftVouchers_Activated_OrderStatusId { get; set; }

        /// <summary>
        ///  Gift vouchers are deactivated when the order is canceled
        /// </summary>
        public bool DeactivateGiftVouchersAfterCancelOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to deactivate related gift vouchers after deleting the order
        /// </summary>
        public bool DeactivateGiftVouchersAfterDeletingOrder { get; set; }
        /// <summary>
        /// Gets or sets an order placement interval in seconds (prevent 2 orders being placed within an X seconds time frame).
        /// </summary>
        public int MinimumOrderPlacementInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an order status should be set to "Complete" only when its shipping status is "Delivered". Otherwise, "Shipped" status will be enough.
        /// </summary>
        public bool CompleteOrderWhenDelivered { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether order status can be marked as cancelled by user (if order isn't paid and shipped yet)
        /// </summary>
        public bool UserCanCancelUnpaidOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether unpublish auction product after made order.
        /// </summary>
        public bool UnpublishAuctionProduct { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether customers can add order notes
        /// </summary>
        public bool AllowCustomerToAddOrderNote { get; set; }

        /// <summary>
        /// Gets or sets a length for order code
        /// </summary>
        public int LengthCode { get; set; }

        /// <summary>
        /// Gets or sets a page size
        /// </summary>
        public int PageSize { get; set; }

    }
}