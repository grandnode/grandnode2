using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Components;

public class StoreCustomerReportRegisteredViewComponent : BaseAdminViewComponent
{
    private readonly IPermissionService _permissionService;

    public StoreCustomerReportRegisteredViewComponent(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageReports))
            return Content("");

        return View();
    }
}