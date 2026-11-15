using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Module.Api.Commands.Handlers.Inventory;
using Grand.Module.Api.Commands.Models.Inventory;
using Grand.Module.Api.DTOs.Inventory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Module.Api.Tests.Commands.Inventory;

[TestClass]
public class ApplyStockCommandHandlerTests
{
    private Mock<IStockMovementService> _stockMovementServiceMock;
    private Mock<IRepository<Product>> _productRepositoryMock;
    private Mock<IRepository<Warehouse>> _warehouseRepositoryMock;
    private ApplyStockCommandHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _stockMovementServiceMock = new Mock<IStockMovementService>();
        _stockMovementServiceMock.Setup(x => x.Apply(It.IsAny<string>(), It.IsAny<IReadOnlyList<StockChange>>()))
            .ReturnsAsync([]);
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _warehouseRepositoryMock = new Mock<IRepository<Warehouse>>();
        _handler = new ApplyStockCommandHandler(_stockMovementServiceMock.Object, _productRepositoryMock.Object,
            _warehouseRepositoryMock.Object);
    }

    [TestMethod]
    public async Task Handle_MissingKey_Rejected()
    {
        var result = await _handler.Handle(new ApplyStockCommand {
            Lines = [new StockApplyLineDto { ProductId = "p1", Mode = "Set", Quantity = 3 }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(StockErrors.KeyRequired, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_InvalidMode_Rejected()
    {
        var result = await _handler.Handle(new ApplyStockCommand {
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", Mode = "Adjust", Quantity = 3 }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(StockErrors.ModeInvalid, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_ProductIdAndSku_Rejected()
    {
        var result = await _handler.Handle(new ApplyStockCommand {
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", Sku = "s1", Mode = "Set", Quantity = 3 }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(StockErrors.ProductTargetBoth, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_TrackedProductSet_Rejected()
    {
        _productRepositoryMock.Setup(x => x.GetByIdAsync("p1")).ReturnsAsync(new Product {
            Id = "p1",
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            InventoryTrackingModeId = InventoryTrackingMode.SerialNumber
        });

        var result = await _handler.Handle(new ApplyStockCommand {
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", Mode = "Set", Quantity = 3 }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(StockErrors.TrackedSetNotSupported, result[0].Error);
        _stockMovementServiceMock.Verify(x => x.Apply(It.IsAny<string>(), It.IsAny<IReadOnlyList<StockChange>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_SerialCountMismatch_Rejected()
    {
        _productRepositoryMock.Setup(x => x.GetByIdAsync("p1")).ReturnsAsync(new Product {
            Id = "p1",
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            InventoryTrackingModeId = InventoryTrackingMode.SerialNumber
        });

        var result = await _handler.Handle(new ApplyStockCommand {
            Lines = [new StockApplyLineDto {
                Key = "k1", ProductId = "p1", Mode = "Change", Quantity = 2, SerialNumbers = ["s1"]
            }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(UnitErrors.SerialCountMismatch, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_BatchPositiveWithoutBatch_Rejected()
    {
        _productRepositoryMock.Setup(x => x.GetByIdAsync("p1")).ReturnsAsync(new Product {
            Id = "p1",
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            InventoryTrackingModeId = InventoryTrackingMode.Batch
        });

        var result = await _handler.Handle(new ApplyStockCommand {
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", Mode = "Change", Quantity = 3 }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(UnitErrors.BatchRequired, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_WarehouseNotOnProduct_Rejected()
    {
        _productRepositoryMock.Setup(x => x.GetByIdAsync("p1")).ReturnsAsync(new Product {
            Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1"
        });

        var result = await _handler.Handle(new ApplyStockCommand {
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", WarehouseId = "w2", Mode = "Change", Quantity = 1 }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(StockErrors.WarehouseNotOnProduct, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_ProductByIdSet_Applied_BooksThroughLedgerAsApi()
    {
        //the stored product after the change: the handler reads it again for the bucket's stock
        _productRepositoryMock.Setup(x => x.GetByIdAsync("p1")).ReturnsAsync(new Product {
            Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 10
        });

        var result = await _handler.Handle(new ApplyStockCommand {
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", Mode = "Set", Quantity = 10 }],
            CustomerId = "api-user"
        }, default);

        Assert.AreEqual("Applied", result[0].Status);
        Assert.AreEqual(10, result[0].Stock);
        _stockMovementServiceMock.Verify(x => x.Apply("p1", It.Is<IReadOnlyList<StockChange>>(c =>
            c.Count == 1 && c[0].SetTo && c[0].Quantity == 10 && c[0].MovementType == InventoryMovementType.Api &&
            c[0].IdempotencyKey == "k1" && c[0].Source.CustomerId == "api-user")), Times.Once);
    }

    [TestMethod]
    public async Task Handle_AlreadyApplied_MappedToAlreadyAppliedStatus()
    {
        _productRepositoryMock.Setup(x => x.GetByIdAsync("p1")).ReturnsAsync(new Product {
            Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 10
        });
        _stockMovementServiceMock.Setup(x => x.Apply("p1", It.IsAny<IReadOnlyList<StockChange>>()))
            .ReturnsAsync([StockErrors.AlreadyApplied]);

        var result = await _handler.Handle(new ApplyStockCommand {
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", Mode = "Set", Quantity = 10 }]
        }, default);

        Assert.AreEqual("AlreadyApplied", result[0].Status);
        Assert.AreEqual(10, result[0].Stock);
    }

    [TestMethod]
    public async Task Handle_MixedLines_RejectedLineDoesNotStopOthers()
    {
        _productRepositoryMock.Setup(x => x.GetByIdAsync("p1")).ReturnsAsync(new Product {
            Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 10
        });

        var result = await _handler.Handle(new ApplyStockCommand {
            Lines = [
                new StockApplyLineDto { Key = "k1", ProductId = "p1", Mode = "Set", Quantity = 10 },
                new StockApplyLineDto { ProductId = "p1", Mode = "Set", Quantity = 20 }
            ]
        }, default);

        Assert.AreEqual("Applied", result[0].Status);
        Assert.AreEqual("Rejected", result[1].Status);
        Assert.AreEqual(StockErrors.KeyRequired, result[1].Error);
        _stockMovementServiceMock.Verify(x => x.Apply("p1", It.IsAny<IReadOnlyList<StockChange>>()), Times.Once);
    }
}
