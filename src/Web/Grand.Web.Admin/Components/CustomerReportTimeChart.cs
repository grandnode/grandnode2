using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Components
{
    public class CustomerReportTimeChartViewComponent : BaseAdminViewComponent
    {
        private readonly IPermissionService _permissionService;

        public CustomerReportTimeChartViewComponent(IPermissionService permissionService)
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
}