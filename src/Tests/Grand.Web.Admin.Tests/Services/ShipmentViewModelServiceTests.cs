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
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

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
            new Mock<ITranslationService>().Object,
            new Mock<IDownloadService>().Object,
            new Mock<IShippingService>().Object,
            new Mock<IStockQuantityService>().Object,
            new MeasureSettings(),
            new ShippingSettings(),
            new ShippingProviderSettings(),
            _scopeMock.Object);
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
