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
    protected Task<string> GetActiveStore()
    {
        return HttpContext.RequestServices.GetRequiredService<IAdminStoreService>().GetActiveStore();
    }
}