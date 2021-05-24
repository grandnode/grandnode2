using Grand.Business.Checkout.Commands.Models.Shipping;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Pdf;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
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
    [PermissionAuthorize(PermissionSystemName.Shipments)]
    public partial class ShipmentController : BaseAdminController
    {
        #region Fields
        private readonly IShipmentViewModelService _shipmentViewModelService;
        private readonly IOrderService _orderService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly IPdfService _pdfService;
        private readonly IShipmentService _shipmentService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IMediator _mediator;

        #endregion

        public ShipmentController(
            IShipmentViewModelService shipmentViewModelService,
            IOrderService orderService,
            ITranslationService translationService,
            IWorkContext workContext,
            IGroupService groupService,
            IPdfService pdfService,
            IShipmentService shipmentService,
            IDateTimeService dateTimeService,
            IMediator mediator)
        {
            _shipmentViewModelService = shipmentViewModelService;
            _orderService = orderService;
            _translationService = translationService;
            _workContext = workContext;
            _groupService = groupService;
            _pdfService = pdfService;
            _shipmentService = shipmentService;
            _dateTimeService = dateTimeService;
            _mediator = mediator;
        }

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
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                model.VendorId = _workContext.CurrentVendor.Id;

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.StoreId = _workContext.CurrentCustomer.StaffStoreId;
            }

            var shipments = await _shipmentViewModelService.PrepareShipments(model, command.Page, command.PageSize);
            var items = new List<ShipmentModel>();
            foreach (var item in shipments.shipments)
            {
                items.Add(await _shipmentViewModelService.PrepareShipmentModel(item, false));
            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = shipments.totalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> ShipmentsByOrder(string orderId, DataSourceRequest command)
        {
            var order = await _orderService.GetOrderById(orderId);
            if (order == null)
                throw new ArgumentException("No order found with the specified id");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToOrder(order) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return Content("");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return Content("");
            }

            //shipments
            var shipmentModels = new List<ShipmentModel>();
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                var shipments = (await _shipmentService.GetShipmentsByOrder(orderId))
                    //a vendor should have access only to his products
                    .Where(s => _workContext.CurrentVendor == null || _workContext.HasAccessToShipment(order, s))
                    .OrderBy(s => s.CreatedOnUtc)
                    .ToList();
                foreach (var shipment in shipments)
                    shipmentModels.Add(await _shipmentViewModelService.PrepareShipmentModel(shipment, false));
            }
            else
            {
                var shipments = (await _shipmentService.GetShipmentsByOrder(orderId))
                   .OrderBy(s => s.CreatedOnUtc)
                   .ToList();
                foreach (var shipment in shipments)
                    shipmentModels.Add(await _shipmentViewModelService.PrepareShipmentModel(shipment, false));
            }
            var gridModel = new DataSourceResult
            {
                Data = shipmentModels,
                Total = shipmentModels.Count
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> ShipmentsItemsByShipmentId(string shipmentId, DataSourceRequest command)
        {
            var shipment = await _shipmentService.GetShipmentById(shipmentId);
            if (shipment == null)
                throw new ArgumentException("No shipment found with the specified id");

            var order = await _orderService.GetOrderById(shipment.OrderId);
            if (order == null)
                throw new ArgumentException("No order found with the specified id");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToOrder(order) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return Content("");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return Content("");
            }

            //shipments
            var shipmentModel = await _shipmentViewModelService.PrepareShipmentModel(shipment, true);
            var gridModel = new DataSourceResult
            {
                Data = shipmentModel.Items,
                Total = shipmentModel.Items.Count
            };

            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public async Task<IActionResult> AddShipment(string orderId)
        {
            var order = await _orderService.GetOrderById(orderId);
            if (order == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToOrder(order) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            var model = await _shipmentViewModelService.PrepareShipmentModel(order);

            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost, ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
        public async Task<IActionResult> AddShipment(string orderId, IFormCollection form, bool continueEditing)
        {
            var order = await _orderService.GetOrderById(orderId);
            if (order == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToOrder(order) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && order.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            var orderItems = order.OrderItems;
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                orderItems = orderItems.Where(_workContext.HasAccessToOrderItem).ToList();
            }
            var sh = await _shipmentViewModelService.PrepareShipment(order, orderItems.ToList(), form);
            Shipment shipment = sh.shipment;
            //check stock
            var (valid, message) = await _shipmentViewModelService.ValidStockShipment(shipment);
            if (!valid)
            {
                Error(message);
                return RedirectToAction("AddShipment", new { orderId = orderId });
            }
                //if we have at least one item in the shipment, then save it
            if (shipment != null && shipment.ShipmentItems.Count > 0)
            {
                shipment.TotalWeight = sh.totalWeight;
                await _shipmentService.InsertShipment(shipment);

                //add a note
                await _orderService.InsertOrderNote(new OrderNote
                {
                    Note = $"A shipment #{shipment.ShipmentNumber} has been added",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,
                });

                await _shipmentViewModelService.LogShipment(shipment.Id, $"A shipment #{shipment.ShipmentNumber} has been added");
                Success(_translationService.GetResource("Admin.Orders.Shipments.Added"));
                return continueEditing
                           ? RedirectToAction("ShipmentDetails", new { id = shipment.Id })
                           : RedirectToAction("List", new { id = shipment.Id });
            }

            Error(_translationService.GetResource("Admin.Orders.Shipments.NoProductsSelected"));
            return RedirectToAction("AddShipment", new { orderId = orderId });
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> ShipmentDetails(string id)
        {
            var shipment = await _shipmentService.GetShipmentById(id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            var orderId = shipment.OrderId;
            var order = await _orderService.GetOrderById(orderId);
            if (order == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToShipment(order, shipment) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            var model = await _shipmentViewModelService.PrepareShipmentModel(shipment, true, true);
            return View(model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> DeleteShipment(string id)
        {
            var shipment = await _shipmentService.GetShipmentById(id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            if (_workContext.CurrentVendor != null && _workContext.CurrentVendor.Id != shipment.VendorId)
            {
                Error(_translationService.GetResource("Admin.Orders.Shipments.VendorAccess"));
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }

            var orderId = shipment.OrderId;
            var order = await _orderService.GetOrderById(orderId);
            if (order == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToShipment(order, shipment) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            //delete shipment
            await _shipmentService.DeleteShipment(shipment);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = $"A shipment #{shipment.ShipmentNumber} has been deleted",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });

            await _shipmentViewModelService.LogShipment(shipment.Id, $"A shipment #{shipment.ShipmentNumber} has been deleted");

            Success(_translationService.GetResource("Admin.Orders.Shipments.Deleted"));

            return RedirectToAction("Edit", "Order", new { Id = order.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> SetTrackingNumber(ShipmentModel model)
        {
            var shipment = await _shipmentService.GetShipmentById(model.Id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }
            if (_workContext.CurrentVendor != null && _workContext.CurrentVendor.Id != shipment.VendorId)
            {
                Error(_translationService.GetResource("Admin.Orders.Shipments.VendorAccess"));
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }

            var order = await _orderService.GetOrderById(shipment.OrderId);
            if (order == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToShipment(order, shipment) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            shipment.TrackingNumber = model.TrackingNumber;
            await _shipmentService.UpdateShipment(shipment);

            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> SetShipmentAdminComment(ShipmentModel model)
        {
            var shipment = await _shipmentService.GetShipmentById(model.Id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }
            if (_workContext.CurrentVendor != null && _workContext.CurrentVendor.Id != shipment.VendorId)
            {
                Error(_translationService.GetResource("Admin.Orders.Shipments.VendorAccess"));
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }

            var order = await _orderService.GetOrderById(shipment.OrderId);
            if (order == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToShipment(order, shipment) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
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
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }
            if (_workContext.CurrentVendor != null && _workContext.CurrentVendor.Id != shipment.VendorId)
            {
                Error(_translationService.GetResource("Admin.Orders.Shipments.VendorAccess"));
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }

            var order = await _orderService.GetOrderById(shipment.OrderId);
            if (order == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToShipment(order, shipment) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            try
            {
                await _mediator.Send(new ShipCommand() { Shipment = shipment, NotifyCustomer = true });
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
            catch (Exception exc)
            {
                //error
                Error(exc, true);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> EditShippedDate(ShipmentModel model)
        {
            var shipment = await _shipmentService.GetShipmentById(model.Id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }
            if (_workContext.CurrentVendor != null && _workContext.CurrentVendor.Id != shipment.VendorId)
            {
                Error(_translationService.GetResource("Admin.Orders.Shipments.VendorAccess"));
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }

            var order = await _orderService.GetOrderById(shipment.OrderId);
            if (order == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToShipment(order, shipment) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            try
            {
                if (!model.ShippedDate.HasValue)
                {
                    throw new Exception("Enter shipped date");
                }
                shipment.ShippedDateUtc = model.ShippedDate.ConvertToUtcTime(_dateTimeService);
                await _shipmentService.UpdateShipment(shipment);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
            catch (Exception exc)
            {
                //error
                Error(exc, true);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> SetAsDelivered(string id)
        {
            var shipment = await _shipmentService.GetShipmentById(id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }
            if (_workContext.CurrentVendor != null && _workContext.CurrentVendor.Id != shipment.VendorId)
            {
                Error(_translationService.GetResource("Admin.Orders.Shipments.VendorAccess"));
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }

            var order = await _orderService.GetOrderById(shipment.OrderId);
            if (order == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToShipment(order, shipment) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            try
            {
                await _mediator.Send(new DeliveryCommand() { Shipment = shipment, NotifyCustomer = true });
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
            catch (Exception exc)
            {
                //error
                Error(exc, true);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
        }


        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> EditDeliveryDate(ShipmentModel model)
        {
            var shipment = await _shipmentService.GetShipmentById(model.Id);
            if (shipment == null)
                //No shipment found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            if (_workContext.CurrentVendor != null && _workContext.CurrentVendor.Id != shipment.VendorId)
            {
                Error(_translationService.GetResource("Admin.Orders.Shipments.VendorAccess"));
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }

            var order = await _orderService.GetOrderById(shipment.OrderId);
            if (order == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToShipment(order, shipment) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            try
            {
                if (!model.DeliveryDate.HasValue)
                {
                    throw new Exception("Enter delivery date");
                }
                shipment.DeliveryDateUtc = model.DeliveryDate.ConvertToUtcTime(_dateTimeService);
                await _shipmentService.UpdateShipment(shipment);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
            catch (Exception exc)
            {
                //error
                Error(exc, true);
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> EditUserFields(string id, ShipmentModel model)
        {
            var shipment = await _shipmentService.GetShipmentById(id);
            if (shipment == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor does not have access to this functionality
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }

            if (_workContext.CurrentVendor != null && _workContext.CurrentVendor.Id != shipment.VendorId)
            {
                Error(_translationService.GetResource("Admin.Orders.Shipments.VendorAccess"));
                return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
            }
            shipment.UserFields = model.UserFields;
            await _shipmentService.UpdateShipment(shipment);

            //selected tab
            await SaveSelectedTabIndex();

            return RedirectToAction("ShipmentDetails", new { id = shipment.Id });
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> PdfPackagingSlip(string shipmentId)
        {
            var shipment = await _shipmentService.GetShipmentById(shipmentId);
            if (shipment == null)
                //no shipment found with the specified id
                return RedirectToAction("List");

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return RedirectToAction("List");
            }

            var order = await _orderService.GetOrderById(shipment.OrderId);
            if (order == null)
                //No order found with the specified id
                return RedirectToAction("List");

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !_workContext.HasAccessToShipment(order, shipment) && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return RedirectToAction("List");

            var shipments = new List<Shipment>
            {
                shipment
            };

            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                await _pdfService.PrintPackagingSlipsToPdf(stream, shipments, _workContext.WorkingLanguage.Id);
                bytes = stream.ToArray();
            }
            return File(bytes, "application/pdf", string.Format("packagingslip_{0}.pdf", shipment.Id));
        }

        [PermissionAuthorizeAction(PermissionActionName.Export)]
        [HttpPost]
        public async Task<IActionResult> PdfPackagingSlipAll(ShipmentListModel model)
        {
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.StoreId = _workContext.CurrentCustomer.StaffStoreId;
            }
            if (_workContext.CurrentVendor != null)
            {
                model.VendorId = _workContext.CurrentVendor.Id;
            }

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
                await _pdfService.PrintPackagingSlipsToPdf(stream, shipments.shipments.ToList(), _workContext.WorkingLanguage.Id);
                bytes = stream.ToArray();
            }
            return File(bytes, "application/pdf", "packagingslips.pdf");
        }

        [PermissionAuthorizeAction(PermissionActionName.Export)]
        [HttpPost]
        public async Task<IActionResult> PdfPackagingSlipSelected(string selectedIds)
        {
            var shipments = new List<Shipment>();
            var shipments_access = new List<Shipment>();
            if (selectedIds != null)
            {
                var ids = selectedIds
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x)
                    .ToArray();
                shipments.AddRange(await _shipmentService.GetShipmentsByIds(ids));
            }

            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                foreach (var item in shipments)
                {
                    var hasaccess = item.VendorId == _workContext.CurrentVendor.Id;
                    if (hasaccess)
                        shipments_access.Add(item);
                }
            }
            else
            {
                var storeId = "";
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    storeId = _workContext.CurrentCustomer.StaffStoreId;
                }
                shipments_access = shipments.Where(x => x.StoreId == storeId || string.IsNullOrEmpty(storeId)).ToList();
            }
            //ensure that we at least one shipment selected
            if (shipments.Count == 0)
            {
                Error(_translationService.GetResource("Admin.Orders.Shipments.NoShipmentsSelected"));
                return RedirectToAction("List");
            }

            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                await _pdfService.PrintPackagingSlipsToPdf(stream, shipments_access, _workContext.WorkingLanguage.Id);
                bytes = stream.ToArray();
            }
            return File(bytes, "application/pdf", "packagingslips.pdf");
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> SetAsShippedSelected(ICollection<string> selectedIds)
        {
            var shipments = new List<Shipment>();
            var shipments_access = new List<Shipment>();
            if (selectedIds != null)
            {
                shipments.AddRange(await _shipmentService.GetShipmentsByIds(selectedIds.ToArray()));
            }
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                foreach (var item in shipments)
                {
                    var hasaccess = item.VendorId == _workContext.CurrentVendor.Id;
                    if (hasaccess)
                        shipments_access.Add(item);
                }
            }
            else
            {
                var storeId = "";
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    storeId = _workContext.CurrentCustomer.StaffStoreId;
                }
                shipments_access = shipments.Where(x => x.StoreId == storeId || string.IsNullOrEmpty(storeId)).ToList();
            }

            foreach (var shipment in shipments_access)
            {
                try
                {
                    await _mediator.Send(new ShipCommand() { Shipment = shipment, NotifyCustomer = true });
                }
                catch
                {
                    //ignore any exception
                }
            }

            return Json(new { Result = true });
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> SetAsDeliveredSelected(ICollection<string> selectedIds)
        {
            var shipments = new List<Shipment>();
            var shipments_access = new List<Shipment>();
            if (selectedIds != null)
            {
                shipments.AddRange(await _shipmentService.GetShipmentsByIds(selectedIds.ToArray()));
            }
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                foreach (var item in shipments)
                {
                    var hasaccess = item.VendorId == _workContext.CurrentVendor.Id;
                    if (hasaccess)
                        shipments_access.Add(item);
                }
            }
            else
            {
                var storeId = "";
                if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                {
                    storeId = _workContext.CurrentCustomer.StaffStoreId;
                }
                shipments_access = shipments.Where(x => x.StoreId == storeId || string.IsNullOrEmpty(storeId)).ToList();
            }

            foreach (var shipment in shipments_access)
            {
                try
                {
                    await _mediator.Send(new DeliveryCommand() { Shipment = shipment, NotifyCustomer = true });
                }
                catch
                {
                    //ignore any exception
                }
            }

            return Json(new { Result = true });
        }

        #region Shipment notes

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> ShipmentNotesSelect(string shipmentId, DataSourceRequest command)
        {
            var shipment = await _shipmentService.GetShipmentById(shipmentId);
            if (shipment == null)
                throw new ArgumentException("No shipment found with the specified id");

            if (_workContext.CurrentVendor != null && _workContext.CurrentVendor.Id != shipment.VendorId)
                return Json(new DataSourceResult());

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return Content("");
            }
            //shipment notes
            var shipmentNoteModels = await _shipmentViewModelService.PrepareShipmentNotes(shipment);
            var gridModel = new DataSourceResult
            {
                Data = shipmentNoteModels,
                Total = shipmentNoteModels.Count
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> ShipmentNoteAdd(string shipmentId, string downloadId, bool displayToCustomer, string message)
        {
            var shipment = await _shipmentService.GetShipmentById(shipmentId);
            if (shipment == null)
                return Json(new { Result = false });

            if (_workContext.CurrentVendor != null && _workContext.CurrentVendor.Id != shipment.VendorId)
                return Json(new { Result = false });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return Json(new { Result = false });
            }
            await _shipmentViewModelService.InsertShipmentNote(shipment, downloadId, displayToCustomer, message);

            return Json(new { Result = true });
        }

        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        [HttpPost]
        public async Task<IActionResult> ShipmentNoteDelete(string id, string shipmentId)
        {
            var shipment = await _shipmentService.GetShipmentById(shipmentId);
            if (shipment == null)
                throw new ArgumentException("No shipment found with the specified id");

            if (_workContext.CurrentVendor != null && _workContext.CurrentVendor.Id != shipment.VendorId)
                return Json(new { Result = false });

            if (await _groupService.IsStaff(_workContext.CurrentCustomer) && shipment.StoreId != _workContext.CurrentCustomer.StaffStoreId)
            {
                return Json(new { Result = false });
            }

            await _shipmentViewModelService.DeleteShipmentNote(shipment, id);

            return new JsonResult("");
        }
        #endregion

        #endregion
    }
}
