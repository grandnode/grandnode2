using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Module.Api.Commands.Handlers.Catalog;
using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Commands.Catalog;

[TestClass]
public class UpdateProductStockCommandHandlerTests
{
    private UpdateProductStockCommandHandler _handler;
    private Mock<IInventoryManageService> _inventoryManageServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<IStockMovementService> _stockMovementServiceMock;

    [TestInitialize]
    public void Init()
    {
        _productServiceMock = new Mock<IProductService>();
        _inventoryManageServiceMock = new Mock<IInventoryManageService>();
        _stockMovementServiceMock = new Mock<IStockMovementService>();
        _stockMovementServiceMock.Setup(x => x.Apply(It.IsAny<string>(), It.IsAny<IReadOnlyList<StockChange>>()))
            .ReturnsAsync([]);
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { Id = "api-user" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        _handler = new UpdateProductStockCommandHandler(_productServiceMock.Object, _inventoryManageServiceMock.Object,
            _stockMovementServiceMock.Object, contextAccessor.Object);
    }

    private Product Stored(Product product)
    {
        _productServiceMock.Setup(x => x.GetProductById(product.Id, false)).ReturnsAsync(product);
        return product;
    }

    [TestMethod]
    public async Task Handle_TrackedProduct_Refused_NoMovement()
    {
        var product = Stored(new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            InventoryTrackingModeId = InventoryTrackingMode.SerialNumber
        });

        var result = await _handler.Handle(
            new UpdateProductStockCommand { Product = new ProductDto { Id = product.Id }, Stock = 12 }, default);

        Assert.IsFalse(result);
        _stockMovementServiceMock.Verify(x => x.Apply(It.IsAny<string>(), It.IsAny<IReadOnlyList<StockChange>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_SetsStockThroughLedgerAsApi()
    {
        var product = Stored(new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStock });

        await _handler.Handle(new UpdateProductStockCommand { Product = new ProductDto { Id = product.Id }, Stock = 12 },
            default);

        _stockMovementServiceMock.Verify(x => x.Apply(product.Id, It.Is<IReadOnlyList<StockChange>>(c =>
            c.Count == 1 && c[0].SetTo && c[0].Quantity == 12 && c[0].MovementType == InventoryMovementType.Api &&
            c[0].Source.CustomerId == "api-user")), Times.Once);
        _inventoryManageServiceMock.Verify(x => x.UpdateStockProduct(It.IsAny<Product>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_MultiWarehouseWithoutWarehouse_Throws()
    {
        var product = Stored(new Product
            { ManageInventoryMethodId = ManageInventoryMethod.ManageStock, UseMultipleWarehouses = true });
        _stockMovementServiceMock.Setup(x => x.Apply(product.Id, It.IsAny<IReadOnlyList<StockChange>>()))
            .ReturnsAsync([StockErrors.WarehouseRequired]);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _handler.Handle(
            new UpdateProductStockCommand { Product = new ProductDto { Id = product.Id }, Stock = 3 }, default));
    }

    [TestMethod]
    public async Task Handle_WarehouseForSingleWarehouseProduct_Throws()
    {
        var product = Stored(new Product { ManageInventoryMethodId = ManageInventoryMethod.ManageStock });

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _handler.Handle(
            new UpdateProductStockCommand { Product = new ProductDto { Id = product.Id }, WarehouseId = "w1" },
            default));
        _stockMovementServiceMock.Verify(x => x.Apply(It.IsAny<string>(), It.IsAny<IReadOnlyList<StockChange>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_ProductNotTrackingStock_WritesTheCounterAsBefore()
    {
        var product = Stored(new Product { ManageInventoryMethodId = ManageInventoryMethod.DontManageStock });

        await _handler.Handle(new UpdateProductStockCommand { Product = new ProductDto { Id = product.Id }, Stock = 4 },
            default);

        _inventoryManageServiceMock.Verify(
            x => x.UpdateStockProduct(It.Is<Product>(p => p.StockQuantity == 4), false), Times.Once);
        _stockMovementServiceMock.Verify(x => x.Apply(It.IsAny<string>(), It.IsAny<IReadOnlyList<StockChange>>()),
            Times.Never);
    }
}
