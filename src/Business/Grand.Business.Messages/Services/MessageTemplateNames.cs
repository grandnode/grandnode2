using Grand.Domain.Orders;

namespace Grand.Business.Messages.Services;

public static class MessageTemplateNames
{
    public const string CustomerRegistered = "NewCustomer.Notification";
    public const string CustomerWelcome = "Customer.WelcomeMessage";
    public const string CustomerEmailValidation = "Customer.EmailValidationMessage";
    public const string CustomerPasswordRecovery = "Customer.PasswordRecovery";
    public const string CustomerNewCustomerNote = "Customer.NewCustomerNote";
    public const string CustomerEmailTokenValidationMessage = "Customer.EmailTokenValidationMessage";

    public const string OrderApprovalRequested = "OrderApproval.Requested";
    public const string OrderApprovalDecided = "OrderApproval.Decided";
    public const string OrderApprovalStaffRequested = "OrderApproval.StaffRequested";

    public const string RecurringPaymentCancelledCustomerNotification =
        "RecurringPayment.Cancelled.CustomerNotification";

    public const string RecurringPaymentCancelledStoreOwnerNotification =
        "RecurringPayment.Cancelled.StoreOwnerNotification";

    public const string RecurringPaymentFailedCustomerNotification =
        "RecurringPayment.PaymentFailed.CustomerNotification";

    public const string RecurringPaymentFailedStoreOwnerNotification =
        "RecurringPayment.PaymentFailed.StoreOwnerNotification";

    public const string SendOrderPlacedStoreOwnerMessage = "OrderPlaced.StoreOwnerNotification";
    public const string SendOrderPaidStoreOwnerMessage = "OrderPaid.StoreOwnerNotification";
    public const string SendOrderCancelledStoreOwnerMessage = "OrderCancelled.StoreOwnerNotification";
    public const string SendOrderRefundedStoreOwnerMessage = "OrderRefunded.StoreOwnerNotification";

    public const string SendOrderPlacedCustomerMessage = "OrderPlaced.CustomerNotification";
    public const string SendOrderPaidCustomerMessage = "OrderPaid.CustomerNotification";
    public const string SendOrderCompletedCustomerMessage = "OrderCompleted.CustomerNotification";
    public const string SendOrderCancelledCustomerMessage = "OrderCancelled.CustomerNotification";
    public const string SendOrderRefundedCustomerMessage = "OrderRefunded.CustomerNotification";
    public const string SendPaymentDueReminderCustomerMessage = "OrderPaymentDue.CustomerNotification";
    public const string SendPaymentOverdueCustomerMessage = "OrderPaymentOverdue.CustomerNotification";

    public const string SendOrderPlacedVendorMessage = "OrderPlaced.VendorNotification";
    public const string SendOrderPaidVendorMessage = "OrderPaid.VendorNotification";
    public const string SendOrderCancelledVendorMessage = "OrderCancelled.VendorNotification";

    public const string SendShipmentSentCustomerMessage = "ShipmentSent.CustomerNotification";
    public const string SendShipmentDeliveredCustomerMessage = "ShipmentDelivered.CustomerNotification";

    public const string SendNewOrderNoteAddedCustomerMessage = "Customer.NewOrderNote";

    public const string SendNewsLetterSubscriptionActivationMessage = "NewsLetterSubscription.ActivationMessage";
    public const string SendNewsLetterSubscriptionDeactivationMessage = "NewsLetterSubscription.DeactivationMessage";

    public const string SendWishlistEmailAFriendMessage = "Wishlist.EmailAFriend";
    public const string SendProductEmailAFriendMessage = "Service.EmailAFriend";
    public const string SendProductQuestionMessage = "Service.AskQuestion";

    public const string SendNewMerchandiseReturnStoreOwnerMessage = "NewMerchandiseReturn.StoreOwnerNotification";

    public const string SendMerchandiseReturnStatusChangedCustomerMessage =
        "MerchandiseReturnStatusChanged.CustomerNotification";

    public const string SendNewMerchandiseReturnCustomerMessage = "NewMerchandiseReturn.CustomerNotification";
    public const string SendNewMerchandiseReturnNoteAddedCustomerMessage = "Customer.NewMerchandiseReturnNote";

    public const string SendNewMerchandiseReturnNoteAddedStoreOwnerMessage =
        "NewMerchandiseReturnNote.StoreOwnerNotification";

    public const string SendMerchandiseReturnAcceptedCustomerMessage =
        "MerchandiseReturnAccepted.CustomerNotification";

    public const string SendMerchandiseReturnReceivedCustomerMessage =
        "MerchandiseReturnReceived.CustomerNotification";

    public const string SendMerchandiseReturnRejectedCustomerMessage =
        "MerchandiseReturnRejected.CustomerNotification";

    public const string SendMerchandiseReturnCompletedCustomerMessage =
        "MerchandiseReturnCompleted.CustomerNotification";

    public const string SendGuestOrderLinkCustomerMessage =
        "GuestOrderLink.CustomerNotification";

    /// <summary>
    ///     The customer's message for a merchandise return that reached the status; null for a status told by the
    ///     generic status changed message (a return opened again, a cancelled one)
    /// </summary>
    public static string MerchandiseReturnStatusTemplate(MerchandiseReturnStatus status)
    {
        return status switch {
            MerchandiseReturnStatus.Accepted => SendMerchandiseReturnAcceptedCustomerMessage,
            MerchandiseReturnStatus.Received => SendMerchandiseReturnReceivedCustomerMessage,
            MerchandiseReturnStatus.Rejected => SendMerchandiseReturnRejectedCustomerMessage,
            MerchandiseReturnStatus.Completed => SendMerchandiseReturnCompletedCustomerMessage,
            _ => null
        };
    }
}