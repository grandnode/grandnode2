using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public abstract class BaseAdminController : BaseController
{
    /// <summary>
    ///     Save selected TAB index
    /// </summary>
    /// <param name="index">Idnex to save; null to automatically detect it</param>
    /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
    protected async Task SaveSelectedTabIndex(int? index = null, bool persistForTheNextRequest = true)
    {
        if (!index.HasValue)
        {
            var form = await HttpContext.Request.ReadFormAsync();
            var tabindex = form["selected-tab-index"];
            if (tabindex.Count > 0)
            {
                if (int.TryParse(tabindex[0], out var tmp)) index = tmp;
            }
            else
            {
                index = 1;
            }
        }

        if (index.HasValue)
        {
            var dataKey = "Grand.selected-tab-index";
            if (persistForTheNextRequest)
                TempData[dataKey] = index;
            else
                ViewData[dataKey] = index;
        }
    }

    /// <summary>
    ///     Get active store scope (for multi-store configuration mode)
    /// </summary>
    /// <returns>Store ID; 0 if we are in a shared mode</returns>
    protected async Task<string> GetActiveStore()
    {
        var storeService = HttpContext.RequestServices.GetRequiredService<IStoreService>();
        var workContext = HttpContext.RequestServices.GetRequiredService<IWorkContext>();
        var groupService = HttpContext.RequestServices.GetRequiredService<IGroupService>();

        var stores = await storeService.GetAllStores();
        if (stores.Count < 2)
            return stores.FirstOrDefault()?.Id;

        if (await groupService.IsStaff(workContext.CurrentCustomer)) return workContext.CurrentCustomer.StaffStoreId;

        var storeId =
            workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames
                .AdminAreaStoreScopeConfiguration);
        if (string.IsNullOrEmpty(storeId)) return stores.FirstOrDefault()?.Id;
        var store = await storeService.GetStoreById(storeId);
        return store != null ? store.Id : stores.FirstOrDefault()?.Id;
    }
}