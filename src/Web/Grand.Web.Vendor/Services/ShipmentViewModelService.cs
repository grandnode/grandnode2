using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.Vendor.Extensions;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Models.Shipment;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Vendor.Services;

public class ShipmentViewModelService : IShipmentViewModelService
{
    private readonly IDateTimeService _dateTimeService;
    private readonly IGroupService _groupService;
    private readonly IMeasureService _measureService;
    private readonly MeasureSettings _measureSettings;
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly IShipmentService _shipmentService;
    private readonly ShippingProviderSettings _shippingProviderSettings;
    private readonly IShippingService _shippingService;
    private readonly ShippingSettings _shippingSettings;
    private readonly IStockQuantityService _stockQuantityService;
    private readonly ITranslationService _translationService;
    private readonly IWarehouseService _warehouseService;
    private readonly IWorkContext _workContext;

    public ShipmentViewModelService(
        IOrderService orderService,
        IWorkContext workContext,
        IGroupService groupService,
        IProductService productService,
        IShipmentService shipmentService,
        IWarehouseService warehouseService,
        IMeasureService measureService,
        IDateTimeService dateTimeService,
        ITranslationService translationService,
        IShippingService shippingService,
        IStockQuantityService stockQuantityService,
        MeasureSettings measureSettings,
        ShippingSettings shippingSettings,
        ShippingProviderSettings shippingProviderSettings)
    {
        _orderService = orderService;
        _workContext = workContext;
        _groupService = groupService;
        _productService = productService;
        _shipmentService = shipmentService;
        _warehouseService = warehouseService;
        _measureService = measureService;
        _dateTimeService = dateTimeService;
        _translationService = translationService;
        _shippingService = shippingService;
        _stockQuantityService = stockQuantityService;
        _measureSettings = measureSettings;
        _shippingSettings = shippingSettings;
        _shippingProviderSettings = shippingProviderSettings;
    }

    public virtual async Task<ShipmentModel> PrepareShipmentModel(Shipment shipment, bool prepareProducts,
        bool prepareShipmentEvent = false)
    {
        //measures
        var baseWeight = await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId);
        var baseWeightIn = baseWeight != null ? baseWeight.Name : "";
        var baseDimension = await _measureService.GetMeasureDimensionById(_measureSettings.BaseDimensionId);
        var baseDimensionIn = baseDimension != null ? baseDimension.Name : "";
        var order = await _orderService.GetOrderById(shipment.OrderId);

