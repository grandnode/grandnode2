using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

// ARCH-001: Admin's and Store's original OnlineCustomerController were near-identical - no entity,
// no per-store/per-vendor scope beyond the StaffStoreId filter both hosts already applied
// identically, so no IAdminDataScope is needed here (same shape as BasePictureController). The one
// real difference is Admin's Sales-Manager restriction (GetOnlineCustomers' salesEmployeeId filter);
// Store has no such concept and always passes null. Parameterized with a protected virtual member,
// the same idiom this repo's pre-existing BaseLoginController already uses for its own
// host-specific GetCurrentArea() value. Vendor never had its own copy.
[PermissionAuthorize(PermissionSystemName.Customers)]
[AutoValidateAntiforgeryToken]
public abstract class BaseOnlineCustomerController(
    ICustomerService customerService,
    IDateTimeService dateTimeService,
    CustomerSettings customerSettings,
    ITranslationService translationService,
    IContextAccessor contextAccessor)
    : BaseController
{
    /// <summary>
    ///     Sales-Manager restriction on the online-customers list. Admin restricts to the current
    ///     Sales-Manager's own customers; Store has no such concept and never restricts by it.
    /// </summary>
    protected virtual string SalesEmployeeIdFilter => null;

    public IActionResult List()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public virtual async Task<IActionResult> List(DataSourceRequest command)
    {
        var customers = await customerService.GetOnlineCustomers(
            DateTime.UtcNow.AddMinutes(-customerSettings.OnlineCustomerMinutes),
            null, contextAccessor.WorkContext.CurrentCustomer.StaffStoreId, SalesEmployeeIdFilter, command.Page - 1,
            command.PageSize);
        var items = customers.Select(x => new OnlineCustomerModel {
            Id = x.Id,
            CustomerInfo = !string.IsNullOrEmpty(x.Email) ? x.Email : translationService.GetResource("Admin.Customers.Guest"),
            LastIpAddress = x.LastIpAddress,
            LastActivityDate = dateTimeService.ConvertToUserTime(x.LastActivityDateUtc, DateTimeKind.Utc),
            LastVisitedPage = customerSettings.StoreLastVisitedPage ? x.LastVisitedPage : translationService.GetResource("Admin.Dashboards.OnlineCustomers.Fields.LastVisitedPage.Disabled")
        }).ToList();

        var gridModel = new DataSourceResult {
            Data = items,
            Total = customers.TotalCount
        };

        return Json(gridModel);
    }
}
