using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Extensions;

public static class StoreServiceExtensions
{
    public static async Task<Store> GetStoreByHostOrStoreId(this IStoreService storeService, string host, string storeId = null)
    {
        if (string.IsNullOrEmpty(storeId))
        {
            return await storeService.GetStoreByHost(host);
        }
        var store = await storeService.GetStoreById(storeId);

        return store ?? (await storeService.GetAllStores()).FirstOrDefault();
    }
}