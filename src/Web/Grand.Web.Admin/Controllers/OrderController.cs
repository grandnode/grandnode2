using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Concrete host subclass of BaseOrderManagementController (ARCH-001 Order consolidation). This class
// supplies Admin's DI wiring plus the attributes that used to arrive transitively via
// BaseAdminController - BaseOrderManagementController can't inherit any single host's base controller
// (it's shared across Admin/Store, each with a different [Area]/[Authorize*] pair), so each subclass
// restates its own host's attribute set explicitly, same pattern as CategoryController/ProductController.
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class OrderController(
    IOrderViewModelService orderViewModelService,
    IOrderService orderService,
    IOrderStatusService orderStatusService,
    ITranslationService translationService,
    IContextAccessor contextAccessor,
    IPdfService pdfService,
    IMediator mediator,
    IAdminDataScope<Order> scope,
    IExportManager<Order> exportManager)
    : BaseOrderManagementController(orderViewModelService, orderService, orderStatusService,
        translationService, contextAccessor, pdfService, mediator, scope)
{
    // Admin-exclusive: Store already holds the Export/Delete PermissionActionName grants (used by
    // PdfInvoiceAll/PdfInvoiceSelected/Delete today), so these three stay off both base classes -
    // see plan's Global Constraints and spec §3.6.

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportExcelAll(OrderListModel model)
    {
        var orders = await OrderViewModelService.PrepareOrders(model);
        try
        {
            var bytes = await exportManager.Export(orders);
            return File(bytes, "text/xls", "orders.xlsx");
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportExcelSelected(string selectedIds)
    {
        var orders = new List<Order>();
        if (selectedIds != null)
        {
            var ids = selectedIds.Split([','], StringSplitOptions.RemoveEmptyEntries).ToArray();
            orders.AddRange(await OrderService.GetOrdersByIds(ids));
        }

        var bytes = await exportManager.Export(orders);
        return File(bytes, "text/xls", "orders.xlsx");
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteSelected(
        ICollection<string> selectedIds,
        [FromServices] IShipmentService shipmentService)
    {
        if (selectedIds != null)
        {
            var orders = new List<Order>();
            orders.AddRange(await OrderService.GetOrdersByIds(selectedIds.ToArray()));
            foreach (var order in orders)
            {
                var shipments = await shipmentService.GetShipmentsByOrder(order.Id);
                if (shipments.Any())
                    Error("Some orders is in associated with shipments. Please delete it first.");
                else
                    await Mediator.Send(new DeleteOrderCommand { Order = order });
            }
        }

        return Json(new { Result = true });
    }
}
