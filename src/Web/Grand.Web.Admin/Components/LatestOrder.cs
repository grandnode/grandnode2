using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components
{
    public class LatestOrderViewComponent : BaseAdminViewComponent
    {
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;

        public LatestOrderViewComponent(IPermissionService permissionService, IWorkContext workContext)
        {
            _permissionService = permissionService;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Content("");

            var isLoggedInAsVendor = _workContext.CurrentVendor != null;
            return View(isLoggedInAsVendor);
        }
    }
}