using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

/// <summary>The customer's notes tab; the store panel has no notes tab, so this controller is Admin-only.</summary>
[AuthorizeAdmin]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class CustomerNoteController(
    ICustomerService customerService,
    ICustomerNoteViewModelService customerNoteViewModelService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerScopedController(customerService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> CustomerNotesSelect(string customerId, DataSourceRequest command)
    {
        var (_, denied) = await LoadAuthorizedCustomer(customerId);
        if (denied != null) throw new ArgumentException("No customer found with the specified id");

        var customerNoteModels = await customerNoteViewModelService.PrepareCustomerNoteList(customerId);
        var gridModel = new DataSourceResult { Data = customerNoteModels, Total = customerNoteModels.Count };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CustomerNoteAdd(string customerId, string downloadId, bool displayToCustomer,
        string title, string message)
    {
        var (_, denied) = await LoadAuthorizedCustomer(customerId);
        if (denied != null) return Json(new { Result = false });

        await customerNoteViewModelService.InsertCustomerNote(customerId, downloadId, displayToCustomer, title, message);
        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CustomerNoteDelete(string id, string customerId)
    {
        var (_, denied) = await LoadAuthorizedCustomer(customerId);
        if (denied != null) throw new ArgumentException("No customer found with the specified id");

        await customerNoteViewModelService.DeleteCustomerNote(id, customerId);
        return new JsonResult("");
    }
}
