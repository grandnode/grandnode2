using Grand.Domain.Permissions;
using Grand.Web.Common.Components;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components;

public class PopularSearchTermsReportViewComponent : BaseAdminViewComponent
{
    private readonly IRequestPermissionService _permissionService;

    public PopularSearchTermsReportViewComponent(IRequestPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageProducts))
            return Content("");

        return View();
    }
}