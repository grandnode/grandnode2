using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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