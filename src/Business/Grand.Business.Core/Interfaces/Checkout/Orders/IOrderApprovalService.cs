using Grand.Domain;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Order approval service: a company sub-account's order waits for an approver's decision
/// </summary>
public interface IOrderApprovalService
{
    /// <summary>
    ///     Gets an approval of the company
    /// </summary>
    /// <param name="id">Approval identifier</param>
    /// <param name="companyOwnerId">Identifier of the company owner the approval must belong to</param>
    /// <returns>Approval; null when it does not exist or belongs to another company</returns>
    Task<OrderApproval> GetById(string id, string companyOwnerId);

    /// <summary>
    ///     Gets the approval request of an order
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <returns>Approval; null when the order has none</returns>
    Task<OrderApproval> GetByOrderId(string orderId);

    /// <summary>
    ///     Gets the company's requests in the store, pending first and then the newest
    /// </summary>
    /// <param name="companyOwnerId">Identifier of the company owner</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    Task<IPagedList<OrderApproval>> GetForCompany(string companyOwnerId, string storeId, int pageIndex,
        int pageSize);

    /// <summary>
    ///     Gets the customer's requests in the store, the newest first
    /// </summary>
    /// <param name="customerId">Buyer identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    Task<IPagedList<OrderApproval>> GetForCustomer(string customerId, string storeId, int pageIndex,
        int pageSize);

    /// <summary>
    ///     Inserts an approval
    /// </summary>
    /// <param name="approval">Approval</param>
    Task Insert(OrderApproval approval);

    /// <summary>
    ///     Updates an approval
    /// </summary>
    /// <param name="approval">Approval</param>
    Task Update(OrderApproval approval);

    /// <summary>
    ///     Gets a value indicating whether the payment method may be used for an order that needs an approval
    /// </summary>
    /// <param name="paymentMethodSystemName">Payment method system name</param>
    bool IsPaymentMethodAllowed(string paymentMethodSystemName);
}
