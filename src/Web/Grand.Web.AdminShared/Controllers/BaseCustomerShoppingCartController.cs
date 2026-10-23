#nullable enable

using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseCustomerShoppingCartController(
    ICustomerService customerService,
    ICustomerShoppingCartViewModelService customerShoppingCartViewModelService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerScopedController(customerService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GetCartList(string customerId, int cartTypeId)
    {
        var (customer, _) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) return Json(new DataSourceResult { Data = null, Total = 0 });

        var cart = await customerShoppingCartViewModelService.PrepareShoppingCartItemModel(customer, cartTypeId);
        var gridModel = new DataSourceResult { Data = cart, Total = cart.Count };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> UpdateCart(string id, string customerId, double? unitPriceValue)
    {
        var (customer, _) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        var warnings = await customerShoppingCartViewModelService.UpdateCart(customer, id, unitPriceValue);
        if (warnings.Any()) return ErrorForKendoGridJson(string.Join(",", warnings));
        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeleteCart(string id, string customerId)
    {
        var (customer, _) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) throw new ArgumentException("No customer found with the specified id", nameof(customerId));

        await customerShoppingCartViewModelService.DeleteCart(customer, id);
        return new JsonResult("");
    }
}
