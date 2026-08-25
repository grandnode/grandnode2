using Grand.Business.Core.Commands.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

[PermissionAuthorize(PermissionSystemName.Shipments)]
[AutoValidateAntiforgeryToken]
public abstract class BaseShipmentController(
    IShipmentViewModelService shipmentViewModelService,
    IOrderService orderService,
    ITranslationService translationService,
    IContextAccessor contextAccessor,
    IPdfService pdfService,
    IShipmentService shipmentService,
    IDateTimeService dateTimeService,
    IMediator mediator,
    IAdminDataScope<Shipment> scope,
    IAdminDataScope<Order> orderScope)
    : BaseController
{
    // Exposed for host-specific concrete subclasses (Admin's EditUserFields action needs these
    // same fields — primary-constructor parameters aren't visible to derived classes by name in
    // C#).
    protected IShipmentViewModelService ShipmentViewModelService => shipmentViewModelService;
    protected IOrderService OrderService => orderService;
    protected ITranslationService TranslationService => translationService;
    protected IContextAccessor ContextAccessor => contextAccessor;
    protected IPdfService PdfService => pdfService;
    protected IShipmentService ShipmentService => shipmentService;
    protected IDateTimeService DateTimeService => dateTimeService;
    protected IMediator Mediator => mediator;
    protected IAdminDataScope<Shipment> Scope => scope;
    protected IAdminDataScope<Order> OrderScope => orderScope;

    /// <summary>DRY replacement for the repeated "load shipment, redirect to List if not found or
    /// not authorized" pattern found in all 3 original controllers. Not a behavior change — every
    /// call site below still individually returns RedirectToAction("List") exactly as the
    /// originals did.</summary>
    protected async Task<(Shipment shipment, IActionResult denied)> LoadAuthorizedShipment(string id)
    {
        var shipment = await shipmentService.GetShipmentById(id);
        if (shipment == null) return (null, RedirectToAction("List"));
        if (!await scope.HasAccess(shipment)) return (null, RedirectToAction("List"));
        return (shipment, null);
    }

    #region Shipments

    public async Task<IActionResult> List()
    {
        var model = await shipmentViewModelService.PrepareShipmentListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> ShipmentListSelect(DataSourceRequest command, ShipmentListModel model)
    {
        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;
        if (scope.DefaultVendorId is not null) model.VendorId = scope.DefaultVendorId;

        var shipments = await shipmentViewModelService.PrepareShipments(model, command.Page, command.PageSize);
        var items = new List<ShipmentModel>();
        foreach (var item in shipments.shipments)
            items.Add(await shipmentViewModelService.PrepareShipmentModel(item, false));

        var gridModel = new DataSourceResult {
            Data = items,
            Total = shipments.totalCount
        };
        return Json(gridModel);
    }

    /// <summary>Filters per-shipment via scope.HasAccess rather than gating on the parent order.
    /// Admin: GlobalAdminDataScope.HasAccess is always true, so this is a no-op filter — matches
    /// Admin's original, which had no check at all. Store: every shipment under a given order
    /// always shares that order's StoreId (PrepareShipment always sets StoreId = order.StoreId,
    /// see Task 3 Step 5), so per-shipment filtering produces the same user-visible result as
    /// Store's original whole-order Content("") denial, with no possible mixed-store shipment set
    /// under one order. Vendor: this is the literal mechanical equivalent of Vendor's original
    /// per-shipment HasAccessToShipment loop.</summary>
    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> ShipmentsByOrder(string orderId, DataSourceRequest command)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || order.Deleted)
            throw new ArgumentException("No order found with the specified id");

        //shipments
        var shipmentModels = new List<ShipmentModel>();
        var shipments = (await shipmentService.GetShipmentsByOrder(orderId))
            .OrderBy(s => s.CreatedOnUtc)
            .ToList();
        var accessibleShipments = new List<Shipment>();
        foreach (var shipment in shipments)
            if (await scope.HasAccess(shipment))
                accessibleShipments.Add(shipment);

        foreach (var shipment in accessibleShipments)
            shipmentModels.Add(await shipmentViewModelService.PrepareShipmentModel(shipment, false));

        var gridModel = new DataSourceResult {
            Data = shipmentModels,
            Total = shipmentModels.Count
        };
        return Json(gridModel);
    }

    /// <summary>Deliberate, disclosed behavior change for Store only: Store's original returned a
    /// soft Content("") on a store mismatch; Admin/Vendor's originals both threw
    /// ArgumentException. Unified on the throwing form (2 of 3 hosts' original shape) rather than
    /// using LoadAuthorizedShipment (which redirects — wrong fit for a JSON-grid endpoint). Flag
    /// this explicitly in this task's commit message and for the final review.</summary>
    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> ShipmentsItemsByShipmentId(string shipmentId, DataSourceRequest command)
    {
        var shipment = await shipmentService.GetShipmentById(shipmentId) ?? throw new ArgumentException("No shipment found with the specified id");
        if (!await scope.HasAccess(shipment))
            throw new ArgumentException("No shipment found with the specified id");
        var order = await orderService.GetOrderById(shipment.OrderId) ?? throw new ArgumentException("No order found with the specified id");

        //shipments
        var shipmentModel = await shipmentViewModelService.PrepareShipmentModel(shipment, true);
        var gridModel = new DataSourceResult {
            Data = shipmentModel.Items,
            Total = shipmentModel.Items.Count
        };

        return Json(gridModel);
    }

    #endregion

    #region AddShipment

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> AddShipment(string orderId)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || order.Deleted || !await orderScope.HasAccess(order))
            //No order found with the specified id
            return RedirectToAction("List");

        var model = await shipmentViewModelService.PrepareShipmentModel(order);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> AddShipment(AddShipmentModel model, bool continueEditing)
    {
        var order = await orderService.GetOrderById(model.OrderId);
        if (order == null || order.Deleted || !await orderScope.HasAccess(order))
            //No order found with the specified id
            return RedirectToAction("List");

        var orderItems = scope.FilterOrderItems(order.OrderItems).ToList();

        var (shipment, totalWeight) = await shipmentViewModelService.PrepareShipment(order, orderItems, model);
        if (shipment == null || !shipment.ShipmentItems.Any())
        {
            Error(translationService.GetResource("Admin.Orders.Shipments.NoProductsSelected"));
            return RedirectToAction("AddShipment", new { orderId = model.OrderId });
        }

        //check stock
        var (valid, message) = await shipmentViewModelService.ValidStockShipment(shipment);
        if (!valid)
        {
            Error(message);
            return RedirectToAction("AddShipment", new { orderId = model.OrderId });
        }

        shipment.TotalWeight = totalWeight;
        await shipmentService.InsertShipment(shipment);

        //add a note
        await orderService.InsertOrderNote(new OrderNote {
            Note = $"A shipment #{shipment.ShipmentNumber} has been added",
            DisplayToCustomer = false,
            OrderId = order.Id
        });

        Success(translationService.GetResource("Admin.Orders.Shipments.Added"));
        return continueEditing
            ? RedirectToAction("ShipmentDetails", new { id = shipment.Id })
            : RedirectToAction("List", new { id = shipment.Id });
    }

    #endregion

    #region Shipment details

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> ShipmentDetails(string id)
    {
        var (shipment, denied) = await LoadAuthorizedShipment(id);
        if (denied != null) return denied;

        var order = await orderService.GetOrderById(shipment.OrderId);
        if (order == null)
            //No order found with the specified id
            return RedirectToAction("List");

        var model = await shipmentViewModelService.PrepareShipmentModel(shipment, true, true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteShipment(string id)
    {
        var (shipment, denied) = await LoadAuthorizedShipment(id);
        if (denied != null) return denied;

        var order = await orderService.GetOrderById(shipment.OrderId);
        if (order == null)
            //No order found with the specified id
            return RedirectToAction("List");

        await shipmentService.DeleteShipment(shipment);

        //add a note
        await orderService.InsertOrderNote(new OrderNote {
            Note = $"A shipment #{shipment.ShipmentNumber} has been deleted",
            DisplayToCustomer = false,
            OrderId = order.Id
        });

        Success(translationService.GetResource("Admin.Orders.Shipments.Deleted"));

        return RedirectToAction("Edit", "Order", new { order.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SetTrackingNumber(ShipmentTrackingModel model)
    {
        var (shipment, denied) = await LoadAuthorizedShipment(model.Id);
        if (denied != null) return denied;

        shipment.TrackingNumber = model.TrackingNumber;
        await shipmentService.UpdateShipment(shipment);

        return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SetShipmentAdminComment(ShipmentAdminCommentModel model)
    {
        var (shipment, denied) = await LoadAuthorizedShipment(model.Id);
        if (denied != null) return denied;

        shipment.AdminComment = model.AdminComment;
        await shipmentService.UpdateShipment(shipment);

        return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SetAsShipped(string id)
    {
        var (shipment, denied) = await LoadAuthorizedShipment(id);
        if (denied != null) return denied;

        try
        {
            await mediator.Send(new ShipCommand { Shipment = shipment, NotifyCustomer = true });
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> EditShippedDate(ShipmentShippedDateModel model)
    {
        var (shipment, denied) = await LoadAuthorizedShipment(model.Id);
        if (denied != null) return denied;

        try
        {
            if (!model.ShippedDate.HasValue) throw new Exception("Enter shipped date");

            shipment.ShippedDateUtc = model.ShippedDate.ConvertToUtcTime(dateTimeService);
            await shipmentService.UpdateShipment(shipment);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SetAsDelivered(string id)
    {
        var (shipment, denied) = await LoadAuthorizedShipment(id);
        if (denied != null) return denied;

        try
        {
            await mediator.Send(new DeliveryCommand { Shipment = shipment, NotifyCustomer = true });
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> EditDeliveryDate(ShipmentDeliveryDateModel model)
    {
        var (shipment, denied) = await LoadAuthorizedShipment(model.Id);
        if (denied != null) return denied;

        try
        {
            if (!model.DeliveryDate.HasValue) throw new Exception("Enter delivery date");

            shipment.DeliveryDateUtc = model.DeliveryDate.ConvertToUtcTime(dateTimeService);
            await shipmentService.UpdateShipment(shipment);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }

    #endregion

    #region PDF export and bulk actions

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> PdfPackagingSlip(string shipmentId)
    {
        var (shipment, denied) = await LoadAuthorizedShipment(shipmentId);
        if (denied != null) return denied;

        var order = await orderService.GetOrderById(shipment.OrderId);
        if (order == null)
            //No order found with the specified id
            return RedirectToAction("List");

        var shipments = new List<Shipment> { shipment };

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintPackagingSlipsToPdf(stream, shipments, contextAccessor.WorkContext.WorkingLanguage.Id);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", $"packagingslip_{shipment.Id}.pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> PdfPackagingSlipAll(ShipmentListModel model)
    {
        if (scope.DefaultStoreId is not null) model.StoreId = scope.DefaultStoreId;
        if (scope.DefaultVendorId is not null) model.VendorId = scope.DefaultVendorId;

        //load shipments
        var shipments = await shipmentViewModelService.PrepareShipments(model, 1, 100);

        //ensure that we at least one shipment selected
        if (shipments.totalCount == 0)
        {
            Error(translationService.GetResource("Admin.Orders.Shipments.NoShipmentsSelected"));
            return RedirectToAction("List");
        }

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintPackagingSlipsToPdf(stream, shipments.shipments.ToList(),
                contextAccessor.WorkContext.WorkingLanguage.Id);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", "packagingslips.pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> PdfPackagingSlipSelected(string selectedIds)
    {
        var shipments = new List<Shipment>();
        if (selectedIds != null)
        {
            var ids = selectedIds
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x)
                .ToArray();
            shipments.AddRange(await shipmentService.GetShipmentsByIds(ids));
        }

        var accessibleShipments = new List<Shipment>();
        foreach (var s in shipments)
            if (await scope.HasAccess(s))
                accessibleShipments.Add(s);

        //ensure that we at least one shipment selected — checks the unfiltered count, matching
        //all 3 originals' pre-existing (and slightly inconsistent) behavior; see the disclosed,
        //not-fixed note below this method
        if (shipments.Count == 0)
        {
            Error(translationService.GetResource("Admin.Orders.Shipments.NoShipmentsSelected"));
            return RedirectToAction("List");
        }

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintPackagingSlipsToPdf(stream, accessibleShipments, contextAccessor.WorkContext.WorkingLanguage.Id);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", "packagingslips.pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SetAsShippedSelected(ICollection<string> selectedIds)
    {
        var shipments = new List<Shipment>();
        if (selectedIds != null) shipments.AddRange(await shipmentService.GetShipmentsByIds(selectedIds.ToArray()));

        var accessibleShipments = new List<Shipment>();
        foreach (var s in shipments)
            if (await scope.HasAccess(s))
                accessibleShipments.Add(s);

        foreach (var shipment in accessibleShipments)
            try
            {
                await mediator.Send(new ShipCommand { Shipment = shipment, NotifyCustomer = true });
            }
            catch
            {
                //ignore any exception
            }

        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SetAsDeliveredSelected(ICollection<string> selectedIds)
    {
        var shipments = new List<Shipment>();
        if (selectedIds != null) shipments.AddRange(await shipmentService.GetShipmentsByIds(selectedIds.ToArray()));

        var accessibleShipments = new List<Shipment>();
        foreach (var s in shipments)
            if (await scope.HasAccess(s))
                accessibleShipments.Add(s);

        foreach (var shipment in accessibleShipments)
            try
            {
                await mediator.Send(new DeliveryCommand { Shipment = shipment, NotifyCustomer = true });
            }
            catch
            {
                //ignore any exception
            }

        return Json(new { Result = true });
    }

    #endregion
}
