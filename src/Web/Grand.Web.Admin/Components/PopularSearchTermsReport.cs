using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components
{
    public class PopularSearchTermsReportViewComponent : BaseAdminViewComponent
    {
        private readonly IPermissionService _permissionService;

        public PopularSearchTermsReportViewComponent(IPermissionService permissionService)
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
}