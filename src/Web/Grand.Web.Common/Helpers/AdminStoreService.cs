using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Domain.Common;

namespace Grand.Web.Common.Helpers;

public interface IAdminStoreService
{
    Task<string> GetActiveStore();
}

public class AdminStoreService : IAdminStoreService
{
    private readonly IStoreService _storeService;
    private readonly IContextAccessor _contextAccessor;

    public AdminStoreService(IStoreService storeService, IContextAccessor contextAccessor)
    {
        _storeService = storeService;
        _contextAccessor = contextAccessor;
    }

    public async Task<string> GetActiveStore()
    {
        var stores = await _storeService.GetAllStores();
        if (stores.Count < 2)
            return stores.FirstOrDefault()!.Id;

        var storeId = _contextAccessor.WorkContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.AdminAreaStoreScopeConfiguration);
        var store = await _storeService.GetStoreById(storeId);

        return store != null ? store.Id : "";
    }
}