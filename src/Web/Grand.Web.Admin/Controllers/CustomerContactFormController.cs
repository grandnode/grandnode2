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

/// <summary>The contact form grid on the customer's activity log tab, which only the Admin panel has.</summary>
[AuthorizeAdmin]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class CustomerContactFormController(
    ICustomerService customerService,
    ICustomerContactFormViewModelService customerContactFormViewModelService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerScopedController(customerService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ContactFormList(DataSourceRequest command, string customerId)
    {
        var (contactFormModels, totalCount) =
            await customerContactFormViewModelService.PrepareContactFormModel(customerId, command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = contactFormModels.ToList(), Total = totalCount };
        return Json(gridModel);
    }
}
