using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICustomerLoyaltyPointsViewModelService
{
    Task<IEnumerable<CustomerModel.LoyaltyPointsHistoryModel>> PrepareLoyaltyPointsHistoryModel(string customerId);

    Task<LoyaltyPointsHistory> InsertLoyaltyPointsHistory(Customer customer, string storeId, int addLoyaltyPointsValue,
        string addLoyaltyPointsMessage);
}
