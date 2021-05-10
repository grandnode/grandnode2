
namespace Grand.Business.Messages
{
    public class MessageTemplateNames
    {
        public const string CustomerRegistered = "NewCustomer.Notification";
        public const string CustomerWelcome = "Customer.WelcomeMessage";
        public const string CustomerEmailValidation = "Customer.EmailValidationMessage";
        public const string CustomerPasswordRecovery = "Customer.PasswordRecovery";
        public const string CustomerNewCustomerNote = "Customer.NewCustomerNote";
        public const string CustomerEmailTokenValidationMessage = "Customer.EmailTokenValidationMessage";

        public const string SendOrderPlacedStoreOwnerMessage = "OrderPlaced.StoreOwnerNotification";
        public const string SendOrderPaidStoreOwnerMessage = "OrderPaid.StoreOwnerNotification";
        public const string SendOrderCancelledStoreOwnerMessage = "OrderCancelled.StoreOwnerNotification";
        public const string SendOrderRefundedStoreOwnerMessage = "OrderRefunded.StoreOwnerNotification";

        public const string SendOrderPlacedCustomerMessage = "OrderPlaced.CustomerNotification";
        public const string SendOrderPaidCustomerMessage = "OrderPaid.CustomerNotification";
        public const string SendOrderCompletedCustomerMessage = "OrderCompleted.CustomerNotification";
        public const string SendOrderCancelledCustomerMessage = "OrderCancelled.CustomerNotification";
        public const string SendOrderRefundedCustomerMessage = "OrderRefunded.CustomerNotification";

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
        public const string SendMerchandiseReturnStatusChangedCustomerMessage = "MerchandiseReturnStatusChanged.CustomerNotification";
        public const string SendNewMerchandiseReturnCustomerMessage = "NewMerchandiseReturn.CustomerNotification";
        public const string SendNewMerchandiseReturnNoteAddedCustomerMessage = "Customer.NewMerchandiseReturnNote";
    }
}
