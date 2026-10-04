using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Orders)]
[AutoValidateAntiforgeryToken]
public abstract class BaseOrderController(
    IOrderViewModelService orderViewModelService,
    IOrderService orderService,
    ITranslationService translationService,
    IContextAccessor contextAccessor,
    IPdfService pdfService,
    IAdminDataScope<Order> scope)
    : BaseController
{
    // Exposed for BaseOrderManagementController (primary-constructor parameters aren't visible to
    // derived classes by name in C#).
    protected IOrderViewModelService OrderViewModelService => orderViewModelService;
    protected IOrderService OrderService => orderService;
    protected ITranslationService TranslationService => translationService;
    protected IContextAccessor ContextAccessor => contextAccessor;
    protected IPdfService PdfService => pdfService;
    protected IAdminDataScope<Order> Scope => scope;

    /// <summary>DRY replacement for the ~20x-duplicated
    /// "load order, redirect to List if not found or not authorized" pattern found identically in
    /// both Admin's and Store's original controllers (every action in both files redirects to
    /// "List", never "Edit", on either condition). Not a behavior change — every call site below
    /// still individually returns RedirectToAction("List") exactly as both originals did.</summary>
    protected async Task<(Order order, IActionResult denied)> LoadAuthorizedOrder(string id)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null) return (null, RedirectToAction("List"));
        if (!await scope.HasAccess(order)) return (null, RedirectToAction("List"));
        return (order, null);
    }

    #region Order list

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List(int? orderStatusId = null, int? paymentStatusId = null,
        int? shippingStatusId = null, DateTime? startDate = null, string code = null)
    {
        var model = await orderViewModelService.PrepareOrderListModel(orderStatusId, paymentStatusId,
            shippingStatusId, startDate, scope.DefaultStoreId ?? "", code);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> OrderList(DataSourceRequest command, OrderListModel model)
    {
        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;
        if (scope.DefaultVendorId is not null) model.VendorId = scope.DefaultVendorId;

        var (orderModels, totalCount) =
            await orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);

        var gridModel = new DataSourceResult {
            Data = orderModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    #endregion

    #region Order details (view-only)

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || order.Deleted) return RedirectToAction("List");
        if (!await scope.HasAccess(order)) return RedirectToAction("List");

        var model = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        return View(model);
    }

    public async Task<IActionResult> PdfInvoice(string orderId)
    {
        var (order, denied) = await LoadAuthorizedOrder(orderId);
        if (denied != null) return denied;

        var orders = new List<Order> { order };
        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintOrdersToPdf(stream, orders, contextAccessor.WorkContext.WorkingLanguage.Id,
                scope.DefaultVendorId);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", $"order_{order.Id}.pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> PdfInvoiceAll(OrderListModel model)
    {
        var orders = await orderViewModelService.PrepareOrders(model);
        // Store's original code post-filtered by StoreId here even though PrepareOrders already
        // takes model.StoreId as a search filter - keep the extra filter for defense in depth,
        // matching Store's original exactly; harmless no-op for Admin/Vendor (DefaultStoreId null).
        if (scope.DefaultStoreId is not null)
            orders = orders.Where(x => x.StoreId == scope.DefaultStoreId).ToList();

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintOrdersToPdf(stream, orders, contextAccessor.WorkContext.WorkingLanguage.Id,
                scope.DefaultVendorId ?? model.VendorId);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", "orders.pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> PdfInvoiceSelected(string selectedIds)
    {
        var orders = new List<Order>();
        if (selectedIds != null)
        {
            var ids = selectedIds.Split([','], StringSplitOptions.RemoveEmptyEntries).ToArray();
            orders.AddRange(await orderService.GetOrdersByIds(ids));
        }

        // Store filters by StoreId; Vendor's original filtered by HasAccessToOrder (any-item
        // vendor match); Admin's original has no filter here at all. scope.HasAccess already
        // expresses all three checks per-host, so applying it unconditionally is a deliberate,
        // disclosed, security-positive behavior change for Admin: it closes a pre-existing gap
        // where a Sales Manager could previously export any order id via a crafted selectedIds
        // list, bypassing the Sales-Manager scoping that AdminOrderDataScope.HasAccess enforces
        // everywhere else in this controller.
        var accessible = new List<Order>();
        foreach (var order in orders)
            if (await scope.HasAccess(order))
                accessible.Add(order);
        orders = accessible;

        if (orders.Count == 0)
        {
            Error(translationService.GetResource($"{scope.ResourceKeyPrefix}.Orders.PdfInvoice.NoOrders"));
            return RedirectToAction("List");
        }

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintOrdersToPdf(stream, orders, contextAccessor.WorkContext.WorkingLanguage.Id,
                scope.DefaultVendorId);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", "orders.pdf");
    }

    public async Task<IActionResult> ProductSearchAutoComplete(string term,
        [FromServices] Grand.Business.Core.Interfaces.Catalog.Products.IProductService productService)
    {
        const int searchTermMinimumLength = 3;
        if (string.IsNullOrWhiteSpace(term) || term.Length < searchTermMinimumLength)
            return Content("");

        const int productNumber = 15;
        var products = (await productService.SearchProducts(
            storeId: scope.DefaultStoreId,
            vendorId: scope.DefaultVendorId,
            keywords: term,
            pageSize: productNumber,
            showHidden: true)).products;

        var result = products.Select(p => new { label = p.Name, productid = p.Id }).ToList();
        return Json(result);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GoToOrderId(OrderListModel model)
    {
        Order order = null;
        int.TryParse(model.GoDirectlyToNumber, out var orderNumber);
        if (orderNumber > 0) order = await orderService.GetOrderByNumber(orderNumber);
        else
        {
            var orders = await orderService.GetOrdersByCode(model.GoDirectlyToNumber);
            switch (orders.Count)
            {
                case > 1: return RedirectToAction("List", new { Code = model.GoDirectlyToNumber });
                case 1: order = orders.FirstOrDefault(); break;
                case 0: return RedirectToAction("List", new { Code = model.GoDirectlyToNumber });
            }
        }

        if (order == null || !await scope.HasAccess(order)) return RedirectToAction("List");

        return RedirectToAction("Edit", "Order", new { id = order.Id });
    }

    #endregion
}
