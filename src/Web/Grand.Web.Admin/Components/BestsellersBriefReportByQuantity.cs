﻿using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components
{
    public class BestsellersBriefReportByQuantityViewComponent : BaseAdminViewComponent
    {
        private readonly IPermissionService _permissionService;

        public BestsellersBriefReportByQuantityViewComponent(IPermissionService permissionService)
        {
            this._permissionService = permissionService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Content("");

            return View();
        }
    }
}