using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Vendor.Extensions;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Models.Orders;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

[PermissionAuthorize(PermissionSystemName.Orders)]
public class OrderController : BaseVendorController
{
    #region Ctor

    public OrderController(
        IOrderViewModelService orderViewModelService,
        IOrderService orderService,
        ITranslationService translationService,
        IWorkContext workContext,
        IPdfService pdfService)
    {
        _orderViewModelService = orderViewModelService;
        _orderService = orderService;
        _translationService = translationService;
        _workContext = workContext;
        _pdfService = pdfService;
    }

    #endregion

    #region Fields

    private readonly IOrderViewModelService _orderViewModelService;
    private readonly IOrderService _orderService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;
    private readonly IPdfService _pdfService;

    #endregion

    #region Order list

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public async Task<IActionResult> List(int? orderStatusId = null,
        int? paymentStatusId = null, int? shippingStatusId = null,
        DateTime? startDate = null, string code = null)
    {
        var model = await _orderViewModelService.PrepareOrderListModel(orderStatusId, paymentStatusId, shippingStatusId,
            startDate, code);
        return View(model);
    }

    public async Task<IActionResult> ProductSearchAutoComplete(string term,
        [FromServices] IProductService productService)
    {
        const int searchTermMinimumLength = 3;
        if (string.IsNullOrWhiteSpace(term) || term.Length < searchTermMinimumLength)
            return Content("");

        //products
        const int productNumber = 15;
        var products = (await productService.SearchProducts(
            vendorId: _workContext.CurrentVendor.Id,
            keywords: term,
            pageSize: productNumber,
            showHidden: true)).products;

        var result = (from p in products
                select new {
                    label = p.Name,
                    productid = p.Id
                })
            .ToList();
        return Json(result);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> OrderList(DataSourceRequest command, OrderListModel model)
    {
        var (orderModels, totalCount) =
            await _orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = orderModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GoToOrderId(OrderListModel model)
    {
        Order order = null;
        if (int.TryParse(model.GoDirectlyToNumber, out var orderNumber))
        {
            order = await _orderService.GetOrderByNumber(orderNumber);
        }
        else
        {
            var orders = await _orderService.GetOrdersByCode(model.GoDirectlyToNumber);
            switch (orders.Count)
            {
                case > 1:
                    return RedirectToAction("List", new { Code = model.GoDirectlyToNumber });
                case 1:
                    order = orders.FirstOrDefault();
                    break;
            }
        }

        return RedirectToAction("Edit", "Order", new { id = order?.Id });
    }

    #endregion

    #region Order details

    #region Edit, delete

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var order = await _orderService.GetOrderById(id);
        if (order == null || order.Deleted || !_workContext.HasAccessToOrder(order))
            //No order found with the specified id
            return RedirectToAction("List");

        var model = new OrderModel();
        await _orderViewModelService.PrepareOrderDetailsModel(model, order);

        return View(model);
    }

    public async Task<IActionResult> PdfInvoice(string orderId)
    {
        var order = await _orderService.GetOrderById(orderId);
        //No order found with the specified id
        if (order == null || order.Deleted || !_workContext.HasAccessToOrder(order)) return RedirectToAction("List");

        var orders = new List<Order> {
            order
        };
        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await _pdfService.PrintOrdersToPdf(stream, orders, _workContext.WorkingLanguage.Id,
                _workContext.CurrentVendor.Id);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", $"order_{order.Id}.pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> PdfInvoiceAll(OrderListModel model)
    {
        //load orders
        var orders = await _orderViewModelService.PrepareOrders(model);
        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await _pdfService.PrintOrdersToPdf(stream, orders, _workContext.WorkingLanguage.Id,
                _workContext.CurrentVendor.Id);
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
            var ids = selectedIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x)
                .ToArray();
            orders.AddRange(await _orderService.GetOrdersByIds(ids));
        }

        //a vendor should have access only to his products
        orders = orders.Where(_workContext.HasAccessToOrder).ToList();

        //ensure that we at least one order selected
        if (orders.Count == 0)
        {
            Error(_translationService.GetResource("Vendor.Orders.PdfInvoice.NoOrders"));
            return RedirectToAction("List");
        }

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await _pdfService.PrintOrdersToPdf(stream, orders, _workContext.WorkingLanguage.Id,
                _workContext.CurrentVendor.Id);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", "orders.pdf");
    }

    #endregion

    #endregion
}