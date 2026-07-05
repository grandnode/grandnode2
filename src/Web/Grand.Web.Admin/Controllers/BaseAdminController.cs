using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public abstract class BaseAdminController : BaseController
{
    /// <summary>
    ///     Get active store scope (for multi-store configuration mode)
    /// </summary>
    /// <returns>Store ID; 0 if we are in a shared mode</returns>
    protected async Task<string> GetActiveStore()
    {
        var storeService = HttpContext.RequestServices.GetRequiredService<IStoreService>();
        var workContext = HttpContext.RequestServices.GetRequiredService<IContextAccessor>().WorkContext;

        var stores = await storeService.GetAllStores();
        if (stores.Count < 2)
            return stores.FirstOrDefault()?.Id;

        var storeId =
            workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames
                .AdminAreaStoreScopeConfiguration);
        //empty scope means "all stores" - settings are loaded from/saved to the global scope
        if (string.IsNullOrEmpty(storeId))
        {
            return "";
        }

        var store = await storeService.GetStoreById(storeId);
        return store != null ? store.Id : "";
    }
}