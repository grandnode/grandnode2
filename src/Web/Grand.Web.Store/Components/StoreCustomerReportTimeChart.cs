using Grand.Domain.Permissions;
using Grand.Web.Common.Components;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Components;

public class StoreCustomerReportTimeChartViewComponent : BaseStoreViewComponent
{
    private readonly IRequestPermissionService _permissionService;

    public StoreCustomerReportTimeChartViewComponent(IRequestPermissionService permissionService)
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