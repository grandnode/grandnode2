using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Components
{
    public class OrderPeriodReportViewComponent : BaseAdminViewComponent
    {
        private readonly IPermissionService _permissionService;

        public OrderPeriodReportViewComponent(IPermissionService permissionService)
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
}