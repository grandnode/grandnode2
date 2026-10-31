using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseOrderManagementController(
    IOrderViewModelService orderViewModelService,
    IOrderService orderService,
    IOrderStatusService orderStatusService,
    IRequestTranslationService translationService,
    IContextAccessor contextAccessor,
    IPdfService pdfService,
    IMediator mediator,
    IAdminDataScope<Order> scope,
    IOrderNoteViewModelService orderNoteViewModelService,
    IOrderAddressViewModelService orderAddressViewModelService,
    IOrderProductViewModelService orderProductViewModelService)
    : BaseOrderController(orderViewModelService, orderService, translationService, contextAccessor,
        pdfService, scope)
{
    // Exposed for Grand.Web.Admin's concrete OrderController subclass (Task 17), which calls
    // Mediator.Send(...) directly - primary-constructor parameters aren't visible to derived
    // classes by name in C#.
    protected IMediator Mediator => mediator;

    #region Payments and other order workflow

    // POST: the order page submits its form here (the button is an <input type="submit">), and a
    // state-changing GET would let any link or prefetch cancel an order.
    [PermissionAuthorizeAction(PermissionActionName.Cancel)]
    [HttpPost]
    public async Task<IActionResult> CancelOrder(string id)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        if (!await mediator.Send(new CanCancelOrderQuery { Order = order }))
        {
            Error(TranslationService.GetResource("Admin.Orders.CannotCancel"));
            return RedirectToAction("Edit", "Order", new { id });
        }

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
    public async Task<IActionResult> DecideApprovalPopup(string id)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        var model = await OrderViewModelService.PrepareOrderApprovalDecisionModel(order);
        if (model == null)
            return RedirectToAction("Edit", "Order", new { id });

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DecideApproval(string id, bool? approve, string comment)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        //the decision must be explicit: a missing value is never read as a rejection or an approval
        var decided = approve.HasValue && await mediator.Send(new DecideStoreOrderApprovalCommand {
            Order = order,
            DecidedBy = ContextAccessor.WorkContext.CurrentCustomer,
            Approve = approve.Value,
            Comment = comment,
            Language = ContextAccessor.WorkContext.WorkingLanguage
        });

        if (decided)
            Success(TranslationService.GetResource(approve.Value
                ? "Admin.Orders.Approval.Approved"
                : "Admin.Orders.Approval.Rejected"));
        else
            Error(TranslationService.GetResource("Admin.Orders.Approval.Refused"));

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SaveOrderTags(OrderModel orderModel)
    {
        var (order, denied) = await LoadAuthorizedOrder(orderModel.Id);
        if (denied != null) return denied;

        try
        {
            await OrderViewModelService.SaveOrderTags(order, orderModel.OrderTags);
            var model = new OrderModel();
            await OrderViewModelService.PrepareOrderDetailsModel(model, order);
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

        //an order leaves Awaiting approval through the approver's decision, the buyer's withdrawal or a cancel,
        //which also close its approval request; only the checkout puts an order into that status
        if (order.OrderStatusId == (int)OrderStatusSystem.AwaitingApproval)
        {
            Error(TranslationService.GetResource("Admin.Orders.Fields.OrderStatus.Change.AwaitingApproval"));
            return RedirectToAction("Edit", "Order", new { id });
        }

        if (model.OrderStatusId == (int)OrderStatusSystem.AwaitingApproval)
        {
            Error(TranslationService.GetResource("Admin.Orders.Fields.OrderStatus.Change.ToAwaitingApproval"));
            return RedirectToAction("Edit", "Order", new { id });
        }

        try
        {
            var status = await orderStatusService.GetByStatusId(model.OrderStatusId);
            ArgumentNullException.ThrowIfNull(status);

            order.OrderStatusId = model.OrderStatusId;
            await OrderService.UpdateOrder(order);

            await OrderService.InsertOrderNote(new OrderNote {
                Note = $"Order status has been edited. New status: {status.Name}",
                DisplayToCustomer = false,
                OrderId = order.Id
            });
            model = new OrderModel();
            await OrderViewModelService.PrepareOrderDetailsModel(model, order);
            return RedirectToAction("Edit", "Order", new { id });
        }
        catch (Exception exc)
        {
            Error(exc, false);
            return RedirectToAction("Edit", "Order", new { id });
        }
    }

    #endregion

    #region Order totals / shipping / user fields

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> EditOrderTotals(string id, OrderModel model)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

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
        await OrderService.UpdateOrder(order);

        await OrderService.InsertOrderNote(new OrderNote {
            Note = "Order totals have been edited",
            DisplayToCustomer = false,
            OrderId = order.Id
        });

        await OrderViewModelService.PrepareOrderDetailsModel(model, order);
        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> EditShippingMethod(string id, OrderModel model)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        order.ShippingMethod = model.ShippingMethod;
        await OrderService.UpdateOrder(order);

        await OrderService.InsertOrderNote(new OrderNote {
            Note = "Shipping method has been edited",
            DisplayToCustomer = false,
            OrderId = order.Id
        });
        await OrderViewModelService.PrepareOrderDetailsModel(model, order);

        await SaveSelectedTabIndex(persistForTheNextRequest: true);
        return RedirectToAction("Edit", "Order", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> EditUserFields(string id, OrderModel model)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        order.UserFields = model.UserFields;
        await OrderService.UpdateOrder(order);
        await OrderViewModelService.PrepareOrderDetailsModel(model, order);

        await SaveSelectedTabIndex(persistForTheNextRequest: true);
        return RedirectToAction("Edit", "Order", new { id });
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

    #region Order items

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SaveOrderItem(string id, OrderItemsModel model)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        if (order.OrderStatusId == (int)OrderStatusSystem.Cancelled)
        {
            Error("You can't edit position when order is canceled");
            return RedirectToAction("Edit", "Order", new { id });
        }

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId)
            ?? throw new ArgumentException("No order item found with the specified id");
        var itemModel = model.Items.FirstOrDefault(x => x.Id == model.OrderItemId)
            ?? throw new ArgumentException("No order item model found with the specified id");

        var (error, message) = await ApplyOrderItemEdit(order, orderItem, itemModel.Quantity,
            itemModel.UnitPriceExclTaxValue);
        if (error)
        {
            Error(message);
            return RedirectToAction("Edit", "Order", new { id });
        }

        await SaveSelectedTabIndex(persistForTheNextRequest: true);
        return RedirectToAction("Edit", "Order", new { id });
    }

    /// <summary>
    ///     The quantity/unit price rules and the amount arithmetic of an order item edit, shared by
    ///     the form post (<see cref="SaveOrderItem" />) and the products grid
    ///     (<see cref="OrderItemUpdate" />). Every amount is still computed here on the server.
    /// </summary>
    private async Task<(bool error, string message)> ApplyOrderItemEdit(Order order, OrderItem orderItem,
        int quantity, double unitPriceExclTax)
    {
        if (quantity == 0 || (orderItem.OpenQty != orderItem.Quantity && orderItem.IsShipEnabled))
            return (true, "You can't change quantity");

        if (orderItem.Quantity == quantity && orderItem.UnitPriceExclTax == unitPriceExclTax)
            return (true, "Nothing has been changed");

        orderItem.Quantity = quantity;
        orderItem.OpenQty = quantity;

        if (orderItem.UnitPriceExclTax != unitPriceExclTax)
        {
            orderItem.UnitPriceExclTax = unitPriceExclTax;
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
        return (false, string.Empty);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeleteOrderItem(string id, string orderItemId)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId)
            ?? throw new ArgumentException("No order item found with the specified id");
        var result = await mediator.Send(new DeleteOrderItemCommand { Order = order, OrderItem = orderItem });
        if (result.error) Error(result.message);

        await SaveSelectedTabIndex(persistForTheNextRequest: true);
        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CancelOrderItem(string id, string orderItemId)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId)
            ?? throw new ArgumentException("No order item found with the specified id");
        var result = await mediator.Send(new CancelOrderItemCommand { Order = order, OrderItem = orderItem });
        if (result.error) Error(result.message);
        else Success("The order item was successfully canceled");

        await SaveSelectedTabIndex(persistForTheNextRequest: true);
        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ResetDownloadCount(string id, string orderItemId)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId)
            ?? throw new ArgumentException("No order item found with the specified id");
        orderItem.DownloadCount = 0;
        await OrderService.UpdateOrder(order);
        var model = new OrderModel();
        await OrderViewModelService.PrepareOrderDetailsModel(model, order);

        await SaveSelectedTabIndex(persistForTheNextRequest: true);
        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ActivateDownloadItem(string id, string orderItemId)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId)
            ?? throw new ArgumentException("No order item found with the specified id");
        orderItem.IsDownloadActivated = !orderItem.IsDownloadActivated;
        await OrderService.UpdateOrder(order);
        var model = new OrderModel();
        await OrderViewModelService.PrepareOrderDetailsModel(model, order);

        await SaveSelectedTabIndex(persistForTheNextRequest: true);
        return RedirectToAction("Edit", "Order", new { id });
    }

    #endregion

    #region Order items grid

    /// <summary>
    ///     Inline edit of one row of the products grid. Enforces exactly the rules
    ///     <see cref="SaveOrderItem" /> enforces; the answer is the grid's
    ///     <see cref="Grand.Web.Common.DataSource.DataSourceResult" /> contract, so a refused edit is
    ///     shown by display_kendoui_grid_error instead of a redirect.
    /// </summary>
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> OrderItemUpdate(string orderId, OrderModel.OrderItemModel model)
    {
        var (order, denied) = await LoadAuthorizedOrderForGrid(orderId);
        if (denied != null) return denied;

        if (order.OrderStatusId == (int)OrderStatusSystem.Cancelled)
            return GridError("You can't edit position when order is canceled");

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.Id);
        if (orderItem == null) return GridError("No order item found with the specified id");

        var (error, message) = await ApplyOrderItemEdit(order, orderItem, model.Quantity, model.UnitPriceExclTaxValue);
        return error ? GridError(message) : Json(new Grand.Web.Common.DataSource.DataSourceResult());
    }

    /// <summary>Delete command of the products grid; the same command <see cref="DeleteOrderItem" /> sends.</summary>
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> OrderItemDelete(string orderId, OrderModel.OrderItemModel model)
    {
        var (order, denied) = await LoadAuthorizedOrderForGrid(orderId);
        if (denied != null) return denied;

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.Id);
        if (orderItem == null) return GridError("No order item found with the specified id");

        var result = await mediator.Send(new DeleteOrderItemCommand { Order = order, OrderItem = orderItem });
        return result.error ? GridError(result.message) : Json(new Grand.Web.Common.DataSource.DataSourceResult());
    }

    /// <summary>Cancel command of the products grid; the same command <see cref="CancelOrderItem" /> sends.</summary>
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> OrderItemCancel(string orderId, string orderItemId)
    {
        var (order, denied) = await LoadAuthorizedOrderForGrid(orderId);
        if (denied != null) return denied;

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        if (orderItem == null) return GridError("No order item found with the specified id");

        var result = await mediator.Send(new CancelOrderItemCommand { Order = order, OrderItem = orderItem });
        return result.error ? GridError(result.message) : Json(new Grand.Web.Common.DataSource.DataSourceResult());
    }

    /// <summary>
    ///     <see cref="LoadAuthorizedOrder" /> for the grid endpoints: a denial is an Errors answer the
    ///     grid can show, never a redirect an XHR would follow into an HTML page.
    /// </summary>
    private async Task<(Order order, IActionResult denied)> LoadAuthorizedOrderForGrid(string orderId)
    {
        var order = await OrderService.GetOrderById(orderId);
        if (order == null || !await Scope.HasAccess(order)) return (null, GridError("Access denied"));
        return (order, null);
    }

    private JsonResult GridError(string message) =>
        Json(new Grand.Web.Common.DataSource.DataSourceResult { Errors = message });

    #endregion

    #region License popup

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UploadLicenseFilePopup(string id, string orderItemId,
        [FromServices] Grand.Business.Core.Interfaces.Catalog.Products.IProductService productService)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId)
            ?? throw new ArgumentException("No order item found with the specified id");
        var product = await productService.GetProductByIdIncludeArch(orderItem.ProductId);
        if (!product.IsDownload) throw new ArgumentException("Product is not downloadable");

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
        var (order, denied) = await LoadAuthorizedOrder(model.OrderId);
        if (denied != null) return denied;

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId)
            ?? throw new ArgumentException("No order item found with the specified id");
        orderItem.LicenseDownloadId = !string.IsNullOrEmpty(model.LicenseDownloadId) ? model.LicenseDownloadId : null;
        await OrderService.UpdateOrder(order);

        model.RefreshPage = true;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeleteLicenseFilePopup(OrderModel.UploadLicenseModel model)
    {
        var (order, denied) = await LoadAuthorizedOrder(model.OrderId);
        if (denied != null) return denied;

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId)
            ?? throw new ArgumentException("No order item found with the specified id");
        orderItem.LicenseDownloadId = null;
        await OrderService.UpdateOrder(order);

        return RedirectToAction("Edit", "Order", new { id = model.OrderId });
    }

    #endregion

    #region Add product to order

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddProductToOrder(string orderId)
    {
        var (order, denied) = await LoadAuthorizedOrder(orderId);
        if (denied != null) return denied;

        var model = await orderProductViewModelService.PrepareAddOrderProductModel(order);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddProductToOrder(
        Grand.Web.Common.DataSource.DataSourceRequest command, OrderModel.AddOrderProductModel model,
        [FromServices] Grand.Business.Core.Interfaces.Catalog.Products.IProductService productService)
    {
        var (order, denied) = await LoadAuthorizedOrderForGrid(model.OrderId);
        if (denied != null) return denied;

        var categoryIds = new List<string>();
        if (!string.IsNullOrEmpty(model.SearchCategoryId)) categoryIds.Add(model.SearchCategoryId);

        //only what the order's store sells, the same rule AddProductToOrderDetails applies
        var gridModel = new Grand.Web.Common.DataSource.DataSourceResult();
        var products = (await productService.SearchProducts(
            access: CatalogAccess.IncludingHidden(order.StoreId), categoryIds: categoryIds,
            brandId: model.SearchBrandId,
            collectionId: model.SearchCollectionId,
            productType: model.SearchProductTypeId > 0 ? (Grand.Domain.Catalog.ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize)).products;

        gridModel.Data = products.Select(x => new OrderModel.AddOrderProductModel.ProductModel {
            Id = x.Id, Name = x.Name, Sku = x.Sku
        });
        gridModel.Total = products.TotalCount;

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddProductToOrderDetails(string orderId, string productId,
        [FromServices] IProductService productService, [FromServices] IAclService aclService)
    {
        var (order, denied) = await LoadAuthorizedOrder(orderId);
        if (denied != null) return denied;
        var product = await LoadProductForOrder(order, productId, productService, aclService);
        if (product == null) return ProductNotInOrderStore(orderId);

        var model = await orderProductViewModelService.PrepareAddProductToOrderModel(order, product);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddProductToOrderDetails(AddProductToOrderModel model,
        [FromServices] IProductService productService, [FromServices] IAclService aclService)
    {
        var (order, denied) = await LoadAuthorizedOrder(model.OrderId);
        if (denied != null) return denied;
        var product = await LoadProductForOrder(order, model.ProductId, productService, aclService);
        if (product == null) return ProductNotInOrderStore(model.OrderId);

        var warnings = await orderProductViewModelService.AddProductToOrderDetails(order, product, model);
        if (!warnings.Any()) return RedirectToAction("Edit", "Order", new { id = model.OrderId });

        var result = await orderProductViewModelService.PrepareAddProductToOrderModel(order, product);
        result.Warnings.AddRange(warnings);
        return View(result);
    }

    // A product id put into the URL by hand must not bring in a product the order's store doesn't sell.
    private static async Task<Product> LoadProductForOrder(Order order, string productId,
        IProductService productService, IAclService aclService)
    {
        var product = await productService.GetProductById(productId);
        return product != null && aclService.Authorize(product, order.StoreId) ? product : null;
    }

    private IActionResult ProductNotInOrderStore(string orderId)
    {
        Error(TranslationService.GetResource("Admin.Orders.Products.AddNew.NotInOrderStore"));
        return RedirectToAction("AddProductToOrder", new { orderId });
    }

    #endregion

    #region Addresses

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> AddressEdit(string addressId, string orderId, bool billingAddress)
    {
        var (order, denied) = await LoadAuthorizedOrder(orderId);
        if (denied != null) return denied;

        var address = billingAddress ? order.BillingAddress : order.ShippingAddress;
        if (address == null) return RedirectToAction("Edit", "Order", new { id = orderId });

        var model = await orderAddressViewModelService.PrepareOrderAddressModel(order, address);
        model.BillingAddress = billingAddress;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressEdit(OrderAddressModel model,
        [FromServices] Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeService addressAttributeService,
        [FromServices] Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeParser addressAttributeParser)
    {
        var (order, denied) = await LoadAuthorizedOrder(model.OrderId);
        if (denied != null) return denied;

        var address = model.BillingAddress ? order.BillingAddress : order.ShippingAddress;
        if (address == null) return RedirectToAction("Edit", "Order", new { id = model.OrderId });

        var customAttributes = await model.Address.ParseCustomAddressAttributes(addressAttributeParser, addressAttributeService);
        if (ModelState.IsValid)
        {
            await orderAddressViewModelService.UpdateOrderAddress(order, address, model, customAttributes);
            return RedirectToAction("AddressEdit",
                new { addressId = address.Id, orderId = model.OrderId, model.BillingAddress });
        }

        //show the form again with what was typed, not with the saved address
        var typed = model.Address.ToEntity();
        typed.Id = address.Id;
        typed.Attributes = customAttributes;
        var billingAddress = model.BillingAddress;
        model = await orderAddressViewModelService.PrepareOrderAddressModel(order, typed);
        model.BillingAddress = billingAddress;
        return View(model);
    }

    #endregion

    #region Order notes

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> OrderNotesSelect(string orderId, Grand.Web.Common.DataSource.DataSourceRequest command)
    {
        var order = await OrderService.GetOrderById(orderId)
            ?? throw new ArgumentException("No order found with the specified id");
        // Preserved host divergence: Admin's original throws for both not-found and Sales-Manager
        // denial; Store's original throws only for not-found and soft-denies (empty content) for
        // store-mismatch. Unifying these into one behavior would be a real change for one host -
        // deliberately not done here. See plan's Global Constraints.
        if (!await Scope.HasAccess(order)) return Content("");

        var orderNoteModels = await orderNoteViewModelService.PrepareOrderNotes(order);
        var gridModel = new Common.DataSource.DataSourceResult {
            Data = orderNoteModels,
            Total = orderNoteModels.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> OrderNoteAdd(string orderId, string downloadId, bool displayToCustomer, string message)
    {
        var order = await OrderService.GetOrderById(orderId);
        if (order == null || !await Scope.HasAccess(order)) return Json(new { Result = false });

        if (!await orderNoteViewModelService.InsertOrderNote(order, downloadId, displayToCustomer, message))
            return Json(new {
                Result = false,
                Message = TranslationService.GetResource("Admin.Orders.OrderNotes.DownloadNotOfOrder")
            });

        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> OrderNoteDelete(string id, string orderId)
    {
        var order = await OrderService.GetOrderById(orderId)
            ?? throw new ArgumentException("No order found with the specified id");
        if (!await Scope.HasAccess(order)) return Json(new { Result = false });

        await orderNoteViewModelService.DeleteOrderNote(order, id);
        return new JsonResult("");
    }

    #endregion
}
