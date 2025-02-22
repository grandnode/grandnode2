using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components;

public class LatestOrderViewComponent : BaseAdminViewComponent
{
    private readonly IPermissionService _permissionService;
    private readonly IContextAccessor _contextAccessor;

    public LatestOrderViewComponent(IPermissionService permissionService, IContextAccessor contextAccessor)
    {
        _permissionService = permissionService;
        _contextAccessor = contextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Content("");

        return View();
    }
}