using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain;
using Grand.Domain.Orders;

namespace Grand.Business.Core.Interfaces.Checkout.Orders;

/// <summary>
///     Order approval service: a company sub-account's cart is approved before the order is placed
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
    ///     Gets the newest pending or approved request of the customer in the store
    /// </summary>
    /// <param name="customerId">Buyer identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>Approval; null when there is none</returns>
    Task<OrderApproval> GetActive(string customerId, string storeId);

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
    ///     Works out whether the cart needs an approval before the order is placed.
    ///     An approval is needed when the customer is a sub-account buyer with an order limit, the sub-account
    ///     feature is on, the order total is known and exceeds the limit. It is satisfied by an approved request
    ///     of the same customer and store for the same cart whose amount is not below the current total.
    /// </summary>
    /// <param name="cart">Shopping cart of the customer</param>
    /// <param name="context">Customer, store and currency the cart is calculated for</param>
    Task<OrderApprovalCheckoutState> GetCheckoutState(IList<ShoppingCartItem> cart, CalculationContext context);
}
