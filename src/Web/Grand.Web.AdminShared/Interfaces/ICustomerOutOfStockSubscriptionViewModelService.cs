using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Interfaces;

public interface ICustomerOutOfStockSubscriptionViewModelService
{
    Task<(IEnumerable<CustomerModel.OutOfStockSubscriptionModel> outOfStockSubscriptionModels, int totalCount)>
        PrepareOutOfStockSubscriptionModel(string customerId, int pageIndex, int pageSize);
}
