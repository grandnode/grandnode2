using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Orders)]
public class OrderController(
    IOrderViewModelService orderViewModelService,
    IOrderService orderService,
    IOrderStatusService orderStatusService,
    ITranslationService translationService,
    IWorkContext workContext,
    IPdfService pdfService,
    IGroupService groupService,
    IExportManager<Order> exportManager,
    IMediator mediator)
    : BaseAdminController
{
    #region Utilities

    protected virtual async Task<bool> CheckSalesManager(Order order)
    {
        return await groupService.IsSalesManager(workContext.CurrentCustomer)
               && workContext.CurrentCustomer.SeId != order.SeId;
    }

    #endregion

    #region Fields

    #endregion

    #region Ctor

    #endregion

    #region Order list

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public async Task<IActionResult> List(int? orderStatusId = null,
        int? paymentStatusId = null, int? shippingStatusId = null, DateTime? startDate = null, string code = null)
    {
        var model = await orderViewModelService.PrepareOrderListModel(orderStatusId, paymentStatusId, shippingStatusId,
            startDate, workContext.CurrentCustomer.StaffStoreId, code);
        return View(model);
    }

    public async Task<IActionResult> ProductSearchAutoComplete(string term,
        [FromServices] IProductService productService)
    {
        const int searchTermMinimumLength = 3;
        if (string.IsNullOrWhiteSpace(term) || term.Length < searchTermMinimumLength)
            return Content("");

        var storeId = string.Empty;
        if (await groupService.IsStaff(workContext.CurrentCustomer))
            storeId = workContext.CurrentCustomer.StaffStoreId;

        //products
        const int productNumber = 15;
        var products = (await productService.SearchProducts(
            storeId: storeId,
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
        if (await groupService.IsStaff(workContext.CurrentCustomer))
            model.StoreId = workContext.CurrentCustomer.StaffStoreId;

        var (orderModels, totalCount) =
            await orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);

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
        int.TryParse(model.GoDirectlyToNumber, out var orderNumber);
        if (orderNumber > 0) order = await orderService.GetOrderByNumber(orderNumber);
        var orders = await orderService.GetOrdersByCode(model.GoDirectlyToNumber);
        switch (orders.Count)
        {
            case > 1:
                return RedirectToAction("List", new { Code = model.GoDirectlyToNumber });
            case 1:
                order = orders.FirstOrDefault();
                break;
        }

        if (order == null || await CheckSalesManager(order))
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        return RedirectToAction("Edit", "Order", new { id = order.Id });
    }

    #endregion

    #region Export

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportExcelAll(OrderListModel model)
    {
        if (await groupService.IsStaff(workContext.CurrentCustomer))
            model.StoreId = workContext.CurrentCustomer.StaffStoreId;

        //load orders
        var orders = await orderViewModelService.PrepareOrders(model);
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
            var ids = selectedIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x)
                .ToArray();
            orders.AddRange(await orderService.GetOrdersByIds(ids));
        }

        if (await groupService.IsStaff(workContext.CurrentCustomer))
            orders = orders.Where(x => x.StoreId == workContext.CurrentCustomer.StaffStoreId).ToList();
        var bytes = await exportManager.Export(orders);
        return File(bytes, "text/xls", "orders.xlsx");
    }

    #endregion

    #region Order details

    #region Payments and other order workflow

    [PermissionAuthorizeAction(PermissionActionName.Cancel)]
    [HttpGet]
    public async Task<IActionResult> CancelOrder(string id)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");
        try
        {
            await mediator.Send(new CancelOrderCommand { Order = order, NotifyCustomer = true });

            Success("Successfully canceled order");
            return RedirectToAction("Edit", "Order", new { id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc);
            return RedirectToAction("Edit", "Order", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SaveOrderTags(OrderModel orderModel)
    {
        var order = await orderService.GetOrderById(orderModel.Id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        try
        {
            await orderViewModelService.SaveOrderTags(order, orderModel.OrderTags);

            var model = new OrderModel();
            await orderViewModelService.PrepareOrderDetailsModel(model, order);
            return RedirectToAction("Edit", "Order", new { id = order.Id });
        }
        catch (Exception exception)
        {
            //error
            Error(exception, false);
            return RedirectToAction("Edit", "Order", new { id = order.Id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ChangeOrderStatus(string id, OrderModel model)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        try
        {
            var status = await orderStatusService.GetByStatusId(model.OrderStatusId);
            if (status == null)
                throw new ArgumentNullException(nameof(status));

            order.OrderStatusId = model.OrderStatusId;
            await orderService.UpdateOrder(order);

            //add a note
            await orderService.InsertOrderNote(new OrderNote {
                Note = $"Order status has been edited. New status: {status?.Name}",
                DisplayToCustomer = false,
                OrderId = order.Id
            });
            model = new OrderModel();
            await orderViewModelService.PrepareOrderDetailsModel(model, order);
            return RedirectToAction("Edit", "Order", new { id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc, false);
            return RedirectToAction("Edit", "Order", new { id });
        }
    }

    #endregion

    #region Edit, delete

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || order.Deleted || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        var model = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(OrderDeleteModel model)
    {
        var order = await orderService.GetOrderById(model.Id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await mediator.Send(new DeleteOrderCommand { Order = order });

            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", "Order", new { model.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteSelected(
        ICollection<string> selectedIds,
        [FromServices] IShipmentService shipmentService)
    {
        if (await groupService.IsStaff(workContext.CurrentCustomer))
            return RedirectToAction("List", "Order");

        if (selectedIds != null)
        {
            var orders = new List<Order>();
            orders.AddRange(await orderService.GetOrdersByIds(selectedIds.ToArray()));
            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var shipments = await shipmentService.GetShipmentsByOrder(order.Id);
                if (shipments.Any())
                    Error("Some orders is in associated with shipments. Please delete it first.");

                if (!shipments.Any()) await mediator.Send(new DeleteOrderCommand { Order = order });
            }
        }

        return Json(new { Result = true });
    }

    public async Task<IActionResult> PdfInvoice(string orderId)
    {
        var order = await orderService.GetOrderById(orderId);
        if ((await groupService.IsStaff(workContext.CurrentCustomer) &&
             order.StoreId != workContext.CurrentCustomer.StaffStoreId) ||
            await CheckSalesManager(order)) return RedirectToAction("List");

        var orders = new List<Order> {
            order
        };
        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintOrdersToPdf(stream, orders, workContext.WorkingLanguage.Id);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", $"order_{order.Id}.pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> PdfInvoiceAll(OrderListModel model)
    {
        //load orders
        var orders = await orderViewModelService.PrepareOrders(model);
        if (await groupService.IsStaff(workContext.CurrentCustomer))
            orders = orders.Where(x => x.StoreId == workContext.CurrentCustomer.StaffStoreId).ToList();

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintOrdersToPdf(stream, orders, workContext.WorkingLanguage.Id, model.VendorId);
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
            orders.AddRange(await orderService.GetOrdersByIds(ids));
        }

        if (await groupService.IsStaff(workContext.CurrentCustomer))
            orders = orders.Where(x => x.StoreId == workContext.CurrentCustomer.StaffStoreId).ToList();

        //ensure that we at least one order selected
        if (orders.Count == 0)
        {
            Error(translationService.GetResource("Admin.Orders.PdfInvoice.NoOrders"));
            return RedirectToAction("List");
        }

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintOrdersToPdf(stream, orders, workContext.WorkingLanguage.Id);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", "orders.pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> EditOrderTotals(string id, OrderModel model)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        order.OrderSubtotalInclTax = model.OrderSubtotalInclTaxValue;
        order.OrderSubtotalExclTax = model.OrderSubtotalExclTaxValue;
        order.OrderSubTotalDiscountInclTax = model.OrderSubTotalDiscountInclTaxValue;
        order.OrderSubTotalDiscountExclTax = model.OrderSubTotalDiscountExclTaxValue;
        order.OrderShippingInclTax = model.OrderShippingInclTaxValue;
        order.OrderShippingExclTax = model.OrderShippingExclTaxValue;
        order.PaymentMethodAdditionalFeeInclTax = model.PaymentMethodAdditionalFeeInclTaxValue;
        order.PaymentMethodAdditionalFeeExclTax = model.PaymentMethodAdditionalFeeExclTaxValue;
        order.OrderTax = model.TaxValue;
        order.OrderDiscount = model.OrderTotalDiscountValue;
        order.OrderTotal = model.OrderTotalValue;
        order.CurrencyRate = model.CurrencyRate;
        await orderService.UpdateOrder(order);

        //add a note
        await orderService.InsertOrderNote(new OrderNote {
            Note = "Order totals have been edited",
            DisplayToCustomer = false,
            OrderId = order.Id
        });

        await orderViewModelService.PrepareOrderDetailsModel(model, order);
        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> EditShippingMethod(string id, OrderModel model)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        order.ShippingMethod = model.ShippingMethod;
        await orderService.UpdateOrder(order);

        //add a note
        await orderService.InsertOrderNote(new OrderNote {
            Note = "Shipping method has been edited",
            DisplayToCustomer = false,
            OrderId = order.Id
        });
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> EditUserFields(string id, OrderModel model)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        order.UserFields = model.UserFields;

        await orderService.UpdateOrder(order);

        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SaveOrderItem(string id, OrderItemsModel model)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });
        if (order.OrderStatusId == (int)OrderStatusSystem.Cancelled)
        {
            Error("You can't edit position when order is canceled");
            return RedirectToAction("Edit", "Order", new { id });
        }

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        var itemModel = model.Items.FirstOrDefault(x => x.Id == model.OrderItemId);
        if (itemModel.Quantity == 0 || (orderItem.OpenQty != orderItem.Quantity && orderItem.IsShipEnabled))
        {
            Error("You can't change quantity");
            return RedirectToAction("Edit", "Order", new { id });
        }

        if (orderItem.Quantity == itemModel.Quantity && orderItem.UnitPriceExclTax == itemModel.UnitPriceExclTaxValue)
        {
            Error("Nothing has been changed");
            return RedirectToAction("Edit", "Order", new { id });
        }

        orderItem.Quantity = itemModel.Quantity;
        orderItem.OpenQty = itemModel.Quantity;

        if (orderItem.UnitPriceExclTax != itemModel.UnitPriceExclTaxValue)
        {
            orderItem.UnitPriceExclTax = itemModel.UnitPriceExclTaxValue;
            orderItem.UnitPriceInclTax =
                Math.Round(orderItem.UnitPriceExclTax * orderItem.TaxRate / 100 + orderItem.UnitPriceExclTax, 2);
            orderItem.PriceInclTax = Math.Round(orderItem.UnitPriceInclTax * orderItem.Quantity, 2);
            orderItem.PriceExclTax = Math.Round(orderItem.UnitPriceExclTax * orderItem.Quantity, 2);

            orderItem.DiscountAmountInclTax = 0;
            orderItem.DiscountAmountExclTax = 0;
        }
        else
        {
            orderItem.PriceInclTax = Math.Round(orderItem.UnitPriceInclTax * orderItem.Quantity, 2);
            orderItem.PriceExclTax = Math.Round(orderItem.UnitPriceExclTax * orderItem.Quantity, 2);

            orderItem.DiscountAmountInclTax = 0;
            orderItem.DiscountAmountExclTax = 0;
        }

        await mediator.Send(new UpdateOrderItemCommand { Order = order, OrderItem = orderItem });

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeleteOrderItem(string id, string orderItemId)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        var result = await mediator.Send(new DeleteOrderItemCommand { Order = order, OrderItem = orderItem });
        if (result.error)
            Error(result.message);

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CancelOrderItem(string id, string orderItemId)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");


        var result = await mediator.Send(new CancelOrderItemCommand { Order = order, OrderItem = orderItem });
        if (result.error)
            Error(result.message);
        else
            Success("The order item was successfully canceled");

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ResetDownloadCount(string id, string orderItemId)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        orderItem.DownloadCount = 0;
        await orderService.UpdateOrder(order);
        var model = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ActivateDownloadItem(string id, string orderItemId)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        orderItem.IsDownloadActivated = !orderItem.IsDownloadActivated;
        await orderService.UpdateOrder(order);
        var model = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UploadLicenseFilePopup(string id, string orderItemId,
        [FromServices] IProductService productService)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        var product = await productService.GetProductByIdIncludeArch(orderItem.ProductId);

        if (!product.IsDownload)
            throw new ArgumentException("Product is not downloadable");
        var model = new OrderModel.UploadLicenseModel {
            LicenseDownloadId = !string.IsNullOrEmpty(orderItem.LicenseDownloadId) ? orderItem.LicenseDownloadId : "",
            OrderId = order.Id,
            OrderItemId = orderItem.Id
        };

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> UploadLicenseFilePopup(OrderModel.UploadLicenseModel model)
    {
        var order = await orderService.GetOrderById(model.OrderId);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id = order.Id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        //attach license
        orderItem.LicenseDownloadId = !string.IsNullOrEmpty(model.LicenseDownloadId) ? model.LicenseDownloadId : null;
        await orderService.UpdateOrder(order);

        //success
        ViewBag.RefreshPage = true;

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeleteLicenseFilePopup(OrderModel.UploadLicenseModel model)
    {
        var order = await orderService.GetOrderById(model.OrderId);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id = model.OrderId });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        //attach license
        orderItem.LicenseDownloadId = null;
        await orderService.UpdateOrder(order);

        //success
        ViewBag.RefreshPage = true;

        return RedirectToAction("Edit", "Order", new { id = model.OrderId });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddProductToOrder(string orderId)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        var model = await orderViewModelService.PrepareAddOrderProductModel(order);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddProductToOrder(DataSourceRequest command, OrderModel.AddOrderProductModel model,
        [FromServices] IProductService productService)
    {
        var categoryIds = new List<string>();
        if (!string.IsNullOrEmpty(model.SearchCategoryId))
            categoryIds.Add(model.SearchCategoryId);

        var storeId = string.Empty;
        if (await groupService.IsStaff(workContext.CurrentCustomer)) storeId = workContext.CurrentCustomer.StaffStoreId;

        var gridModel = new DataSourceResult();
        var products = (await productService.SearchProducts(categoryIds: categoryIds,
            storeId: storeId,
            brandId: model.SearchBrandId,
            collectionId: model.SearchCollectionId,
            productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize,
            showHidden: true)).products;
        gridModel.Data = products.Select(x =>
        {
            var productModel = new OrderModel.AddOrderProductModel.ProductModel {
                Id = x.Id,
                Name = x.Name,
                Sku = x.Sku
            };

            return productModel;
        });
        gridModel.Total = products.TotalCount;

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddProductToOrderDetails(string orderId, string productId)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        var model = await orderViewModelService.PrepareAddProductToOrderModel(order, productId);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddProductToOrderDetails(AddProductToOrderModel model)
    {
        var order = await orderService.GetOrderById(model.OrderId);
        if (order == null || await CheckSalesManager(order))
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        var warnings = await orderViewModelService.AddProductToOrderDetails(model);
        if (!warnings.Any())
            //redirect to order details page
            return RedirectToAction("Edit", "Order", new { id = model.OrderId });

        //errors
        var result = await orderViewModelService.PrepareAddProductToOrderModel(order, model.ProductId);
        result.Warnings.AddRange(warnings);
        return View(result);
    }

    #endregion

    #endregion

    #region Addresses

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> AddressEdit(string addressId, string orderId, bool billingAddress)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        var address = new Address();
        switch (billingAddress)
        {
            case true when order.BillingAddress != null:
            {
                if (order.BillingAddress.Id == addressId)
                    address = order.BillingAddress;
                break;
            }
            case false when order.ShippingAddress != null:
            {
                if (order.ShippingAddress.Id == addressId)
                    address = order.ShippingAddress;
                break;
            }
        }

        if (address == null)
            throw new ArgumentException("No address found with the specified id", nameof(addressId));

        var model = await orderViewModelService.PrepareOrderAddressModel(order, address);
        model.BillingAddress = billingAddress;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressEdit(OrderAddressModel model,
        [FromServices] IAddressAttributeService addressAttributeService,
        [FromServices] IAddressAttributeParser addressAttributeParser)
    {
        var order = await orderService.GetOrderById(model.OrderId);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        var address = new Address();
        switch (model.BillingAddress)
        {
            case true when order.BillingAddress != null:
            {
                if (order.BillingAddress.Id == model.Address.Id)
                    address = order.BillingAddress;
                break;
            }
            case false when order.ShippingAddress != null:
            {
                if (order.ShippingAddress.Id == model.Address.Id)
                    address = order.ShippingAddress;
                break;
            }
        }

        if (ModelState.IsValid)
        {
            var customAttributes =
                await model.Address.ParseCustomAddressAttributes(addressAttributeParser, addressAttributeService);
            await orderViewModelService.UpdateOrderAddress(order, address, model, customAttributes);
            return RedirectToAction("AddressEdit",
                new { addressId = model.Address.Id, orderId = model.OrderId, model.BillingAddress });
        }

        //If we got this far, something failed, redisplay form
        model = await orderViewModelService.PrepareOrderAddressModel(order, address);
        model.BillingAddress = model.BillingAddress;
        return View(model);
    }

    #endregion

    #region Order notes

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> OrderNotesSelect(string orderId, DataSourceRequest command)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            throw new ArgumentException("No order found with the specified id");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return Content("");
        //order notes
        var orderNoteModels = await orderViewModelService.PrepareOrderNotes(order);
        var gridModel = new DataSourceResult {
            Data = orderNoteModels,
            Total = orderNoteModels.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> OrderNoteAdd(string orderId, string downloadId, bool displayToCustomer,
        string message)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return Json(new { Result = false });

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return Json(new { Result = false });
        await orderViewModelService.InsertOrderNote(order, downloadId, displayToCustomer, message);

        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> OrderNoteDelete(string id, string orderId)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            throw new ArgumentException("No order found with the specified id");

        if (await groupService.IsStaff(workContext.CurrentCustomer) &&
            order.StoreId != workContext.CurrentCustomer.StaffStoreId) return Json(new { Result = false });

        await orderViewModelService.DeleteOrderNote(order, id);

        return new JsonResult("");
    }

    #endregion
}