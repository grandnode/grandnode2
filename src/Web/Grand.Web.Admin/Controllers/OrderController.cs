using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Pdf;
using Grand.Business.Common.Services.Security;
using Grand.Business.System.Interfaces.ExportImport;
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Orders)]
    public partial class OrderController : BaseAdminController
    {
        #region Fields

        private readonly IOrderViewModelService _orderViewModelService;
        private readonly IOrderService _orderService;
        private readonly IOrderStatusService _orderStatusService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IPdfService _pdfService;
        private readonly IGroupService _groupService;
        private readonly IExportManager _exportManager;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        public OrderController(
            IOrderViewModelService orderViewModelService,
            IOrderService orderService,
            IOrderStatusService orderStatusService,
            ITranslationService translationService,
            IWorkContext workContext,
            IPdfService pdfService,
            IGroupService groupService,
            IExportManager exportManager,
            IMediator mediator)
        {
            _orderViewModelService = orderViewModelService;
            _orderService = orderService;
            _orderStatusService = orderStatusService;
            _translationService = translationService;
            _workContext = workContext;
            _groupService = groupService;
            _pdfService = pdfService;
            _exportManager = exportManager;
            _mediator = mediator;
        }

        #endregion

        #region Utilities

        protected virtual async Task<bool> CheckSalesManager(Order order)
        {
            return (await _groupService.IsSalesManager(_workContext.CurrentCustomer)
                && (_workContext.CurrentCustomer.SeId != order.SeId));
        }

        #endregion

        #region Order list

        public IActionResult Index() => RedirectToAction("List");

        public async Task<IActionResult> List(int? orderStatusId = null,
            int? paymentStatusId = null, int? shippingStatusId = null, DateTime? startDate = null, string code = null)
        {
            var model = await _orderViewModelService.PrepareOrderListModel(orderStatusId, paymentStatusId, shippingStatusId, startDate, _workContext.CurrentCustomer.StaffStoreId, code);
            return View(model);
        }

        public async Task<IActionResult> ProductSearchAutoComplete(string term, [FromServices] IProductService productService)
        {
            const int searchTermMinimumLength = 3;
            if (string.IsNullOrWhiteSpace(term) || term.Length < searchTermMinimumLength)
                return Content("");

            var storeId = string.Empty;
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            string vendorId = string.Empty;
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null)
            {
                vendorId = _workContext.CurrentVendor.Id;
            }
            //products
            const int productNumber = 15;
            var products = (await productService.SearchProducts(
                storeId: storeId,
                vendorId: vendorId,
                keywords: term,
                pageSize: productNumber,
                showHidden: true)).products;

            var result = (from p in products
                          select new
                          {
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
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.VendorId = _workContext.CurrentVendor.Id;
            }

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.StoreId = _workContext.CurrentCustomer.StaffStoreId;
            }

            var (orderModels, totalCount) = await _orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);

            var gridModel = new DataSourceResult
            {
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
            if (orderNumber > 0)
            {
                order = await _orderService.GetOrderByNumber(orderNumber);
            }
            var orders = await _orderService.GetOrdersByCode(model.GoDirectlyToNumber);
            if (orders.Count > 1)
            {
                return RedirectToAction("List", new { Code = model.GoDirectlyToNumber });
            }
            if (orders.Count == 1)
            {
                order = orders.FirstOrDefault();
            }
            if (order == null || await CheckSalesManager(order))
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            return RedirectToAction("Edit", "Order", new { id = order.Id });
        }

        #endregion

        #region Export

        [PermissionAuthorizeAction(PermissionActionName.Export)]
        [HttpPost]
        public async Task<IActionResult> ExportExcelAll(OrderListModel model)
        {
            //a vendor cannot export orders
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return AccessDeniedView();

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.StoreId = _workContext.CurrentCustomer.StaffStoreId;
            }

            //load orders
            var orders = await _orderViewModelService.PrepareOrders(model);
            try
            {
                byte[] bytes = _exportManager.ExportOrdersToXlsx(orders);
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
            //a vendor cannot export orders
            if (_workContext.CurrentVendor != null)
                return AccessDeniedView();

            var orders = new List<Order>();
            if (selectedIds != null)
            {
                var ids = selectedIds
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x)
                    .ToArray();
                orders.AddRange(await _orderService.GetOrdersByIds(ids));
            }
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                orders = orders.Where(x => x.StoreId == _workContext.CurrentCustomer.StaffStoreId).ToList();
            }
            byte[] bytes = _exportManager.ExportOrdersToXlsx(orders);
            return File(bytes, "text/xls", "orders.xlsx");
        }

        #endregion

        #region Order details

        #region Payments and other order workflow

        [PermissionAuthorizeAction(PermissionActionName.Cancel)]
        [HttpGet]
        public async Task<IActionResult> CancelOrder(string id)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = id });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }
            try
            {
                await _mediator.Send(new CancelOrderCommand() { Order = order, NotifyCustomer = true });
                await _orderViewModelService.LogEditOrder(order.Id);
                Success("Successfully canceled order");
                return RedirectToAction("Edit", "Order", new { id = id });
            }
            catch (Exception exc)
            {
                //error
                Error(exc, true);
                return RedirectToAction("Edit", "Order", new { id = id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> SaveOrderTags(OrderModel orderModel)
        {
            var order = await _orderService.GetOrderById(orderModel.Id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = order.Id });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            try
            {
                await _orderViewModelService.SaveOrderTags(order, orderModel.OrderTags);
                await _orderViewModelService.LogEditOrder(order.Id);
                var model = new OrderModel();
                await _orderViewModelService.PrepareOrderDetailsModel(model, order);
                return RedirectToAction("Edit", "Order", new { id = order.Id });
            }
            catch (Exception exception)
            {
                //error
                var model = new OrderModel();
                await _orderViewModelService.PrepareOrderDetailsModel(model, order);
                Error(exception, false);
                return RedirectToAction("Edit", "Order", new { id = order.Id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ChangeOrderStatus(string id, OrderModel model)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = id });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            try
            {
                var status = await _orderStatusService.GetByStatusId(model.OrderStatusId);
                if (status == null)
                    throw new ArgumentNullException(nameof(status));

                order.OrderStatusId = model.OrderStatusId;
                await _orderService.UpdateOrder(order);

                //add a note
                await _orderService.InsertOrderNote(new OrderNote
                {
                    Note = $"Order status has been edited. New status: {status?.Name}",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,

                });
                await _orderViewModelService.LogEditOrder(order.Id);
                model = new OrderModel();
                await _orderViewModelService.PrepareOrderDetailsModel(model, order);
                return RedirectToAction("Edit", "Order", new { id = id });
            }
            catch (Exception exc)
            {
                //error
                model = new OrderModel();
                await _orderViewModelService.PrepareOrderDetailsModel(model, order);
                Error(exc, false);
                return RedirectToAction("Edit", "Order", new { id = id });
            }
        }

        #endregion

        #region Edit, delete

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || order.Deleted || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToOrder(order) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            var model = new OrderModel();
            await _orderViewModelService.PrepareOrderDetailsModel(model, order);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> Delete(string id,
            [FromServices] ICustomerActivityService customerActivityService,
            [FromServices] IShipmentService shipmentService)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor or staff does not have access to this functionality
            if (_workContext.CurrentVendor != null || await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = id });

            var shipments = (await shipmentService.GetShipmentsByOrder(order.Id));
            if (shipments.Any())
                ModelState.AddModelError("", "This order is in associated with shipment. Please delete it first.");

            if (ModelState.IsValid)
            {
                await _mediator.Send(new DeleteOrderCommand() { Order = order });
                await customerActivityService.InsertActivity("DeleteOrder", id, _translationService.GetResource("ActivityLog.DeleteOrder"), order.Id);
                return RedirectToAction("List");
            }
            Error(ModelState);
            return RedirectToAction("Edit", "Order", new { id = id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> DeleteSelected(
            ICollection<string> selectedIds,
            [FromServices] ICustomerActivityService customerActivityService,
            [FromServices] IShipmentService shipmentService)
        {
            if (_workContext.CurrentVendor != null || await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List", "Order");

            if (selectedIds != null)
            {
                var orders = new List<Order>();
                orders.AddRange(await _orderService.GetOrdersByIds(selectedIds.ToArray()));
                for (var i = 0; i < orders.Count; i++)
                {
                    var order = orders[i];
                    var shipments = (await shipmentService.GetShipmentsByOrder(order.Id));
                    if (shipments.Any())
                        Error("Some orders is in associated with shipments. Please delete it first.");

                    if (await CheckSalesManager(order) && !shipments.Any())
                    {
                        await _mediator.Send(new DeleteOrderCommand() { Order = order });
                        await customerActivityService.InsertActivity("DeleteOrder", order.Id, _translationService.GetResource("ActivityLog.DeleteOrder"), order.Id);
                    }
                }
            }

            return Json(new { Result = true });
        }

        public async Task<IActionResult> PdfInvoice(string orderId)
        {
            var vendorId = string.Empty;

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                vendorId = _workContext.CurrentVendor.Id;
            }

            var order = await _orderService.GetOrderById(orderId);
            if ((await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId) || await CheckSalesManager(order))
            {
                return RedirectToAction("List");
            }

            var orders = new List<Order>
            {
                order
            };
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                await _pdfService.PrintOrdersToPdf(stream, orders, _workContext.WorkingLanguage.Id, vendorId);
                bytes = stream.ToArray();
            }
            return File(bytes, "application/pdf", string.Format("order_{0}.pdf", order.Id));
        }

        [PermissionAuthorizeAction(PermissionActionName.Export)]
        [HttpPost]
        public async Task<IActionResult> PdfInvoiceAll(OrderListModel model)
        {
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.VendorId = _workContext.CurrentVendor.Id;
            }
            //load orders
            var orders = await _orderViewModelService.PrepareOrders(model);
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                orders = orders.Where(x => x.StoreId == _workContext.CurrentCustomer.StaffStoreId).ToList();
            }

            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                await _pdfService.PrintOrdersToPdf(stream, orders, _workContext.WorkingLanguage.Id, model.VendorId);
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
            var vendorId = string.Empty;
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                orders = orders.Where(_workContext.HasAccessToOrder).ToList();
                vendorId = _workContext.CurrentVendor.Id;
            }
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                orders = orders.Where(x => x.StoreId == _workContext.CurrentCustomer.StaffStoreId).ToList();
            }

            //ensure that we at least one order selected
            if (orders.Count == 0)
            {
                Error(_translationService.GetResource("Admin.Orders.PdfInvoice.NoOrders"));
                return RedirectToAction("List");
            }

            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                await _pdfService.PrintOrdersToPdf(stream, orders, _workContext.WorkingLanguage.Id, vendorId);
                bytes = stream.ToArray();
            }
            return File(bytes, "application/pdf", "orders.pdf");
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> EditOrderTotals(string id, OrderModel model)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = id });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("Edit", "Order", new { id = id });
            }

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
            await _orderService.UpdateOrder(order);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = "Order totals have been edited",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });

            await _orderViewModelService.LogEditOrder(order.Id);
            await _orderViewModelService.PrepareOrderDetailsModel(model, order);
            return RedirectToAction("Edit", "Order", new { id = id });
        }
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> EditShippingMethod(string id, OrderModel model)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = id });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("Edit", "Order", new { id = id });
            }

            order.ShippingMethod = model.ShippingMethod;
            await _orderService.UpdateOrder(order);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = "Shipping method has been edited",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });
            await _orderViewModelService.LogEditOrder(order.Id);
            await _orderViewModelService.PrepareOrderDetailsModel(model, order);

            //selected tab
            await SaveSelectedTabIndex(persistForTheNextRequest: false);

            return RedirectToAction("Edit", "Order", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> EditUserFields(string id, OrderModel model)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = id });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("Edit", "Order", new { id = id });
            }

            order.UserFields = model.UserFields;

            await _orderService.UpdateOrder(order);
            await _orderViewModelService.LogEditOrder(order.Id);

            await _orderViewModelService.PrepareOrderDetailsModel(model, order);

            //selected tab
            await SaveSelectedTabIndex(persistForTheNextRequest: false);

            return RedirectToAction("Edit", "Order", new { id = id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> SaveOrderItem(string id, IFormCollection form)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = id });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("Edit", "Order", new { id = id });
            }
            if (order.OrderStatusId == (int)OrderStatusSystem.Cancelled)
            {
                Error("You can't edit position when order is canceled", true);
                return RedirectToAction("Edit", "Order", new { id = id });
            }

            //get order item identifier
            string orderItemId = "";
            foreach (var formValue in form.Keys)
                if (formValue.StartsWith("btnSaveOrderItem", StringComparison.OrdinalIgnoreCase))
                    orderItemId = formValue.Substring("btnSaveOrderItem".Length);

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
            if (orderItem == null)
                throw new ArgumentException("No order item found with the specified id");

            if (!double.TryParse(form["pvUnitPriceExclTax" + orderItemId], out double unitPriceExclTax))
                unitPriceExclTax = orderItem.UnitPriceExclTax;
            if (!int.TryParse(form["pvQuantity" + orderItemId], out int quantity))
                quantity = orderItem.Quantity;

            if (quantity == 0 || (orderItem.OpenQty != orderItem.Quantity && orderItem.IsShipEnabled))
            {
                Error("You can't change quantity", true);
                return RedirectToAction("Edit", "Order", new { id = id });
            }

            if (orderItem.Quantity == quantity && orderItem.UnitPriceExclTax == unitPriceExclTax)
            {
                Error("Nothing has been changed", true);
                return RedirectToAction("Edit", "Order", new { id = id });
            }

            orderItem.Quantity = quantity;
            orderItem.OpenQty = quantity;

            if (orderItem.UnitPriceExclTax != unitPriceExclTax)
            {
                orderItem.UnitPriceExclTax = unitPriceExclTax;
                orderItem.UnitPriceInclTax = Math.Round((orderItem.UnitPriceExclTax * orderItem.TaxRate / 100) + orderItem.UnitPriceExclTax, 2);
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

            await _mediator.Send(new UpdateOrderItemCommand() { Order = order, OrderItem = orderItem });

            //selected tab
            await SaveSelectedTabIndex(persistForTheNextRequest: true);

            return RedirectToAction("Edit", "Order", new { id = id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> DeleteOrderItem(string id, IFormCollection form)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = id });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("Edit", "Order", new { id = id });
            }
            //get order item identifier
            string orderItemId = "";
            foreach (var formValue in form.Keys)
                if (formValue.StartsWith("btnDeleteOrderItem", StringComparison.OrdinalIgnoreCase))
                    orderItemId = formValue.Substring("btnDeleteOrderItem".Length);

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
            if (orderItem == null)
                throw new ArgumentException("No order item found with the specified id");

            var result = await _mediator.Send(new DeleteOrderItemCommand() { Order = order, OrderItem = orderItem });
            if (result.error)
                Error(result.message, true);

            //selected tab
            await SaveSelectedTabIndex(persistForTheNextRequest: true);

            return RedirectToAction("Edit", "Order", new { id = id });

        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> CancelOrderItem(string id, IFormCollection form)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = id });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("Edit", "Order", new { id = id });
            }
            //get order item identifier
            string orderItemId = "";
            foreach (var formValue in form.Keys)
                if (formValue.StartsWith("btnCancelOrderItem", StringComparison.OrdinalIgnoreCase))
                    orderItemId = formValue.Substring("btnCancelOrderItem".Length);

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
            if (orderItem == null)
                throw new ArgumentException("No order item found with the specified id");


            var result = await _mediator.Send(new CancelOrderItemCommand() { Order = order, OrderItem = orderItem });
            if (result.error)
                Error(result.message, false);
            else
                Success("The order item was successfully canceled", true);

            //selected tab
            await SaveSelectedTabIndex(persistForTheNextRequest: true);

            return RedirectToAction("Edit", "Order", new { id = id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ResetDownloadCount(string id, IFormCollection form)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("Edit", "Order", new { id = id });
            }

            //get order item identifier
            string orderItemId = "";
            foreach (var formValue in form.Keys)
                if (formValue.StartsWith("btnResetDownloadCount", StringComparison.OrdinalIgnoreCase))
                    orderItemId = formValue.Substring("btnResetDownloadCount".Length);

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
            if (orderItem == null)
                throw new ArgumentException("No order item found with the specified id");

            //ensure a vendor has access only to his products 
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToOrderItem(orderItem) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");


            orderItem.DownloadCount = 0;
            await _orderService.UpdateOrder(order);
            await _orderViewModelService.LogEditOrder(order.Id);
            var model = new OrderModel();
            await _orderViewModelService.PrepareOrderDetailsModel(model, order);

            //selected tab
            await SaveSelectedTabIndex(persistForTheNextRequest: false);

            return RedirectToAction("Edit", "Order", new { id = id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> ActivateDownloadItem(string id, IFormCollection form)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("Edit", "Order", new { id = id });
            }

            //get order item identifier
            string orderItemId = "";
            foreach (var formValue in form.Keys)
                if (formValue.StartsWith("btnPvActivateDownload", StringComparison.OrdinalIgnoreCase))
                    orderItemId = formValue.Substring("btnPvActivateDownload".Length);

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
            if (orderItem == null)
                throw new ArgumentException("No order item found with the specified id");

            //ensure a vendor has access only to his products 
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToOrderItem(orderItem) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            orderItem.IsDownloadActivated = !orderItem.IsDownloadActivated;
            await _orderService.UpdateOrder(order);
            await _orderViewModelService.LogEditOrder(order.Id);
            var model = new OrderModel();
            await _orderViewModelService.PrepareOrderDetailsModel(model, order);

            //selected tab
            await SaveSelectedTabIndex(persistForTheNextRequest: false);

            return RedirectToAction("Edit", "Order", new { id = id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> UploadLicenseFilePopup(string id, string orderItemId, [FromServices] IProductService productService)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("Edit", "Order", new { id = id });
            }

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
            if (orderItem == null)
                throw new ArgumentException("No order item found with the specified id");

            var product = await productService.GetProductByIdIncludeArch(orderItem.ProductId);

            if (!product.IsDownload)
                throw new ArgumentException("Product is not downloadable");

            //ensure a vendor has access only to his products 
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToOrderItem(orderItem) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            var model = new OrderModel.UploadLicenseModel
            {
                LicenseDownloadId = !String.IsNullOrEmpty(orderItem.LicenseDownloadId) ? orderItem.LicenseDownloadId : "",
                OrderId = order.Id,
                OrderItemId = orderItem.Id
            };

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> UploadLicenseFilePopup(OrderModel.UploadLicenseModel model)
        {
            var order = await _orderService.GetOrderById(model.OrderId);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("Edit", "Order", new { id = order.Id });
            }

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId);
            if (orderItem == null)
                throw new ArgumentException("No order item found with the specified id");

            //ensure a vendor has access only to his products 
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToOrderItem(orderItem) && await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            //attach license
            if (!string.IsNullOrEmpty(model.LicenseDownloadId))
                orderItem.LicenseDownloadId = model.LicenseDownloadId;
            else
                orderItem.LicenseDownloadId = null;
            await _orderService.UpdateOrder(order);

            await _orderViewModelService.LogEditOrder(order.Id);
            //success
            ViewBag.RefreshPage = true;

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> DeleteLicenseFilePopup(OrderModel.UploadLicenseModel model)
        {
            var order = await _orderService.GetOrderById(model.OrderId);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("Edit", "Order", new { id = model.OrderId });
            }

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId);
            if (orderItem == null)
                throw new ArgumentException("No order item found with the specified id");

            //ensure a vendor has access only to his products 
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToOrderItem(orderItem) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            //attach license
            orderItem.LicenseDownloadId = null;
            await _orderService.UpdateOrder(order);
            await _orderViewModelService.LogEditOrder(order.Id);

            //success
            ViewBag.RefreshPage = true;

            return RedirectToAction("Edit", "Order", new { id = model.OrderId });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> AddProductToOrder(string orderId)
        {
            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = orderId });

            var order = await _orderService.GetOrderById(orderId);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            var model = await _orderViewModelService.PrepareAddOrderProductModel(order);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> AddProductToOrder(DataSourceRequest command, OrderModel.AddOrderProductModel model, [FromServices] IProductService productService)
        {
            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return Content("");

            var categoryIds = new List<string>();
            if (!string.IsNullOrEmpty(model.SearchCategoryId))
                categoryIds.Add(model.SearchCategoryId);

            var storeId = string.Empty;
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                storeId = _workContext.CurrentCustomer.StaffStoreId;
            }

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
                var productModel = new OrderModel.AddOrderProductModel.ProductModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Sku = x.Sku,
                };

                return productModel;
            });
            gridModel.Total = products.TotalCount;

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> AddProductToOrderDetails(string orderId, string productId)
        {
            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = orderId });

            var order = await _orderService.GetOrderById(orderId);
            if (order == null || await CheckSalesManager(order))
                return RedirectToAction("List");

            if (order == null)
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            var model = await _orderViewModelService.PrepareAddProductToOrderModel(order, productId);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> AddProductToOrderDetails(string orderId, string productId, IFormCollection form)
        {
            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = orderId });

            var order = await _orderService.GetOrderById(orderId);
            if (order == null || await CheckSalesManager(order))
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            var warnings = await _orderViewModelService.AddProductToOrderDetails(orderId, productId, form);
            if (!warnings.Any())
                //redirect to order details page
                return RedirectToAction("Edit", "Order", new { id = orderId });

            //errors
            var model = await _orderViewModelService.PrepareAddProductToOrderModel(order, productId);
            model.Warnings.AddRange(warnings);
            return View(model);
        }

        #endregion

        #endregion

        #region Addresses

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> AddressEdit(string addressId, string orderId, bool billingAddress)
        {
            var order = await _orderService.GetOrderById(orderId);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = orderId });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            var address = new Address();
            if (billingAddress && order.BillingAddress != null)
                if (order.BillingAddress.Id == addressId)
                    address = order.BillingAddress;
            if (!billingAddress && order.ShippingAddress != null)
                if (order.ShippingAddress.Id == addressId)
                    address = order.ShippingAddress;

            if (address == null)
                throw new ArgumentException("No address found with the specified id", "addressId");

            var model = await _orderViewModelService.PrepareOrderAddressModel(order, address);
            model.BillingAddress = billingAddress;
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> AddressEdit(OrderAddressModel model, IFormCollection form,
            [FromServices] IAddressAttributeService addressAttributeService,
            [FromServices] IAddressAttributeParser addressAttributeParser)
        {
            var order = await _orderService.GetOrderById(model.OrderId);
            if (order == null || await CheckSalesManager(order))
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("Edit", "Order", new { id = order.Id });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            var address = new Address();
            if (model.BillingAddress && order.BillingAddress != null)
                if (order.BillingAddress.Id == model.Address.Id)
                    address = order.BillingAddress;
            if (!model.BillingAddress && order.ShippingAddress != null)
                if (order.ShippingAddress.Id == model.Address.Id)
                    address = order.ShippingAddress;

            if (address == null)
                throw new ArgumentException("No address found with the specified id");

            //custom address attributes
            var customAttributes = await form.ParseCustomAddressAttributes(addressAttributeParser, addressAttributeService);
            var customAttributeWarnings = await addressAttributeParser.GetAttributeWarnings(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            if (ModelState.IsValid)
            {
                await _orderViewModelService.UpdateOrderAddress(order, address, model, customAttributes);
                return RedirectToAction("AddressEdit", new { addressId = model.Address.Id, orderId = model.OrderId, BillingAddress = model.BillingAddress });
            }

            //If we got this far, something failed, redisplay form
            model = await _orderViewModelService.PrepareOrderAddressModel(order, address);
            model.BillingAddress = model.BillingAddress;
            return View(model);
        }

        #endregion

        #region Order notes

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> OrderNotesSelect(string orderId, DataSourceRequest command)
        {
            var order = await _orderService.GetOrderById(orderId);
            if (order == null || await CheckSalesManager(order))
                throw new ArgumentException("No order found with the specified id");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return Content("");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return Content("");
            }
            //order notes
            var orderNoteModels = await _orderViewModelService.PrepareOrderNotes(order);
            var gridModel = new DataSourceResult
            {
                Data = orderNoteModels,
                Total = orderNoteModels.Count
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> OrderNoteAdd(string orderId, string downloadId, bool displayToCustomer, string message)
        {
            var order = await _orderService.GetOrderById(orderId);
            if (order == null || await CheckSalesManager(order))
                return Json(new { Result = false });

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return Json(new { Result = false });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return Json(new { Result = false });
            }
            await _orderViewModelService.InsertOrderNote(order, downloadId, displayToCustomer, message);

            return Json(new { Result = true });
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> OrderNoteDelete(string id, string orderId)
        {
            var order = await _orderService.GetOrderById(orderId);
            if (order == null || await CheckSalesManager(order))
                throw new ArgumentException("No order found with the specified id");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return Json(new { Result = false });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return Json(new { Result = false });
            }

            await _orderViewModelService.DeleteOrderNote(order, id);

            return new JsonResult("");
        }
        #endregion

    }
}
