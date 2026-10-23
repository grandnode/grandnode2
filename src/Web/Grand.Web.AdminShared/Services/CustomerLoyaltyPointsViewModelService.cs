using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Services;

public class CustomerLoyaltyPointsViewModelService(
    ILoyaltyPointsService loyaltyPointsService,
    IStoreService storeService,
    IDateTimeService dateTimeService)
    : ICustomerLoyaltyPointsViewModelService
{
    public virtual async Task<IEnumerable<CustomerModel.LoyaltyPointsHistoryModel>> PrepareLoyaltyPointsHistoryModel(
        string customerId)
    {
        var model = new List<CustomerModel.LoyaltyPointsHistoryModel>();
        foreach (var rph in await loyaltyPointsService.GetLoyaltyPointsHistory(customerId, showAll: true))
        {
            var store = await storeService.GetStoreById(rph.StoreId);
            model.Add(new CustomerModel.LoyaltyPointsHistoryModel {
                StoreName = store != null ? store.Shortcut : "Unknown",
                Points = rph.Points,
                PointsBalance = rph.PointsBalance,
                Message = rph.Message,
                CreatedOn = dateTimeService.ConvertToUserTime(rph.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        return model;
    }

    public virtual async Task<LoyaltyPointsHistory> InsertLoyaltyPointsHistory(Customer customer, string storeId,
        int addLoyaltyPointsValue, string addLoyaltyPointsMessage)
    {
        return await loyaltyPointsService.AddLoyaltyPointsHistory(customer.Id, addLoyaltyPointsValue, storeId,
            addLoyaltyPointsMessage);
    }
}
