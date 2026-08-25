using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     Every mutating Order action. Base for Admin and Store only — Vendor's concrete controller
///     inherits <see cref="BaseOrderController" /> directly, so none of these actions exist on its
///     type at all (not permission-gated, genuinely absent — see ARCH-001 Order consolidation spec
///     §3.5).
/// </summary>
public abstract class BaseOrderManagementController(
    IOrderViewModelService orderViewModelService,
    IOrderService orderService,
    IOrderStatusService orderStatusService,
    ITranslationService translationService,
    IContextAccessor contextAccessor,
    IPdfService pdfService,
    IMediator mediator,
    IAdminDataScope<Order> scope)
    : BaseOrderController(orderViewModelService, orderService, translationService, contextAccessor,
        pdfService, scope)
{
    // Exposed for Grand.Web.Admin's concrete OrderController subclass (Task 17), which calls
    // Mediator.Send(...) directly - primary-constructor parameters aren't visible to derived
    // classes by name in C#.
    protected IMediator Mediator => mediator;

    #region Payments and other order workflow

    [PermissionAuthorizeAction(PermissionActionName.Cancel)]
    [HttpGet]
    public async Task<IActionResult> CancelOrder(string id)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        try
        {
            await mediator.Send(new CancelOrderCommand { Order = order, NotifyCustomer = true });
            Success("Successfully canceled order");
            return RedirectToAction("Edit", "Order", new { id });
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("Edit", "Order", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SaveOrderTags(OrderModel orderModel)
    {
        var (order, denied) = await LoadAuthorizedOrder(orderModel.Id);
        if (denied != null) return denied;

        try
        {
            await orderViewModelService.SaveOrderTags(order, orderModel.OrderTags);
            var model = new OrderModel();
            await orderViewModelService.PrepareOrderDetailsModel(model, order);
            return RedirectToAction("Edit", "Order", new { id = order.Id });
        }
        catch (Exception exception)
        {
            Error(exception, false);
            return RedirectToAction("Edit", "Order", new { id = order.Id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ChangeOrderStatus(string id, OrderModel model)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        try
        {
            var status = await orderStatusService.GetByStatusId(model.OrderStatusId);
            ArgumentNullException.ThrowIfNull(status);

            order.OrderStatusId = model.OrderStatusId;
            await orderService.UpdateOrder(order);

            await orderService.InsertOrderNote(new OrderNote {
                Note = $"Order status has been edited. New status: {status.Name}",
                DisplayToCustomer = false,
                OrderId = order.Id
            });
            model = new OrderModel();
            await orderViewModelService.PrepareOrderDetailsModel(model, order);
            return RedirectToAction("Edit", "Order", new { id });
        }
        catch (Exception exc)
        {
            Error(exc, false);
            return RedirectToAction("Edit", "Order", new { id });
        }
    }

    #endregion

    #region Edit, delete

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(OrderDeleteModel model)
    {
        var (order, denied) = await LoadAuthorizedOrder(model.Id);
        if (denied != null) return denied;

        if (ModelState.IsValid)
        {
            await mediator.Send(new DeleteOrderCommand { Order = order });
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", "Order", new { model.Id });
    }

    #endregion
}
