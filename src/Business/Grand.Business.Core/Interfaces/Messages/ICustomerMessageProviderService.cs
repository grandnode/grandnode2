using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Interfaces.Messages;

public interface ICustomerMessageProviderService
{
    /// <summary>
    ///     Sends 'New customer' notification message to a store owner
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendCustomerRegisteredMessage(Customer customer, Store store, string languageId, DomainHost host = null);

    /// <summary>
    ///     Sends a welcome message to a customer
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendCustomerWelcomeMessage(Customer customer, Store store, string languageId, DomainHost host = null);

    /// <summary>
    ///     Sends an email validation message to a customer
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendCustomerEmailValidationMessage(Customer customer, Store store, string languageId,
        DomainHost host = null);

    /// <summary>
    ///     Sends password recovery message to a customer
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendCustomerPasswordRecoveryMessage(Customer customer, Store store, string languageId,
        DomainHost host = null);

    /// <summary>
    ///     Sends a new customer note added notification to a customer
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="customerNote">Customer note</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendNewCustomerNoteMessage(CustomerNote customerNote, Customer customer, Store store, string languageId);

    /// <summary>
    ///     Send an email token validation message to a customer
    /// </summary>
    /// <param name="customer">Customer instance</param>
    /// <param name="store">Store instance</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendCustomerEmailTokenValidationMessage(Customer customer, Store store, string languageId,
        DomainHost host = null);

    /// <summary>
    ///     Sends a "customer delete" notification to a store owner
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendCustomerDeleteStoreOwnerMessage(Customer customer, string languageId, DomainHost host = null);

    /// <summary>
    ///     Sends "contact us" message
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
    /// <param name="ipaddress">ip address</param>
    /// <returns>Queued email identifier</returns>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    Task<int> SendContactUsMessage(Customer customer, Store store, string languageId, string senderEmail,
        string senderName, string subject, string body, string attrInfo, IList<CustomAttribute> customAttributes,
        string ipaddress, DomainHost host = null);

    /// <summary>
    ///     Sends an 'Order approval requested' message to the approver
    /// </summary>
    /// <param name="approval">Order approval</param>
    /// <param name="buyer">Customer who asked for the approval</param>
    /// <param name="approver">Customer who decides; the recipient</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderApprovalRequestedMessage(OrderApproval approval, Customer buyer, Customer approver,
        Store store, string languageId, DomainHost host = null);

    /// <summary>
    ///     Sends an 'Order approval decided' message to the buyer
    /// </summary>
    /// <param name="approval">Order approval</param>
    /// <param name="buyer">Customer who asked for the approval; the recipient</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderApprovalDecidedMessage(OrderApproval approval, Customer buyer, Store store,
        string languageId, DomainHost host = null);

    /// <summary>
    ///     Sends an 'Order approval decided' message to the given address, for a buyer who has none of their own
    /// </summary>
    /// <param name="approval">Order approval</param>
    /// <param name="buyer">Customer who asked for the approval</param>
    /// <param name="store">Store</param>
    /// <param name="languageId">Message language identifier</param>
    /// <param name="host">
    ///     Host of the storefront request; links use it when it is one of the store's domains, else the primary domain
    /// </param>
    /// <param name="toEmail">Recipient</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderApprovalDecidedMessage(OrderApproval approval, Customer buyer, Store store,
        string languageId, DomainHost host, string toEmail);

    /// <summary>
    ///     Sends an 'Order approval requested' message to the store, when the store decides the request
    /// </summary>
    /// <param name="approval">Order approval</param>
    /// <param name="buyer">Customer who placed the order</param>
    /// <param name="store">Store</param>
    /// <param name="toEmail">Recipient; empty sends it to the e-mail account of the message template</param>
    /// <param name="toName">Recipient name</param>
    /// <param name="panelArea">The back office panel the recipient works in</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>Queued email identifier</returns>
    Task<int> SendOrderApprovalStaffMessage(OrderApproval approval, Customer buyer, Store store, string toEmail,
        string toName, string panelArea, string languageId);
}
