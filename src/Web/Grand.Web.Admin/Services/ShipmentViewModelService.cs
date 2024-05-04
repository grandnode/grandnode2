using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class ShipmentViewModelService : IShipmentViewModelService
{
    private readonly ICountryService _countryService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IDownloadService _downloadService;
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
        IProductService productService,
        IShipmentService shipmentService,
        IWarehouseService warehouseService,
        IMeasureService measureService,
        IDateTimeService dateTimeService,
        ICountryService countryService,
        ITranslationService translationService,
        IDownloadService downloadService,
        IShippingService shippingService,
        IStockQuantityService stockQuantityService,
        MeasureSettings measureSettings,
        ShippingSettings shippingSettings,
        ShippingProviderSettings shippingProviderSettings)
    {
        _orderService = orderService;
        _workContext = workContext;
        _productService = productService;
        _shipmentService = shipmentService;
        _warehouseService = warehouseService;
        _measureService = measureService;
        _dateTimeService = dateTimeService;
        _countryService = countryService;
        _translationService = translationService;
        _downloadService = downloadService;
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
            AdminComment = shipment.AdminComment,
            UserFields = shipment.UserFields
        };

        if (prepareProducts)
            foreach (var shipmentItem in shipment.ShipmentItems)
            {
                var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == shipmentItem.OrderItemId);
                if (orderItem == null)
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
                                var shipmentStatusEventModel = new ShipmentModel.ShipmentStatusEventModel();
                                var shipmentEventCountry =
                                    await _countryService.GetCountryByTwoLetterIsoCode(shipmentEvent.CountryCode);
                                shipmentStatusEventModel.Country = shipmentEventCountry != null
                                    ? shipmentEventCountry.GetTranslation(x => x.Name,
                                        _workContext.WorkingLanguage.Id)
                                    : shipmentEvent.CountryCode;
                                shipmentStatusEventModel.Date = shipmentEvent.Date;
                                shipmentStatusEventModel.EventName = shipmentEvent.EventName;
                                shipmentStatusEventModel.Location = shipmentEvent.Location;
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
        var _qty = new List<int>();
        foreach (var item in product.BundleProducts)
        {
            var p1 = await _productService.GetProductById(item.ProductId);
            if (p1.UseMultipleWarehouses)
            {
                var stock = p1.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
                if (stock != null) _qty.Add(stock.StockQuantity / item.Quantity);
            }
            else
            {
                _qty.Add(p1.StockQuantity / item.Quantity);
            }
        }

        return _qty.Count > 0 ? _qty.Min() : 0;
    }

    public virtual async Task<int> GetReservedQty(Product product, string warehouseId)
    {
        var _qty = new List<int>();
        foreach (var item in product.BundleProducts)
        {
            var p1 = await _productService.GetProductById(item.ProductId);
            if (p1.UseMultipleWarehouses)
            {
                var stock = p1.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
                if (stock != null) _qty.Add(stock.ReservedQuantity / item.Quantity);
            }
        }

        return _qty.Count > 0 ? _qty.Min() : 0;
    }

    public virtual async Task<IList<ShipmentModel.ShipmentNote>> PrepareShipmentNotes(Shipment shipment)
    {
        //shipment notes
        var shipmentNoteModels = new List<ShipmentModel.ShipmentNote>();
        foreach (var shipmentNote in (await _shipmentService.GetShipmentNotes(shipment.Id))
                 .OrderByDescending(on => on.CreatedOnUtc))
        {
            var download = await _downloadService.GetDownloadById(shipmentNote.DownloadId);
            shipmentNoteModels.Add(new ShipmentModel.ShipmentNote {
                Id = shipmentNote.Id,
                ShipmentId = shipment.Id,
                DownloadId = string.IsNullOrEmpty(shipmentNote.DownloadId) ? "" : shipmentNote.DownloadId,
                DownloadGuid = download?.DownloadGuid ?? Guid.Empty,
                DisplayToCustomer = shipmentNote.DisplayToCustomer,
                Note = shipmentNote.Note,
                CreatedOn = _dateTimeService.ConvertToUserTime(shipmentNote.CreatedOnUtc, DateTimeKind.Utc),
                CreatedByCustomer = shipmentNote.CreatedByCustomer
            });
        }

        return shipmentNoteModels;
    }

    public virtual async Task InsertShipmentNote(Shipment shipment, string downloadId, bool displayToCustomer,
        string message)
    {
        var shipmentNote = new ShipmentNote {
            DisplayToCustomer = displayToCustomer,
            Note = message,
            DownloadId = downloadId,
            ShipmentId = shipment.Id
        };
        await _shipmentService.InsertShipmentNote(shipmentNote);

        //new shipment note notification
        // TODO
    }

    public virtual async Task DeleteShipmentNote(Shipment shipment, string id)
    {
        var shipmentNote = (await _shipmentService.GetShipmentNotes(shipment.Id)).FirstOrDefault(on => on.Id == id);
        if (shipmentNote == null)
            throw new ArgumentException("No shipment note found with the specified id");

        shipmentNote.ShipmentId = shipment.Id;
        await _shipmentService.DeleteShipmentNote(shipmentNote);

        //delete an old "attachment" file
        if (!string.IsNullOrEmpty(shipmentNote.DownloadId))
        {
            var attachment = await _downloadService.GetDownloadById(shipmentNote.DownloadId);
            if (attachment != null)
                await _downloadService.DeleteDownload(attachment);
        }
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
            model.StoreId,
            model.VendorId,
            model.WarehouseId,
            model.CountryId,
            model.StateProvinceId,
            model.City,
            model.TrackingNumber,
            model.LoadNotShipped,
            startDateValue,
            endDateValue,
            pageIndex - 1,
            pageSize);

        return (shipments.ToList(), shipments.TotalCount);
    }

    public virtual async Task<ShipmentListModel> PrepareShipmentListModel()
    {
        var model = new ShipmentListModel();
        //countries
        model.AvailableCountries.Add(new SelectListItem { Text = "*", Value = "" });
        foreach (var c in await _countryService.GetAllCountries(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id });
        //states
        model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "" });

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

        foreach (var orderItem in order.OrderItems)
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
        Shipment shipment = null;
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
            var warehouseId = "";
            if (product != null && (((product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock ||
                                      product.ManageInventoryMethodId ==
                                      ManageInventoryMethod.ManageStockByAttributes) &&
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

            if (shipment == null)
            {
                var trackingNumber = model.TrackingNumber;
                var adminComment = model.AdminComment;
                shipment = new Shipment {
                    OrderId = order.Id,
                    SeId = order.SeId,
                    TrackingNumber = trackingNumber,
                    TotalWeight = null,
                    ShippedDateUtc = null,
                    DeliveryDateUtc = null,
                    AdminComment = adminComment,
                    StoreId = order.StoreId
                };
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