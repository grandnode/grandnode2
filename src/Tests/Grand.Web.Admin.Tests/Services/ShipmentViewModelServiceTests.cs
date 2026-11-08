using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using Grand.Web.Common.Localization;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class ShipmentViewModelServiceTests
{
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<IShipmentService> _shipmentServiceMock;
    private Mock<IWarehouseService> _warehouseServiceMock;
    private Mock<IMeasureService> _measureServiceMock;
    private Mock<IAdminDataScope<Shipment>> _scopeMock;
    private ShipmentViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _productServiceMock = new Mock<IProductService>();
        _shipmentServiceMock = new Mock<IShipmentService>();
        _warehouseServiceMock = new Mock<IWarehouseService>();
        _measureServiceMock = new Mock<IMeasureService>();
        _scopeMock = new Mock<IAdminDataScope<Shipment>>();

        _measureServiceMock.Setup(m => m.GetMeasureWeightById(It.IsAny<string>())).ReturnsAsync((MeasureWeight)null);
        _measureServiceMock.Setup(m => m.GetMeasureDimensionById(It.IsAny<string>()))
            .ReturnsAsync((MeasureDimension)null);

        _warehouseServiceMock.Setup(w => w.GetWarehouseById(It.IsAny<string>())).ReturnsAsync((Warehouse)null);

        // Default: Admin's Global scope - identity passthrough, no default vendor.
        _scopeMock.Setup(s => s.FilterOrderItems(It.IsAny<IEnumerable<OrderItem>>()))
            .Returns((IEnumerable<OrderItem> items) => items);
        _scopeMock.Setup(s => s.DefaultVendorId).Returns((string)null);

        _service = new ShipmentViewModelService(
            _orderServiceMock.Object,
            new Mock<IContextAccessor>().Object,
            _productServiceMock.Object,
            _shipmentServiceMock.Object,
            _warehouseServiceMock.Object,
            _measureServiceMock.Object,
            new Mock<IDateTimeService>().Object,
            new Mock<ICountryService>().Object,
            _translationServiceMock.Object,
            new Mock<IDownloadService>().Object,
            new Mock<IShippingService>().Object,
            new Mock<IStockQuantityService>().Object,
            new MeasureSettings(),
            new ShippingSettings(),
            new ShippingProviderSettings(),
            _scopeMock.Object,
            _productUnitServiceMock.Object);
    }

    private readonly Mock<IProductUnitService> _productUnitServiceMock = new();
    private readonly Mock<IRequestTranslationService> _translationServiceMock = new();

    private (Order, OrderItem) TrackedOrder(InventoryTrackingMode mode, bool multipleWarehouses = false)
    {
        var order = new Order { Id = "order1", SeId = "se1", StoreId = "store1" };
        var orderItem = new OrderItem
            { Id = "oi-A", ProductId = "p-A", IsShipEnabled = true, OpenQty = 2, Quantity = 2, WarehouseId = "w1" };
        var product = new Product {
            Id = "p-A", Name = "p", IsShipEnabled = true, ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            InventoryTrackingModeId = mode, WarehouseId = multipleWarehouses ? "" : "w1",
            UseMultipleWarehouses = multipleWarehouses,
            ProductWarehouseInventory = {
                new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = 2 },
                new ProductWarehouseInventory { WarehouseId = "w2", StockQuantity = 2 }
            }
        };
        _productServiceMock.Setup(p => p.GetProductById("p-A", It.IsAny<bool>())).ReturnsAsync(product);
        _productServiceMock.Setup(p => p.GetProductByIdIncludeArch("p-A")).ReturnsAsync(product);
        order.OrderItems.Add(orderItem);
        return (order, orderItem);
    }

    [TestMethod]
    public async Task PrepareShipment_SerialNumbersText_ParsedOntoShipmentItem()
    {
        var (order, orderItem) = TrackedOrder(InventoryTrackingMode.SerialNumber);
        var model = new AddShipmentModel {
            OrderId = order.Id,
            Items = [new() { OrderItemId = "oi-A", QuantityToAdd = 2, SerialNumbersText = " A1\nA2 " }]
        };

        var (shipment, _) = await _service.PrepareShipment(order, [orderItem], model);

        CollectionAssert.AreEqual(new[] { "A1", "A2" }, shipment.ShipmentItems.Single().SerialNumbers.ToArray());
    }

    [TestMethod]
    public async Task PrepareShipment_Batches_OnlyOfTheChosenWarehouse()
    {
        var (order, orderItem) = TrackedOrder(InventoryTrackingMode.Batch, true);
        var model = new AddShipmentModel {
            OrderId = order.Id,
            Items = [
                new() {
                    OrderItemId = "oi-A", QuantityToAdd = 2, WarehouseId = "w2",
                    Batches = [
                        new() { WarehouseId = "w1", Number = "L1", Quantity = 2 },
                        new() { WarehouseId = "w2", Number = "L2", Quantity = 2 },
                        new() { WarehouseId = "w2", Number = "L3", Quantity = 0 }
                    ]
                }
            ]
        };

        var (shipment, _) = await _service.PrepareShipment(order, [orderItem], model);

        var batch = shipment.ShipmentItems.Single().Batches.Single();
        Assert.AreEqual("L2", batch.Number);
        Assert.AreEqual(2, batch.Quantity);
    }

    [TestMethod]
    public async Task PrepareShipmentModel_BatchItem_ProposalPerWarehouse()
    {
        var (order, _) = TrackedOrder(InventoryTrackingMode.Batch, true);
        _warehouseServiceMock.Setup(w => w.GetWarehouseById(It.IsAny<string>()))
            .ReturnsAsync((string id) => new Warehouse { Id = id, Name = id });
        _productUnitServiceMock.Setup(x => x.ProposeBatches(It.IsAny<Product>(),
                It.IsAny<IList<Grand.Domain.Common.CustomAttribute>>(), It.IsAny<string>(), 2, It.IsAny<DateTime>()))
            .ReturnsAsync((Product _, IList<Grand.Domain.Common.CustomAttribute> _, string warehouseId, int _,
                DateTime _) => [new BatchAllocation("L-" + warehouseId, null, null, 2, 2)]);

        var model = await _service.PrepareShipmentModel(order);

        var item = model.Items.Single();
        Assert.AreEqual(InventoryTrackingMode.Batch, item.TrackingMode);
        CollectionAssert.AreEquivalent(new[] { "L-w1", "L-w2" }, item.BatchProposals.Select(x => x.Number).ToArray());
        Assert.AreEqual("w2", item.BatchProposals.Single(x => x.Number == "L-w2").WarehouseId);
    }

    [TestMethod]
    public async Task PrepareShipmentUnits_FormatsErrorsWithProductName()
    {
        _productUnitServiceMock.Setup(x => x.PrepareShipmentUnits(It.IsAny<Shipment>()))
            .ReturnsAsync([new ShipmentUnitError("si1", "p", "key")]);
        _translationServiceMock.Setup(t => t.GetResource("key")).Returns("message");

        var errors = await _service.PrepareShipmentUnits(new Shipment());

        CollectionAssert.AreEqual(new[] { "p: message" }, errors.ToArray());
    }

    [TestMethod]
    public async Task PrepareShipmentModel_VendorScopeFiltersToOwnItems()
    {
        // Arrange
        var order = new Order { Id = "order1" };
        order.OrderItems.Add(new OrderItem { Id = "oi-A", ProductId = "p-A", VendorId = "vendor-A" });
        order.OrderItems.Add(new OrderItem { Id = "oi-B", ProductId = "p-B", VendorId = "vendor-B" });
        _orderServiceMock.Setup(o => o.GetOrderById(order.Id)).ReturnsAsync(order);

        _scopeMock.Setup(s => s.FilterOrderItems(order.OrderItems))
            .Returns(order.OrderItems.Where(i => i.VendorId == "vendor-A"));

        var productA = new Product { Id = "p-A", Name = "Product A" };
        _productServiceMock.Setup(p => p.GetProductByIdIncludeArch("p-A")).ReturnsAsync(productA);

        var shipment = new Shipment { Id = "shipment1", OrderId = order.Id };
        shipment.ShipmentItems.Add(new ShipmentItem { Id = "si-A", OrderItemId = "oi-A", Quantity = 1 });
        shipment.ShipmentItems.Add(new ShipmentItem { Id = "si-B", OrderItemId = "oi-B", Quantity = 1 });

        // Act
        var model = await _service.PrepareShipmentModel(shipment, prepareProducts: true);

        // Assert
        Assert.AreEqual(1, model.Items.Count);
        Assert.AreEqual("oi-A", model.Items[0].OrderItemId);
        Assert.AreEqual("p-A", model.Items[0].ProductId);
    }

    [TestMethod]
    public async Task PrepareShipmentModel_GlobalScopeDoesNotFilterItems()
    {
        // Arrange
        var order = new Order { Id = "order1" };
        order.OrderItems.Add(new OrderItem { Id = "oi-A", ProductId = "p-A", VendorId = "vendor-A" });
        order.OrderItems.Add(new OrderItem { Id = "oi-B", ProductId = "p-B", VendorId = "vendor-B" });
        _orderServiceMock.Setup(o => o.GetOrderById(order.Id)).ReturnsAsync(order);

        // Default Setup() scope: identity passthrough (no filtering).

        var productA = new Product { Id = "p-A", Name = "Product A" };
        var productB = new Product { Id = "p-B", Name = "Product B" };
        _productServiceMock.Setup(p => p.GetProductByIdIncludeArch("p-A")).ReturnsAsync(productA);
        _productServiceMock.Setup(p => p.GetProductByIdIncludeArch("p-B")).ReturnsAsync(productB);

        var shipment = new Shipment { Id = "shipment1", OrderId = order.Id };
        shipment.ShipmentItems.Add(new ShipmentItem { Id = "si-A", OrderItemId = "oi-A", Quantity = 1 });
        shipment.ShipmentItems.Add(new ShipmentItem { Id = "si-B", OrderItemId = "oi-B", Quantity = 1 });

        // Act
        var model = await _service.PrepareShipmentModel(shipment, prepareProducts: true);

        // Assert
        Assert.AreEqual(2, model.Items.Count);
        Assert.IsTrue(model.Items.Any(i => i.OrderItemId == "oi-A"));
        Assert.IsTrue(model.Items.Any(i => i.OrderItemId == "oi-B"));
    }

    [TestMethod]
    public async Task PrepareShipment_SetsVendorIdFromScope()
    {
        // Arrange
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor-A");

        var order = new Order { Id = "order1", SeId = "se1", StoreId = "store1" };
        var orderItem = new OrderItem {
            Id = "oi-A",
            ProductId = "p-A",
            IsShipEnabled = true,
            OpenQty = 1,
            Quantity = 1
        };

        var product = new Product { Id = "p-A", IsShipEnabled = true };
        _productServiceMock.Setup(p => p.GetProductById("p-A")).ReturnsAsync(product);

        var model = new AddShipmentModel {
            OrderId = order.Id,
            Items = new List<AddShipmentModel.ShipmentItemModel> {
                new() { OrderItemId = "oi-A", QuantityToAdd = 1 }
            }
        };

        // Act
        var (shipment, _) = await _service.PrepareShipment(order, new[] { orderItem }, model);

        // Assert
        Assert.IsNotNull(shipment);
        Assert.AreEqual("vendor-A", shipment.VendorId);
    }

    [TestMethod]
    public async Task PrepareShipment_NullDefaultVendorId_LeavesVendorIdNull()
    {
        // Arrange
        _scopeMock.Setup(s => s.DefaultVendorId).Returns((string)null);

        var order = new Order { Id = "order1", SeId = "se1", StoreId = "store1" };
        var orderItem = new OrderItem {
            Id = "oi-A",
            ProductId = "p-A",
            IsShipEnabled = true,
            OpenQty = 1,
            Quantity = 1
        };

        var product = new Product { Id = "p-A", IsShipEnabled = true };
        _productServiceMock.Setup(p => p.GetProductById("p-A")).ReturnsAsync(product);

        var model = new AddShipmentModel {
            OrderId = order.Id,
            Items = new List<AddShipmentModel.ShipmentItemModel> {
                new() { OrderItemId = "oi-A", QuantityToAdd = 1 }
            }
        };

        // Act
        var (shipment, _) = await _service.PrepareShipment(order, new[] { orderItem }, model);

        // Assert
        Assert.IsNotNull(shipment);
        Assert.IsNull(shipment.VendorId);
    }
}
