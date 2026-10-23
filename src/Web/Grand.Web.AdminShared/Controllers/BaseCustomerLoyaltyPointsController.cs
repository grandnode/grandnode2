#nullable enable

using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseCustomerLoyaltyPointsController(
    ICustomerService customerService,
    ICustomerLoyaltyPointsViewModelService customerLoyaltyPointsViewModelService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerScopedController(customerService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> LoyaltyPointsHistorySelect(string customerId)
    {
        var (customer, _) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) throw new ArgumentException("No customer found with the specified id");

        var model = (await customerLoyaltyPointsViewModelService.PrepareLoyaltyPointsHistoryModel(customerId)).ToList();
        var gridModel = new DataSourceResult { Data = model, Total = model.Count };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> LoyaltyPointsHistoryAdd(string customerId, string storeId,
        int addLoyaltyPointsValue, string addLoyaltyPointsMessage)
    {
        var (customer, _) = await LoadAuthorizedCustomer(customerId);
        if (customer is null) return Json(new { Result = false });

        // Store scope always forces its own StaffStoreId, ignoring the caller-supplied storeId —
        // ports the original controller's `CurrentStoreId` usage. Admin has no store concept here
        // and keeps using whatever the caller (its own store picker) submitted.
        var effectiveStoreId = Scope.DefaultStoreId ?? storeId;

        await customerLoyaltyPointsViewModelService.InsertLoyaltyPointsHistory(customer, effectiveStoreId,
            addLoyaltyPointsValue, addLoyaltyPointsMessage);
        return Json(new { Result = true });
    }
}
