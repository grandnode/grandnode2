﻿using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components
{
    public class CustomerReportRegisteredViewComponent : BaseAdminViewComponent
    {
        private readonly IPermissionService _permissionService;

        public CustomerReportRegisteredViewComponent(IPermissionService permissionService)
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
