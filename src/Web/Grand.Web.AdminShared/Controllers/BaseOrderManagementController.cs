using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Extensions;
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
        await orderService.UpdateOrder(order);

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
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        order.ShippingMethod = model.ShippingMethod;
        await orderService.UpdateOrder(order);

        await orderService.InsertOrderNote(new OrderNote {
            Note = "Shipping method has been edited",
            DisplayToCustomer = false,
            OrderId = order.Id
        });
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        await SaveSelectedTabIndex(persistForTheNextRequest: true);
        return RedirectToAction("Edit", "Order", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> EditUserFields(string id, OrderModel model)
    {
        var (order, denied) = await LoadAuthorizedOrder(id);
        if (denied != null) return denied;

        order.UserFields = model.UserFields;
        await orderService.UpdateOrder(order);
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

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
        await SaveSelectedTabIndex(persistForTheNextRequest: true);
        return RedirectToAction("Edit", "Order", new { id });
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
        await orderService.UpdateOrder(order);
        var model = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

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
        await orderService.UpdateOrder(order);
        var model = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        await SaveSelectedTabIndex(persistForTheNextRequest: true);
        return RedirectToAction("Edit", "Order", new { id });
    }

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
        await orderService.UpdateOrder(order);

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
        await orderService.UpdateOrder(order);

        return RedirectToAction("Edit", "Order", new { id = model.OrderId });
    }

    #endregion

    #region Add product to order

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddProductToOrder(string orderId)
    {
        var (order, denied) = await LoadAuthorizedOrder(orderId);
        if (denied != null) return denied;

        var model = await orderViewModelService.PrepareAddOrderProductModel(order);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddProductToOrder(
        Grand.Web.Common.DataSource.DataSourceRequest command, OrderModel.AddOrderProductModel model,
        [FromServices] Grand.Business.Core.Interfaces.Catalog.Products.IProductService productService)
    {
        var categoryIds = new List<string>();
        if (!string.IsNullOrEmpty(model.SearchCategoryId)) categoryIds.Add(model.SearchCategoryId);

        var gridModel = new Grand.Web.Common.DataSource.DataSourceResult();
        var products = (await productService.SearchProducts(categoryIds: categoryIds,
            storeId: scope.DefaultStoreId,
            brandId: model.SearchBrandId,
            collectionId: model.SearchCollectionId,
            productType: model.SearchProductTypeId > 0 ? (Grand.Domain.Catalog.ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize,
            showHidden: true)).products;

        gridModel.Data = products.Select(x => new OrderModel.AddOrderProductModel.ProductModel {
            Id = x.Id, Name = x.Name, Sku = x.Sku
        });
        gridModel.Total = products.TotalCount;

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddProductToOrderDetails(string orderId, string productId)
    {
        var (order, denied) = await LoadAuthorizedOrder(orderId);
        if (denied != null) return denied;

        var model = await orderViewModelService.PrepareAddProductToOrderModel(order, productId);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddProductToOrderDetails(AddProductToOrderModel model)
    {
        var (order, denied) = await LoadAuthorizedOrder(model.OrderId);
        if (denied != null) return denied;

        var warnings = await orderViewModelService.AddProductToOrderDetails(model);
        if (!warnings.Any()) return RedirectToAction("Edit", "Order", new { id = model.OrderId });

        var result = await orderViewModelService.PrepareAddProductToOrderModel(order, model.ProductId);
        result.Warnings.AddRange(warnings);
        return View(result);
    }

    #endregion

    #region Addresses

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> AddressEdit(string addressId, string orderId, bool billingAddress)
    {
        var (order, denied) = await LoadAuthorizedOrder(orderId);
        if (denied != null) return denied;

        var address = new Grand.Domain.Common.Address();
        switch (billingAddress)
        {
            case true when order.BillingAddress != null:
                if (order.BillingAddress.Id == addressId) address = order.BillingAddress;
                break;
            case false when order.ShippingAddress != null:
                if (order.ShippingAddress.Id == addressId) address = order.ShippingAddress;
                break;
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
        [FromServices] Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeService addressAttributeService,
        [FromServices] Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeParser addressAttributeParser)
    {
        var (order, denied) = await LoadAuthorizedOrder(model.OrderId);
        if (denied != null) return denied;

        var address = new Grand.Domain.Common.Address();
        switch (model.BillingAddress)
        {
            case true when order.BillingAddress != null:
                if (order.BillingAddress.Id == model.Address.Id) address = order.BillingAddress;
                break;
            case false when order.ShippingAddress != null:
                if (order.ShippingAddress.Id == model.Address.Id) address = order.ShippingAddress;
                break;
        }

        if (ModelState.IsValid)
        {
            var customAttributes = await model.Address.ParseCustomAddressAttributes(addressAttributeParser, addressAttributeService);
            await orderViewModelService.UpdateOrderAddress(order, address, model, customAttributes);
            return RedirectToAction("AddressEdit",
                new { addressId = model.Address.Id, orderId = model.OrderId, model.BillingAddress });
        }

        model = await orderViewModelService.PrepareOrderAddressModel(order, address);
        return View(model);
    }

    #endregion
}
