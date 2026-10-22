using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Messages;

public interface ICatalogMessageProviderService
{
    /// <summary>
    ///     Sends "email a friend" message
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="product">Product instance</param>
    /// <param name="customerEmail">Customer's email</param>
    /// <param name="friendsEmail">Friend's email</param>
    /// <param name="personalMessage">Personal message</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendProductEmailAFriendMessage(Customer customer, Store store, string languageId,
        Product product, string customerEmail, string friendsEmail, string personalMessage, DomainHost host = null);

    /// <summary>
    ///     Sends wishlist "email a friend" message
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="customerEmail">Customer's email</param>
    /// <param name="friendsEmail">Friend's email</param>
    /// <param name="personalMessage">Personal message</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendWishlistEmailAFriendMessage(Customer customer, Store store, string languageId,
        string customerEmail, string friendsEmail, string personalMessage, DomainHost host = null);

    /// <summary>
    ///     Sends "email a friend" message
    /// </summary>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendProductQuestionMessage(Customer customer, Store store, string languageId,
        Product product, string customerEmail, string fullName, string phone, string message, string ipaddress,
        DomainHost host = null);

    /// <summary>
    ///     Sends a product review notification message to a store owner
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="productReview">Product review</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendProductReviewMessage(Product product, ProductReview productReview, Store store, string languageId,
        DomainHost host = null);

    /// <summary>
    ///     Sends a "quantity below" notification to a store owner
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendQuantityBelowStoreOwnerMessage(Product product, string languageId);

    /// <summary>
    ///     Sends a "quantity below" notification to a store owner
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="combination">Attribute combination</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendQuantityBelowStoreOwnerMessage(Product product, ProductAttributeCombination combination,
        string languageId);

    /// <summary>
    ///     Sends a 'Back in stock' notification message to a customer
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="product">Product</param>
    /// <param name="subscription">Subscription</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendBackInStockMessage(Customer customer, Product product, OutOfStockSubscription subscription,
        string languageId);

    /// <summary>
    ///     Sends auction ended notification to a customer (winner)
    /// </summary>
    /// <param name="product">Auction</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="bid">Bid</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendAuctionWinEndedCustomerMessage(Product product, string languageId, Bid bid);

    /// <summary>
    ///     Sends auction ended notification to a customer (loser)
    /// </summary>
    /// <param name="product">Auction</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="bid"></param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendAuctionEndedLostCustomerMessage(Product product, string languageId, Bid bid);

    /// <summary>
    ///     Sends auction ended notification to a customer (loser - bin)
    /// </summary>
    /// <param name="product">Auction</param>
    /// <param name="customerId">Customer identifier</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendAuctionEndedBinCustomerMessage(Product product, string customerId, string languageId, string storeId);

    /// <summary>
    ///     Sends auction ended notification to a store owner
    /// </summary>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="product">Auction</param>
    /// <param name="bid">Bid</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendAuctionEndedStoreOwnerMessage(Product product, string languageId, Bid bid);

    /// <summary>
    ///     Send outbid notification to a customer
    /// </summary>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="product">Product</param>
    /// <param name="bid">Bid</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOutBidCustomerMessage(Product product, string languageId, Bid bid);
}
