using Grand.Web.Admin.Extensions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Helpers;
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
    /// <remarks>
    ///     ARCH-001 GetActiveStore() consolidation: delegates to the shared IAdminStoreService (also used by
    ///     BaseTaxCategoryController and StoreScope) instead of duplicating its logic. Resolved via the
    ///     service locator, not constructor injection, to avoid touching every Admin controller's constructor.
    /// </remarks>
    protected Task<string> GetActiveStore()
    {
        return HttpContext.RequestServices.GetRequiredService<IAdminStoreService>().GetActiveStore();
    }
}