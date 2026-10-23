using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;

namespace Grand.Business.Core.Interfaces.Messages;

public interface IVendorMessageProviderService
{
    /// <summary>
    ///     Sends 'New vendor account submitted' message to a store owner
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="vendor">Vendor</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendNewVendorAccountApplyStoreOwnerMessage(Customer customer, Vendor vendor, Store store,
        string languageId, DomainHost host = null);

    /// <summary>
    ///     Sends 'Vendor information change' message to a store owner
    /// </summary>
    /// <param name="vendor">Vendor</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendVendorInformationChangeMessage(Vendor vendor, Store store, string languageId);

    /// <summary>
    ///     Sends a vendor review notification message to a store owner
    /// </summary>
    /// <param name="vendorReview">Vendor review</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendVendorReviewMessage(VendorReview vendorReview, Store store, string languageId,
        DomainHost host = null);

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
    /// <param name="ipaddress">Ip Address</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendContactVendorMessage(Customer customer, Store store, Vendor vendor, string languageId,
        string senderEmail, string senderName, string subject, string body, string ipaddress, DomainHost host = null);
}
