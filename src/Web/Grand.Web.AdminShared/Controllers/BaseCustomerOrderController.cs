#nullable enable

using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseCustomerOrderController(
    ICustomerService customerService,
    IOrderService orderService,
    IOrderViewModelService orderViewModelService,
    IRequestPermissionService permissionService,
    IAdminDataScope<Customer> scope)
    : BaseCustomerScopedController(customerService, scope)
{
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> OrderList(string customerId, DataSourceRequest command)
    {
        if (Scope.DefaultStoreId is not null)
        {
            var (_, denied) = await LoadAuthorizedCustomer(customerId);
            if (denied != null) return Json(new DataSourceResult { Data = null, Total = 0 });
        }
        else if (!await permissionService.Authorize(StandardPermission.ManageOrders))
        {
            return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var model = new OrderListModel {
            CustomerId = customerId, StoreId = Scope.DefaultStoreId ?? ""
        };
        var (orderModels, totalCount) = await orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult { Data = orderModels.ToList(), Total = totalCount };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> OrderDetails(string orderId)
    {
        Order order;
        if (Scope.DefaultStoreId is null)
        {
            if (!await permissionService.Authorize(StandardPermission.ManageOrders))
                return Json(new DataSourceResult { Data = null, Total = 0 });

            order = await orderService.GetOrderById(orderId);
            if (order == null) throw new ArgumentException("No order found with the specified id");
        }
        else
        {
            order = await orderService.GetOrderById(orderId);
            if (order == null || order.StoreId != Scope.DefaultStoreId)
                return Json(new DataSourceResult { Data = null, Total = 0 });
        }

        var ordermodel = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(ordermodel, order);
        var gridModel = new DataSourceResult { Data = ordermodel.Items, Total = ordermodel.Items.Count };
        return Json(gridModel);
    }
}
