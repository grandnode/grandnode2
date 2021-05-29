using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Messages;
using Grand.Domain.News;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Interfaces
{
    public partial interface IMessageProviderService
    {
        #region Customer messages

        /// <summary>
        /// Sends 'New customer' notification message to a store owner
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendCustomerRegisteredMessage(Customer customer, Store store, string languageId);

        /// <summary>
        /// Sends a welcome message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendCustomerWelcomeMessage(Customer customer, Store store, string languageId);

        /// <summary>
        /// Sends an email validation message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendCustomerEmailValidationMessage(Customer customer, Store store, string languageId);

        /// <summary>
        /// Sends password recovery message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendCustomerPasswordRecoveryMessage(Customer customer, Store store, string languageId);

        /// <summary>
        /// Sends a new customer note added notification to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="customerNote">Customer note</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendNewCustomerNoteMessage(CustomerNote customerNote, Customer customer, Store store, string languageId);

        /// <summary>
        /// Send an email token validation message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendCustomerEmailTokenValidationMessage(Customer customer, Store store, string languageId);

        #endregion

        #region Order messages

        /// <summary>
        /// Sends an order placed notification to a vendor
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="vendor">Vendor instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderPlacedVendorMessage(Order order, Customer customer, Vendor vendor, string languageId);

        /// <summary>
        /// Sends an order placed notification to a store owner
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderPlacedStoreOwnerMessage(Order order, Customer customer, string languageId);

        /// <summary>
        /// Sends an order paid notification to a store owner
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderPaidStoreOwnerMessage(Order order, Customer customer, string languageId);

        /// <summary>
        /// Sends an order paid notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name. If specified, then this file name will be sent to a recipient. Otherwise, "AttachmentFilePath" name will be used.</param>
        /// <param name="attachments">Attachments ident</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderPaidCustomerMessage(Order order, Customer customer, string languageId,
            string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null);

        /// <summary>
        /// Sends an order paid notification to a vendor
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="vendor">Vendor instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderPaidVendorMessage(Order order, Vendor vendor, string languageId);

        /// <summary>
        /// Sends an order placed notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name. If specified, then this file name will be sent to a recipient. Otherwise, "AttachmentFilePath" name will be used.</param>
        /// <param name="attachments">Attachments ident</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderPlacedCustomerMessage(Order order, Customer customer, string languageId,
            string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null);

        /// <summary>
        /// Sends a shipment sent notification to a customer
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <param name="order">Order</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendShipmentSentCustomerMessage(Shipment shipment, Order order);

        /// <summary>
        /// Sends a shipment delivered notification to a customer
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <param name="order">Order</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendShipmentDeliveredCustomerMessage(Shipment shipment, Order order);

        /// <summary>
        /// Sends an order completed notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name. If specified, then this file name will be sent to a recipient. Otherwise, "AttachmentFilePath" name will be used.</param>
        /// <param name="attachments">Attachments ident</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderCompletedCustomerMessage(Order order, Customer customer, string languageId,
            string attachmentFilePath = null, string attachmentFileName = null, IEnumerable<string> attachments = null);

        /// <summary>
        /// Sends an order cancelled notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderCancelledCustomerMessage(Order order, Customer customer, string languageId);

        /// <summary>
        /// Sends an order cancelled notification to a store owner
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderCancelledStoreOwnerMessage(Order order, Customer customer, string languageId);

        /// <summary>
        /// Sends an order cancel notification to a vendor
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="vendor">Vendor instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderCancelledVendorMessage(Order order, Vendor vendor, string languageId);

        /// <summary>
        /// Sends an order refunded notification to a store owner
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="refundedAmount">Amount refunded</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderRefundedStoreOwnerMessage(Order order, double refundedAmount, string languageId);

        /// <summary>
        /// Sends an order refunded notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="refundedAmount">Amount refunded</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOrderRefundedCustomerMessage(Order order, double refundedAmount, string languageId);

        /// <summary>
        /// Sends a new order note added notification to a customer
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="orderNote">Order note</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendNewOrderNoteAddedCustomerMessage(Order order, OrderNote orderNote);

        #endregion

        #region Newsletter messages

        /// <summary>
        /// Sends a newsletter subscription activation message
        /// </summary>
        /// <param name="subscription">Newsletter subscription</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendNewsLetterSubscriptionActivationMessage(NewsLetterSubscription subscription,
            string languageId);

        /// <summary>
        /// Sends a newsletter subscription deactivation message
        /// </summary>
        /// <param name="subscription">Newsletter subscription</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendNewsLetterSubscriptionDeactivationMessage(NewsLetterSubscription subscription,
            string languageId);

        #endregion

        #region Send a message to a friend, ask question

        /// <summary>
        /// Sends "email a friend" message
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="product">Product instance</param>
        /// <param name="customerEmail">Customer's email</param>
        /// <param name="friendsEmail">Friend's email</param>
        /// <param name="personalMessage">Personal message</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendProductEmailAFriendMessage(Customer customer, Store store, string languageId,
            Product product, string customerEmail, string friendsEmail, string personalMessage);

        /// <summary>
        /// Sends wishlist "email a friend" message
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="customerEmail">Customer's email</param>
        /// <param name="friendsEmail">Friend's email</param>
        /// <param name="personalMessage">Personal message</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendWishlistEmailAFriendMessage(Customer customer, Store store, string languageId,
             string customerEmail, string friendsEmail, string personalMessage);


        /// <summary>
        /// Sends "email a friend" message
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="product">Product instance</param>
        /// <param name="customerEmail">Customer's email</param>
        /// <param name="friendsEmail">Friend's email</param>
        /// <param name="personalMessage">Personal message</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendProductQuestionMessage(Customer customer, Store store, string languageId,
            Product product, string customerEmail, string fullName, string phone, string message);

        #endregion

        #region Merchandise returns

        /// <summary>
        /// Sends 'New Merchandise Return' message to a store owner
        /// </summary>
        /// <param name="merchandiseReturn">Merchandise return</param>
        /// <param name="order">Order</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendNewMerchandiseReturnStoreOwnerMessage(MerchandiseReturn merchandiseReturn, Order order, string languageId);


        /// <summary>
        /// Sends 'Merchandise Return status changed' message to a customer
        /// </summary>
        /// <param name="merchandiseReturn">Merchandise return</param>
        /// <param name="order">Order item</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendMerchandiseReturnStatusChangedCustomerMessage(MerchandiseReturn merchandiseReturn, Order order, string languageId);

        /// <summary>
        /// Sends 'New Merchandise Return' message to a customer
        /// </summary>
        /// <param name="merchandiseReturn"></param>
        /// <param name="order"></param>
        /// <param name="languageId"></param>
        /// <returns></returns>
        Task<int> SendNewMerchandiseReturnCustomerMessage(MerchandiseReturn merchandiseReturn, Order order, string languageId);

        /// <summary>
        /// Sends a new merchandise return note added notification to a customer
        /// </summary>
        /// <param name="merchandiseReturn">Merchandise return</param>
        /// <param name="merchandiseReturnNote">Merchandise return note</param>
        /// <param name="order">Order</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendNewMerchandiseReturnNoteAddedCustomerMessage(MerchandiseReturn merchandiseReturn, MerchandiseReturnNote merchandiseReturnNote, Order order);

        #endregion

        #region Misc

        /// <summary>
        /// Sends 'New vendor account submitted' message to a store owner
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="vendor">Vendor</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendNewVendorAccountApplyStoreOwnerMessage(Customer customer, Vendor vendor, Store store, string languageId);


        /// <summary>
        /// Sends 'Vendor information change' message to a store owner
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendVendorInformationChangeMessage(Vendor vendor, Store store, string languageId);

        /// <summary>
        /// Sends a product review notification message to a store owner
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="productReview">Product review</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendProductReviewMessage(Product product, ProductReview productReview, Store store, string languageId);

        /// <summary>
        /// Sends a vendor review notification message to a store owner
        /// </summary>
        /// <param name="vendorReview">Vendor review</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendVendorReviewMessage(VendorReview vendorReview, Store store, string languageId);

        /// <summary>
        /// Sends a gift voucher notification
        /// </summary>
        /// <param name="giftVoucher">Gift voucher</param>
        /// <param name="order">Order</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendGiftVoucherMessage(GiftVoucher giftVoucher, Order order, string languageId);


        /// <summary>
        /// Sends a "quantity below" notification to a store owner
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendQuantityBelowStoreOwnerMessage(Product product, string languageId);

        /// <summary>
        /// Sends a "quantity below" notification to a store owner
        /// </summary>
        /// <param name="combination">Attribute combination</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendQuantityBelowStoreOwnerMessage(Product product, ProductAttributeCombination combination, string languageId);

        /// <summary>
        /// Sends a "customer delete" notification to a store owner
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendCustomerDeleteStoreOwnerMessage(Customer customer, string languageId);

        /// <summary>
        /// Sends a blog comment notification message to a store owner
        /// </summary>
        /// <param name="blogComment">Blog comment</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendBlogCommentMessage(BlogPost blogPost, BlogComment blogComment, string languageId);

        /// <summary>
        /// Sends an article comment notification message to a store owner
        /// </summary>
        /// <param name="articleComment">Article comment</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendArticleCommentMessage(KnowledgebaseArticle article, KnowledgebaseArticleComment articleComment, string languageId);

        /// <summary>
        /// Sends a news comment notification message to a store owner
        /// </summary>
        /// <param name="newsComment">News comment</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendNewsCommentMessage(NewsItem newsItem, NewsComment newsComment, string languageId);

        /// <summary>
        /// Sends a 'Back in stock' notification message to a customer
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="product">Product</param>
        /// <param name="subscription">Subscription</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendBackinStockMessage(Customer customer, Product product, OutOfStockSubscription subscription, string languageId);


        /// <summary>
        /// Sends "contact us" message
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="senderEmail">Sender email</param>
        /// <param name="senderName">Sender name</param>
        /// <param name="subject">Email subject. Pass null if you want a message template subject to be used.</param>
        /// <param name="body">Email body</param>
        /// <param name="attrInfo">Attr info</param>
        /// <param name="customAttributes">Custom Attributes</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendContactUsMessage(Customer customer, Store store, string languageId, string senderEmail, string senderName, string subject, string body, string attrInfo, IList<CustomAttribute> customAttributes);

        /// <summary>
        /// Sends "contact vendor" message
        /// </summary>
        /// <param name="vendor">Vendor</param>
        /// <param name="store">Store</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="senderEmail">Sender email</param>
        /// <param name="senderName">Sender name</param>
        /// <param name="subject">Email subject. Pass null if you want a message template subject to be used.</param>
        /// <param name="body">Email body</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendContactVendorMessage(Customer customer, Store store, Vendor vendor, string languageId, string senderEmail, string senderName, string subject, string body);

        /// <summary>
        /// Sends a customer action event - Add to cart notification to a customer
        /// </summary>
        /// <param name="CustomerAction">Customer action</param>
        /// <param name="ShoppingCartItem">Item</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendCustomerActionAddToCartMessage(CustomerAction action, ShoppingCartItem cartItem, string languageId, Customer customer);


        /// <summary>
        /// Sends a customer action event - Add order notification to a customer
        /// </summary>
        /// <param name="CustomerAction">Customer action</param>
        /// <param name="Order">Order</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendCustomerActionAddToOrderMessage(CustomerAction action, Order order, Customer customer, string languageId);


        /// <summary>
        /// Sends a customer action event 
        /// </summary>
        /// <param name="CustomerAction">Customer action</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendCustomerActionMessage(CustomerAction action, string languageId, Customer customer);

        /// <summary>
        /// Sends auction ended notification to a customer (winner)
        /// </summary>
        /// <param name="product">Auction</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="Bid">Bid</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendAuctionWinEndedCustomerMessage(Product product, string languageId, Bid bid);

        /// <summary>
        /// Sends auction ended notification to a customer (loser)
        /// </summary>
        /// <param name="product">Auction</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="Bid">Bid</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendAuctionEndedLostCustomerMessage(Product product, string languageId, Bid bid);

        /// <summary>
        /// Sends auction ended notification to a customer (loser - bin)
        /// </summary>
        /// <param name="product">Auction</param>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendAuctionEndedBinCustomerMessage(Product product, string customerId, string languageId, string storeId);

        /// <summary>
        /// Sends auction ended notification to a store owner
        /// </summary>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="product">Auction</param>
        /// <param name="Bid">Bid</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendAuctionEndedStoreOwnerMessage(Product product, string languageId, Bid bid);

        /// <summary>
        /// Send outbid notification to a customer
        /// </summary>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="product">Product</param>
        /// <param name="Bid">Bid</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendOutBidCustomerMessage(Product product, string languageId, Bid bid);

        /// <summary>
        /// Send notification
        /// </summary>
        /// <param name="messageTemplate">Message template</param>
        /// <param name="emailAccount">Email account</param>
        /// <param name="languageId">Language identifier</param>
        /// <param name="liquidObject">LiquidObject</param>
        /// <param name="tokens">Tokens</param>
        /// <param name="toEmailAddress">Recipient email address</param>
        /// <param name="toName">Recipient name</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name</param>
        /// <param name="attachedDownloads">Attached downloads ident</param>
        /// <param name="replyToEmailAddress">"Reply to" email</param>
        /// <param name="replyToName">"Reply to" name</param>
        /// <param name="fromEmail">Sender email. If specified, then it overrides passed "emailAccount" details</param>
        /// <param name="fromName">Sender name. If specified, then it overrides passed "emailAccount" details</param>
        /// <param name="subject">Subject. If specified, then it overrides subject of a message template</param>
        /// <returns>Queued email identifier</returns>
        Task<int> SendNotification(MessageTemplate messageTemplate,
            EmailAccount emailAccount, string languageId, LiquidObject liquidObject,
            string toEmailAddress, string toName,
            string attachmentFilePath = null, string attachmentFileName = null,
            IEnumerable<string> attachedDownloads = null,
            string replyToEmailAddress = null, string replyToName = null,
            string fromEmail = null, string fromName = null, string subject = null);

        #endregion
    }
}