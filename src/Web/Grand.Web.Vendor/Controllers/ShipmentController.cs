using Grand.Business.Core.Commands.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Vendor.Extensions;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Models.Shipment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

[PermissionAuthorize(PermissionSystemName.Shipments)]
public class ShipmentController : BaseVendorController
{
    public ShipmentController(
        IShipmentViewModelService shipmentViewModelService,
        IOrderService orderService,
        ITranslationService translationService,
        IWorkContext workContext,
        IPdfService pdfService,
        IShipmentService shipmentService,
        IDateTimeService dateTimeService,
        IMediator mediator)
    {
        _shipmentViewModelService = shipmentViewModelService;
        _orderService = orderService;
        _translationService = translationService;
        _workContext = workContext;
        _pdfService = pdfService;
        _shipmentService = shipmentService;
        _dateTimeService = dateTimeService;
        _mediator = mediator;
    }

    #region Fields

    private readonly IShipmentViewModelService _shipmentViewModelService;
    private readonly IOrderService _orderService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;
    private readonly IPdfService _pdfService;
    private readonly IShipmentService _shipmentService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IMediator _mediator;

    #endregion

    #region Shipments

    public async Task<IActionResult> List()
    {
        var model = await _shipmentViewModelService.PrepareShipmentListModel();
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> ShipmentListSelect(DataSourceRequest command, ShipmentListModel model)
    {
        var shipments = await _shipmentViewModelService.PrepareShipments(model, command.Page, command.PageSize);
        var items = new List<ShipmentModel>();
        foreach (var item in shipments.shipments)
            items.Add(await _shipmentViewModelService.PrepareShipmentModel(item, false));

        var gridModel = new DataSourceResult {
            Data = items,
            Total = shipments.totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> ShipmentsByOrder(string orderId)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (order == null || order.Deleted || !_workContext.HasAccessToOrder(order))
            throw new ArgumentException("No order found with the specified id");

        //shipments
        var shipmentModels = new List<ShipmentModel>();
        var shipments = (await _shipmentService.GetShipmentsByOrder(orderId))
            //a vendor should have access only to his products
            .Where(s => _workContext.HasAccessToShipment(s))
            .OrderBy(s => s.CreatedOnUtc)
            .ToList();

        foreach (var shipment in shipments)
            shipmentModels.Add(await _shipmentViewModelService.PrepareShipmentModel(shipment, false));

        var gridModel = new DataSourceResult {
            Data = shipmentModels,
            Total = shipmentModels.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> ShipmentsItemsByShipmentId(string shipmentId)
    {
        var shipment = await _shipmentService.GetShipmentById(shipmentId);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            throw new ArgumentException("No shipment found with the specified id");

        //shipments
        var shipmentModel = await _shipmentViewModelService.PrepareShipmentModel(shipment, true);
        var gridModel = new DataSourceResult {
            Data = shipmentModel.Items,
            Total = shipmentModel.Items.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> AddShipment(string orderId)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (order == null || order.Deleted || !_workContext.HasAccessToOrder(order))
            //No order found with the specified id
            return RedirectToAction("List");

        var model = await _shipmentViewModelService.PrepareShipmentModel(order);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> AddShipment(AddShipmentModel model, bool continueEditing)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("AddShipment", new { orderId = model.OrderId });

        var order = await _orderService.GetOrderById(model.OrderId);
        if (order == null || order.Deleted || !_workContext.HasAccessToOrder(order))
            //No order found with the specified id
            return RedirectToAction("List");

        //a vendor should have access only to his products
        var orderItems = order.OrderItems.Where(_workContext.HasAccessToOrderItem).ToList();

        var (shipment, totalWeight) =
            await _shipmentViewModelService.PrepareShipment(order, orderItems.ToList(), model);
        if (!shipment.ShipmentItems.Any())
        {
            Error(_translationService.GetResource("Admin.Orders.Shipments.NoProductsSelected"));
            return RedirectToAction("AddShipment", new { orderId = model.OrderId });
        }

        //check stock
        var (valid, message) = await _shipmentViewModelService.ValidStockShipment(shipment);
        if (!valid)
        {
            Error(message);
            return RedirectToAction("AddShipment", new { orderId = model.OrderId });
        }

        shipment.TotalWeight = totalWeight;
        await _shipmentService.InsertShipment(shipment);

        //add a note
        await _orderService.InsertOrderNote(new OrderNote {
            Note = $"A shipment #{shipment.ShipmentNumber} has been added",
            DisplayToCustomer = false,
            OrderId = order.Id
        });
        Success(_translationService.GetResource("Admin.Orders.Shipments.Added"));
        return continueEditing
            ? RedirectToAction("ShipmentDetails", new { id = shipment.Id })
            : RedirectToAction("List", new { id = shipment.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> ShipmentDetails(string id)
    {
        var shipment = await _shipmentService.GetShipmentById(id);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            //No shipment found with the specified id
            return RedirectToAction("List");

        var model = await _shipmentViewModelService.PrepareShipmentModel(shipment, true, true);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteShipment(string id)
    {
        var shipment = await _shipmentService.GetShipmentById(id);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            //No shipment found with the specified id
            return RedirectToAction("List");

        await _shipmentService.DeleteShipment(shipment);

        //add a note
        await _orderService.InsertOrderNote(new OrderNote {
            Note = $"A shipment #{shipment.ShipmentNumber} has been deleted",
            DisplayToCustomer = false,
            OrderId = shipment.OrderId
        });
        Success(_translationService.GetResource("Admin.Orders.Shipments.Deleted"));

        return RedirectToAction("Edit", "Order", new { Id = shipment.OrderId });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SetTrackingNumber(ShipmentTrackingModel model)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("ShipmentDetails", new { id = model.Id });

        var shipment = await _shipmentService.GetShipmentById(model.Id);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            //No shipment found with the specified id
            return RedirectToAction("List");

        shipment.TrackingNumber = model.TrackingNumber;
        await _shipmentService.UpdateShipment(shipment);

        return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SetShipmentAdminComment(ShipmentAdminCommentModel model)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("ShipmentDetails", new { id = model.Id });

        var shipment = await _shipmentService.GetShipmentById(model.Id);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            //No shipment found with the specified id
            return RedirectToAction("List");

        shipment.AdminComment = model.AdminComment;
        await _shipmentService.UpdateShipment(shipment);

        return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SetAsShipped(string id)
    {
        var shipment = await _shipmentService.GetShipmentById(id);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            //No shipment found with the specified id
            return RedirectToAction("List");

        try
        {
            await _mediator.Send(new ShipCommand { Shipment = shipment, NotifyCustomer = true });
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> EditShippedDate(ShipmentShippedDateModel model)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("ShipmentDetails", new { id = model.Id });

        var shipment = await _shipmentService.GetShipmentById(model.Id);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            //No shipment found with the specified id
            return RedirectToAction("List");

        try
        {
            if (!model.ShippedDate.HasValue) throw new Exception("Enter shipped date");

            shipment.ShippedDateUtc = model.ShippedDate.ConvertToUtcTime(_dateTimeService);
            await _shipmentService.UpdateShipment(shipment);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SetAsDelivered(string id)
    {
        var shipment = await _shipmentService.GetShipmentById(id);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            //No shipment found with the specified id
            return RedirectToAction("List");

        try
        {
            await _mediator.Send(new DeliveryCommand { Shipment = shipment, NotifyCustomer = true });
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }


    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> EditDeliveryDate(ShipmentDeliveryDateModel model)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("ShipmentDetails", new { id = model.Id });

        var shipment = await _shipmentService.GetShipmentById(model.Id);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            //No shipment found with the specified id
            return RedirectToAction("List");

        try
        {
            if (!model.DeliveryDate.HasValue) throw new Exception("Enter delivery date");

            shipment.DeliveryDateUtc = model.DeliveryDate.ConvertToUtcTime(_dateTimeService);
            await _shipmentService.UpdateShipment(shipment);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc);
            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> PdfPackagingSlip(string shipmentId)
    {
        var shipment = await _shipmentService.GetShipmentById(shipmentId);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            //no shipment found with the specified id
            return RedirectToAction("List");

        var shipments = new List<Shipment> {
            shipment
        };

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await _pdfService.PrintPackagingSlipsToPdf(stream, shipments, _workContext.WorkingLanguage.Id);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", $"packagingslip_{shipment.Id}.pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> PdfPackagingSlipAll(ShipmentListModel model)
    {
        //load shipments
        var shipments = await _shipmentViewModelService.PrepareShipments(model, 1, 100);

        //ensure that we at least one shipment selected
        if (shipments.totalCount == 0)
        {
            Error(_translationService.GetResource("Admin.Orders.Shipments.NoShipmentsSelected"));
            return RedirectToAction("List");
        }

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await _pdfService.PrintPackagingSlipsToPdf(stream, shipments.shipments.ToList(),
                _workContext.WorkingLanguage.Id);
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
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x)
                .ToArray();
            shipments.AddRange(await _shipmentService.GetShipmentsByIds(ids));
        }

        //a vendor should have access only to his shipments
        var shipmentsAccess =
            (from item in shipments
                where _workContext.HasAccessToShipment(item)
                select item).ToList();

        //ensure that we at least one shipment selected
        if (shipments.Count == 0)
        {
            Error(_translationService.GetResource("Vendor.Orders.Shipments.NoShipmentsSelected"));
            return RedirectToAction("List");
        }

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await _pdfService.PrintPackagingSlipsToPdf(stream, shipmentsAccess, _workContext.WorkingLanguage.Id);
            bytes = stream.ToArray();
        }

        return File(bytes, "application/pdf", "packagingslips.pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SetAsShippedSelected(ICollection<string> selectedIds)
    {
        var shipments = new List<Shipment>();
        var shipmentsAccess = new List<Shipment>();
        if (selectedIds != null) shipments.AddRange(await _shipmentService.GetShipmentsByIds(selectedIds.ToArray()));

        //a vendor should have access only to his shipments
        shipmentsAccess.AddRange(from item in shipments
            where _workContext.HasAccessToShipment(item)
            select item);

        foreach (var shipment in shipmentsAccess)
            try
            {
                await _mediator.Send(new ShipCommand { Shipment = shipment, NotifyCustomer = true });
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
        var shipmentsAccess = new List<Shipment>();
        if (selectedIds != null) shipments.AddRange(await _shipmentService.GetShipmentsByIds(selectedIds.ToArray()));

        //a vendor should have access only to his shipments
        shipmentsAccess.AddRange(
            from item in shipments
            where _workContext.HasAccessToShipment(item)
            select item);

        foreach (var shipment in shipmentsAccess)
            try
            {
                await _mediator.Send(new DeliveryCommand { Shipment = shipment, NotifyCustomer = true });
            }
            catch
            {
                //ignore any exception
            }

        return Json(new { Result = true });
    }

    #region Shipment notes

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> ShipmentNotesSelect(string shipmentId)
    {
        var shipment = await _shipmentService.GetShipmentById(shipmentId);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            throw new ArgumentException("No shipment found with the specified id");

        //shipment notes
        var shipmentNoteModels = await _shipmentViewModelService.PrepareShipmentNotes(shipment);
        var gridModel = new DataSourceResult {
            Data = shipmentNoteModels,
            Total = shipmentNoteModels.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> ShipmentNoteAdd(string shipmentId, bool displayToCustomer,
        string message)
    {
        var shipment = await _shipmentService.GetShipmentById(shipmentId);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            return Json(new { Result = false });

        await _shipmentViewModelService.InsertShipmentNote(shipment, displayToCustomer, message);

        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> ShipmentNoteDelete(string id, string shipmentId)
    {
        var shipment = await _shipmentService.GetShipmentById(shipmentId);
        if (shipment == null || !_workContext.HasAccessToShipment(shipment))
            throw new ArgumentException("No shipment found with the specified id");

        await _shipmentViewModelService.DeleteShipmentNote(shipment, id);

        return new JsonResult("");
    }

    #endregion

    #endregion
}