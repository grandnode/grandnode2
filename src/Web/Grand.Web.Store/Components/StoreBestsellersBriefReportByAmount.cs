using Grand.Domain.Permissions;
using Grand.Web.Common.Components;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Components;

public class StoreBestsellersBriefReportByAmountViewComponent : BaseStoreViewComponent
{
    private readonly IRequestPermissionService _permissionService;

    public StoreBestsellersBriefReportByAmountViewComponent(IRequestPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Content("");

        return View();
    }
}