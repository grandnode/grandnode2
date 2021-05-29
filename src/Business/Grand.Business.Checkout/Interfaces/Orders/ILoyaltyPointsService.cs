using Grand.Domain.Customers;
using Grand.Domain.Orders;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Interfaces.Orders
{
    /// <summary>
    /// LoyaltyPoints service interface
    /// </summary>
    public partial interface ILoyaltyPointsService
    {

        /// <summary>
        /// Gets loyalty points balance
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="storeId">Store identifier; pass </param>
        /// <returns>Balance</returns>
        Task<int> GetLoyaltyPointsBalance(string customerId, string storeId);

        /// <summary>
        /// Add loyalty points history record
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="points">Number of points to add</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="message">Message</param>
        /// <param name="usedWithOrder">the order for which points were redeemed as a payment</param>
        /// <param name="usedAmount">Used amount</param>
        Task<LoyaltyPointsHistory> AddLoyaltyPointsHistory(string customerId, int points, string storeId, string message = "",
           string usedWithOrderId = "", double usedAmount = 0);

        /// <summary>
        /// Load loyalty point history records
        /// </summary>
        /// <param name="customerId">Customer identifier; "" to load all records</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="showAll">A value indicating whether to show for all store(filter by current store if possible)</param>
        /// <returns>Loyalty point history records</returns>
        Task<IList<LoyaltyPointsHistory>> GetLoyaltyPointsHistory(string customerId = "", string storeId = "", bool showAll = false);

    }
}
