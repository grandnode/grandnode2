#nullable enable

using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseCustomerOutOfStockSubscriptionController(
    ICustomerService customerService,
    ICustomerOutOfStockSubscriptionViewModelService customerOutOfStockSubscriptionViewModelService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerScopedController(customerService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> OutOfStockSubscriptionList(DataSourceRequest command, string customerId)
    {
        if (Scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var (outOfStockSubscriptionModels, totalCount) =
            await customerOutOfStockSubscriptionViewModelService.PrepareOutOfStockSubscriptionModel(customerId,
                command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = outOfStockSubscriptionModels.ToList(), Total = totalCount };
        return Json(gridModel);
    }
}