        var model = new ShipmentModel {
            Id = shipment.Id,
            ShipmentNumber = shipment.ShipmentNumber,
            OrderId = shipment.OrderId,
            OrderNumber = order?.OrderNumber ?? 0,
            OrderCode = order != null ? order.Code : "",
            TrackingNumber = shipment.TrackingNumber,
            TotalWeight = shipment.TotalWeight.HasValue ? $"{shipment.TotalWeight:F2} [{baseWeightIn}]" : "",
            ShippedDate = shipment.ShippedDateUtc.HasValue
                ? _dateTimeService.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc)
                : new DateTime?(),
            ShippedDateUtc = shipment.ShippedDateUtc,
            CanShip = !shipment.ShippedDateUtc.HasValue,
            DeliveryDate = shipment.DeliveryDateUtc.HasValue
                ? _dateTimeService.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc)
                : new DateTime?(),
            DeliveryDateUtc = shipment.DeliveryDateUtc,
            CanDeliver = shipment.ShippedDateUtc.HasValue && !shipment.DeliveryDateUtc.HasValue,
            AdminComment = shipment.AdminComment
        };

        if (prepareProducts)
            foreach (var shipmentItem in shipment.ShipmentItems)
            {
                var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == shipmentItem.OrderItemId);
                if (orderItem == null)
                    continue;

                if (orderItem.VendorId != _workContext.CurrentVendor.Id)
                    continue;

                //quantities
                var qtyInThisShipment = shipmentItem.Quantity;
                var maxQtyToAdd = orderItem.OpenQty;
                var qtyOrdered = shipmentItem.Quantity;
                var qtyInAllShipments = orderItem.ShipQty;
                var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);
                if (product != null)
                {
                    var warehouse = await _warehouseService.GetWarehouseById(shipmentItem.WarehouseId);
                    var shipmentItemModel = new ShipmentModel.ShipmentItemModel {
                        Id = shipmentItem.Id,
                        OrderItemId = orderItem.Id,
                        ProductId = orderItem.ProductId,
                        ProductName = product.Name,
                        Sku = product.FormatSku(orderItem.Attributes),
                        AttributeInfo = orderItem.AttributeDescription,
                        ShippedFromWarehouse = warehouse?.Name,
                        ShipSeparately = product.ShipSeparately,
                        ItemWeight = orderItem.ItemWeight.HasValue
                            ? $"{orderItem.ItemWeight:F2} [{baseWeightIn}]"
                            : "",
                        ItemDimensions =
                            $"{product.Length:F2} x {product.Width:F2} x {product.Height:F2} [{baseDimensionIn}]",
                        QuantityOrdered = qtyOrdered,
                        QuantityInThisShipment = qtyInThisShipment,
                        QuantityInAllShipments = qtyInAllShipments,
                        QuantityToAdd = maxQtyToAdd
                    };

                    model.Items.Add(shipmentItemModel);
                }
            }

        if (prepareShipmentEvent && !string.IsNullOrEmpty(shipment.TrackingNumber))
        {
            var srcm = _shippingService.LoadShippingRateCalculationProviderBySystemName(
                order.ShippingRateProviderSystemName);
            if (srcm != null &&
                srcm.IsShippingRateMethodActive(_shippingProviderSettings))
            {
                var shipmentTracker = srcm.ShipmentTracker;
                if (shipmentTracker != null)
                {
                    model.TrackingNumberUrl = await shipmentTracker.GetUrl(shipment.TrackingNumber);
                    if (_shippingSettings.DisplayShipmentEventsToStoreOwner)
                    {
                        var shipmentEvents = await shipmentTracker.GetShipmentEvents(shipment.TrackingNumber);
                        if (shipmentEvents != null)
                            foreach (var shipmentEvent in shipmentEvents)
                            {
                                var shipmentStatusEventModel = new ShipmentModel.ShipmentStatusEventModel {
                                    Date = shipmentEvent.Date,
                                    EventName = shipmentEvent.EventName,
                                    Location = shipmentEvent.Location
                                };
                                model.ShipmentStatusEvents.Add(shipmentStatusEventModel);
                            }
                    }
                }
            }
        }

        return model;
    }


    public virtual async Task<int> GetStockQty(Product product, string warehouseId)
    {
        var qty = new List<int>();
        foreach (var item in product.BundleProducts)
        {
            var p1 = await _productService.GetProductById(item.ProductId);
            if (p1.UseMultipleWarehouses)
            {
                var stock = p1.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
                if (stock != null) qty.Add(stock.StockQuantity / item.Quantity);
            }
            else
            {
                qty.Add(p1.StockQuantity / item.Quantity);
            }
        }

        return qty.Count > 0 ? qty.Min() : 0;
    }

    public virtual async Task<int> GetReservedQty(Product product, string warehouseId)
    {
        var qty = new List<int>();
        foreach (var item in product.BundleProducts)
        {
            var p1 = await _productService.GetProductById(item.ProductId);
            if (p1.UseMultipleWarehouses)
            {
                var stock = p1.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
                if (stock != null) qty.Add(stock.ReservedQuantity / item.Quantity);
            }
        }

        return qty.Count > 0 ? qty.Min() : 0;
    }

    public virtual async Task<IList<ShipmentModel.ShipmentNote>> PrepareShipmentNotes(Shipment shipment)
    {
        //shipment notes
        var shipmentNoteModels = new List<ShipmentModel.ShipmentNote>();
        foreach (var shipmentNote in (await _shipmentService.GetShipmentNotes(shipment.Id))
                 .OrderByDescending(on => on.CreatedOnUtc))
            shipmentNoteModels.Add(new ShipmentModel.ShipmentNote {
                Id = shipmentNote.Id,
                ShipmentId = shipment.Id,
                DisplayToCustomer = shipmentNote.DisplayToCustomer,
                Note = shipmentNote.Note,
                CreatedOn = _dateTimeService.ConvertToUserTime(shipmentNote.CreatedOnUtc, DateTimeKind.Utc),
                CreatedByCustomer = shipmentNote.CreatedByCustomer
            });

        return shipmentNoteModels;
    }

    public virtual async Task InsertShipmentNote(Shipment shipment, bool displayToCustomer,
        string message)
    {
        var shipmentNote = new ShipmentNote {
            DisplayToCustomer = displayToCustomer,
            Note = message,
            ShipmentId = shipment.Id
        };
        await _shipmentService.InsertShipmentNote(shipmentNote);
    }

    public virtual async Task DeleteShipmentNote(Shipment shipment, string id)
    {
        var shipmentNote = (await _shipmentService.GetShipmentNotes(shipment.Id)).FirstOrDefault(on => on.Id == id);
        if (shipmentNote == null)
            throw new ArgumentException("No shipment note found with the specified id");

        shipmentNote.ShipmentId = shipment.Id;
        await _shipmentService.DeleteShipmentNote(shipmentNote);
    }

    public virtual async Task<(IEnumerable<Shipment> shipments, int totalCount)> PrepareShipments(
        ShipmentListModel model, int pageIndex, int pageSize)
    {
        DateTime? startDateValue = model.StartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

        //load shipments
        var shipments = await _shipmentService.GetAllShipments(
            vendorId: _workContext.CurrentVendor.Id,
            warehouseId: model.WarehouseId,
            shippingCity: model.City,
            trackingNumber: model.TrackingNumber,
            loadNotShipped: model.LoadNotShipped,
            createdFromUtc: startDateValue,
            createdToUtc: endDateValue,
            pageIndex: pageIndex - 1,
            pageSize: pageSize);

        return (shipments.ToList(), shipments.TotalCount);
    }

    public virtual async Task<ShipmentListModel> PrepareShipmentListModel()
    {
        var model = new ShipmentListModel();
        //warehouses
        model.AvailableWarehouses.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var w in await _warehouseService.GetAllWarehouses())
            model.AvailableWarehouses.Add(new SelectListItem { Text = w.Name, Value = w.Id });

        return model;
    }

    public virtual async Task<ShipmentModel> PrepareShipmentModel(Order order)
    {
        var model = new ShipmentModel {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber
        };

        //measures
        var baseWeight = await _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId);
        var baseWeightIn = baseWeight != null ? baseWeight.Name : "";
        var baseDimension = await _measureService.GetMeasureDimensionById(_measureSettings.BaseDimensionId);
        var baseDimensionIn = baseDimension != null ? baseDimension.Name : "";

        var orderItems = order.OrderItems;
        //a vendor should have access only to his products
        if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            orderItems = orderItems.Where(_workContext.HasAccessToOrderItem).ToList();

        foreach (var orderItem in orderItems)
        {
            var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);
            //we can ship only shippable products
            if (!product.IsShipEnabled)
                continue;

            //quantities
            var qtyInThisShipment = 0;
            var maxQtyToAdd = orderItem.OpenQty;
            var qtyOrdered = orderItem.Quantity;
            var qtyInAllShipments = orderItem.ShipQty;

            //ensure that this product can be added to a shipment
            if (maxQtyToAdd <= 0)
                continue;

            var shipmentItemModel = new ShipmentModel.ShipmentItemModel {
                OrderItemId = orderItem.Id,
                ProductId = orderItem.ProductId,
                ProductName = product.Name,
                WarehouseId = orderItem.WarehouseId,
                Sku = product.FormatSku(orderItem.Attributes),
                AttributeInfo = orderItem.AttributeDescription,
                ShipSeparately = product.ShipSeparately,
                ItemWeight = orderItem.ItemWeight.HasValue ? $"{orderItem.ItemWeight:F2} [{baseWeightIn}]" : "",
                ItemDimensions =
                    $"{product.Length:F2} x {product.Width:F2} x {product.Height:F2} [{baseDimensionIn}]",
                QuantityOrdered = qtyOrdered,
                QuantityInThisShipment = qtyInThisShipment,
                QuantityInAllShipments = qtyInAllShipments,
                QuantityToAdd = maxQtyToAdd
            };

            switch (product.ManageInventoryMethodId)
            {
                case ManageInventoryMethod.ManageStock when product.UseMultipleWarehouses:
                {
                    //multiple warehouses supported
                    shipmentItemModel.AllowToChooseWarehouse = true;
                    foreach (var pwi in product.ProductWarehouseInventory
                                 .OrderBy(w => w.WarehouseId).ToList())
                    {
                        var warehouse = await _warehouseService.GetWarehouseById(pwi.WarehouseId);
                        if (warehouse != null)
                            shipmentItemModel.AvailableWarehouses.Add(
                                new ShipmentModel.ShipmentItemModel.WarehouseInfo {
                                    WarehouseId = warehouse.Id,
                                    WarehouseName = warehouse.Name,
                                    WarehouseCode = warehouse.Code,
                                    StockQuantity = pwi.StockQuantity,
                                    ReservedQuantity = pwi.ReservedQuantity
                                });
                    }

                    break;
                }
                case ManageInventoryMethod.ManageStock:
                {
                    //multiple warehouses are not supported
                    var warehouse = await _warehouseService.GetWarehouseById(product.WarehouseId);
                    if (warehouse != null)
                        shipmentItemModel.AvailableWarehouses.Add(new ShipmentModel.ShipmentItemModel.WarehouseInfo {
                            WarehouseId = warehouse.Id,
                            WarehouseName = warehouse.Name,
                            WarehouseCode = warehouse.Code,
                            StockQuantity = product.StockQuantity
                        });

                    break;
                }
                case ManageInventoryMethod.ManageStockByAttributes when product.UseMultipleWarehouses:
                {
                    //multiple warehouses supported
                    shipmentItemModel.AllowToChooseWarehouse = true;
                    var comb = product.FindProductAttributeCombination(orderItem.Attributes);
                    if (comb != null)
                        foreach (var pwi in comb.WarehouseInventory
                                     .OrderBy(w => w.WarehouseId).ToList())
                        {
                            var warehouse = await _warehouseService.GetWarehouseById(pwi.WarehouseId);
                            if (warehouse != null)
                                shipmentItemModel.AvailableWarehouses.Add(
                                    new ShipmentModel.ShipmentItemModel.WarehouseInfo {
                                        WarehouseId = warehouse.Id,
                                        WarehouseName = warehouse.Name,
                                        StockQuantity = pwi.StockQuantity,
                                        WarehouseCode = warehouse.Code,
                                        ReservedQuantity = pwi.ReservedQuantity
                                    });
                        }

                    break;
                }
                case ManageInventoryMethod.ManageStockByAttributes:
                {
                    //multiple warehouses are not supported
                    var warehouse = await _warehouseService.GetWarehouseById(product.WarehouseId);
                    if (warehouse != null)
                        shipmentItemModel.AvailableWarehouses.Add(new ShipmentModel.ShipmentItemModel.WarehouseInfo {
                            WarehouseId = warehouse.Id,
                            WarehouseName = warehouse.Name,
                            WarehouseCode = warehouse.Code,
                            StockQuantity = product.StockQuantity
                        });

                    break;
                }
            }

            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByBundleProducts)
            {
                if (!string.IsNullOrEmpty(orderItem.WarehouseId))
                {
                    var warehouse = await _warehouseService.GetWarehouseById(product.WarehouseId);
                    if (warehouse != null)
                        shipmentItemModel.AvailableWarehouses.Add(new ShipmentModel.ShipmentItemModel.WarehouseInfo {
                            WarehouseId = warehouse.Id,
                            WarehouseName = warehouse.Name,
                            WarehouseCode = warehouse.Code,
                            StockQuantity = await GetStockQty(product, orderItem.WarehouseId),
                            ReservedQuantity = await GetReservedQty(product, orderItem.WarehouseId)
                        });
                }
                else
                {
                    shipmentItemModel.AllowToChooseWarehouse = false;
                    if (shipmentItemModel.AllowToChooseWarehouse)
                    {
                        var warehouses = await _warehouseService.GetAllWarehouses();
                        foreach (var warehouse in warehouses)
                            shipmentItemModel.AvailableWarehouses.Add(
                                new ShipmentModel.ShipmentItemModel.WarehouseInfo {
                                    WarehouseId = warehouse.Id,
                                    WarehouseName = warehouse.Name,
                                    WarehouseCode = warehouse.Code,
                                    StockQuantity = await GetStockQty(product, warehouse.Id),
                                    ReservedQuantity = await GetReservedQty(product, warehouse.Id)
                                });
                    }
                }
            }

            model.Items.Add(shipmentItemModel);
        }

        return model;
    }

    public virtual async Task<(bool valid, string message)> ValidStockShipment(Shipment shipment)
    {
        foreach (var item in shipment.ShipmentItems)
        {
            var product = await _productService.GetProductById(item.ProductId);
            switch (product.ManageInventoryMethodId)
            {
                case ManageInventoryMethod.ManageStock:
                {
                    var stock = _stockQuantityService.GetTotalStockQuantity(product, false,
                        item.WarehouseId);
                    if (stock - item.Quantity < 0)
                        return (false, $"Out of stock for product {product.Name}");
                    break;
                }
                case ManageInventoryMethod.ManageStockByAttributes:
                {
                    var combination = product.FindProductAttributeCombination(item.Attributes);
                    if (combination == null)
                        return (false, $"Can't find combination for product {product.Name}");

                    var stock = _stockQuantityService.GetTotalStockQuantityForCombination(product, combination,
                        false, item.WarehouseId);
                    if (stock - item.Quantity < 0)
                        return (false, $"Out of stock for product {product.Name}");
                    break;
                }
            }
        }

        return (true, string.Empty);
    }

    public virtual async Task<(Shipment shipment, double? totalWeight)> PrepareShipment(Order order,
        IEnumerable<OrderItem> orderItems, AddShipmentModel model)
    {
        var shipment = new Shipment {
            OrderId = order.Id,
            SeId = order.SeId,
            TrackingNumber = model.TrackingNumber,
            TotalWeight = null,
            ShippedDateUtc = null,
            DeliveryDateUtc = null,
            AdminComment = model.AdminComment,
            StoreId = order.StoreId,
            VendorId = _workContext.CurrentVendor.Id
        };
        double? totalWeight = null;
        foreach (var orderItem in orderItems)
        {
            //is shippable
            if (!orderItem.IsShipEnabled)
                continue;

            //ensure that this product can be shipped (have at least one item to ship)
            if (orderItem.OpenQty <= 0)
                continue;

            var shipmentItemModel = model.Items.FirstOrDefault(x => x.OrderItemId == orderItem.Id);
            if (shipmentItemModel == null)
                continue;

            var product = await _productService.GetProductById(orderItem.ProductId);
            string warehouseId;
            if (product != null && ((product.ManageInventoryMethodId is ManageInventoryMethod.ManageStock
                                         or ManageInventoryMethod.ManageStockByAttributes &&
                                     product.UseMultipleWarehouses) || product.ManageInventoryMethodId ==
                    ManageInventoryMethod.ManageStockByBundleProducts))
                //multiple warehouses supported
                //warehouse is chosen by a store owner
                warehouseId = shipmentItemModel.WarehouseId;
            else
                //multiple warehouses are not supported
                warehouseId = orderItem.WarehouseId;

            //validate quantity
            if (shipmentItemModel.QuantityToAdd <= 0)
                continue;
            if (shipmentItemModel.QuantityToAdd > orderItem.OpenQty)
                shipmentItemModel.QuantityToAdd = orderItem.OpenQty;

            //ok. we have at least one item. create a shipment (if it does not exist)
            var orderItemTotalWeight = orderItem.ItemWeight * shipmentItemModel.QuantityToAdd;
            if (orderItemTotalWeight.HasValue)
            {
                totalWeight ??= 0;
                totalWeight += orderItemTotalWeight.Value;
            }

            //create a shipment item
            var shipmentItem = new ShipmentItem {
                ProductId = orderItem.ProductId,
                OrderItemId = orderItem.Id,
                Quantity = shipmentItemModel.QuantityToAdd,
                WarehouseId = warehouseId,
                Attributes = orderItem.Attributes
            };
            shipment.ShipmentItems.Add(shipmentItem);
        }

        return (shipment, totalWeight);
    }
}