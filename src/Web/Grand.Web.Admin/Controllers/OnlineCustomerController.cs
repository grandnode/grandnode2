using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Customers)]
public class OnlineCustomerController : BaseAdminController
{
    #region Constructors

    public OnlineCustomerController(ICustomerService customerService,
        IDateTimeService dateTimeService,
        CustomerSettings customerSettings,
        ITranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _customerService = customerService;
        _dateTimeService = dateTimeService;
        _customerSettings = customerSettings;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    #endregion

    #region Fields

    private readonly ICustomerService _customerService;
    private readonly IDateTimeService _dateTimeService;
    private readonly CustomerSettings _customerSettings;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    #endregion

    #region Methods

    public IActionResult List()
    {
        return View();
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var customers = await _customerService.GetOnlineCustomers(
            DateTime.UtcNow.AddMinutes(-_customerSettings.OnlineCustomerMinutes),
            null, _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId, _contextAccessor.WorkContext.CurrentCustomer.SeId, command.Page - 1,
            command.PageSize);
        var items = new List<OnlineCustomerModel>();
        foreach (var x in customers)
        {
            var item = new OnlineCustomerModel {
                Id = x.Id,
                CustomerInfo = !string.IsNullOrEmpty(x.Email) ? x.Email : _translationService.GetResource("Admin.Customers.Guest"),
                LastIpAddress = x.LastIpAddress,
                LastActivityDate = _dateTimeService.ConvertToUserTime(x.LastActivityDateUtc, DateTimeKind.Utc),
                LastVisitedPage = _customerSettings.StoreLastVisitedPage ? x.LastVisitedPage : _translationService.GetResource("Admin.Dashboards.OnlineCustomers.Fields.LastVisitedPage.Disabled")
            };
            items.Add(item);
        }

        var gridModel = new DataSourceResult {
            Data = items,
            Total = customers.TotalCount
        };

        return Json(gridModel);
    }

    #endregion
}