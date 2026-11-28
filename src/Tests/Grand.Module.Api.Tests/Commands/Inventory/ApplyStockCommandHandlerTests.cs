using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
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
    //real repositories on the test database: the lookups must translate to queries, and the mock counts them
    private Mock<MongoDBRepositoryTest<Product>> _productRepositoryMock;
    private MongoDBRepositoryTest<Warehouse> _warehouseRepository;
    private ApplyStockCommandHandler _handler;

    public ApplyStockCommandHandlerTests()
    {
        _stockMovementServiceMock = new Mock<IStockMovementService>();
        _stockMovementServiceMock.Setup(x => x.Apply(It.IsAny<string>(), It.IsAny<IReadOnlyList<StockChange>>()))
            .ReturnsAsync([]);
        _productRepositoryMock = new Mock<MongoDBRepositoryTest<Product>> { CallBase = true };
        _warehouseRepository = new MongoDBRepositoryTest<Warehouse>();
        _handler = new ApplyStockCommandHandler(_stockMovementServiceMock.Object, _productRepositoryMock.Object,
            _warehouseRepository);
    }

    [TestMethod]
    public async Task Handle_MissingKey_Rejected()
    {
        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { ProductId = "p1", Mode = "Set", Quantity = 3 }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(StockErrors.KeyRequired, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_InvalidMode_Rejected()
    {
        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", Mode = "Adjust", Quantity = 3 }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(StockErrors.ModeInvalid, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_ProductIdAndSku_Rejected()
    {
        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", Sku = "s1", Mode = "Set", Quantity = 3 }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(StockErrors.ProductTargetBoth, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_TrackedProductSet_Rejected()
    {
        await _productRepositoryMock.Object.InsertAsync(new Product {
            Id = "p1",
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            InventoryTrackingModeId = InventoryTrackingMode.SerialNumber
        });

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
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
        await _productRepositoryMock.Object.InsertAsync(new Product {
            Id = "p1",
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            InventoryTrackingModeId = InventoryTrackingMode.SerialNumber
        });

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
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
        await _productRepositoryMock.Object.InsertAsync(new Product {
            Id = "p1",
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            InventoryTrackingModeId = InventoryTrackingMode.Batch
        });

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", Mode = "Change", Quantity = 3 }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(UnitErrors.BatchRequired, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_WarehouseNotOnProduct_Rejected()
    {
        await _productRepositoryMock.Object.InsertAsync(new Product {
            Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, WarehouseId = "w1"
        });

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", WarehouseId = "w2", Mode = "Change", Quantity = 1 }]
        }, default);

        Assert.AreEqual("Rejected", result[0].Status);
        Assert.AreEqual(StockErrors.WarehouseNotOnProduct, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_ProductByIdSet_Applied_BooksThroughLedgerAsApi()
    {
        //the stored product after the change: the handler reads it again for the bucket's stock
        await _productRepositoryMock.Object.InsertAsync(new Product {
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
        await _productRepositoryMock.Object.InsertAsync(new Product {
            Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 10
        });
        _stockMovementServiceMock.Setup(x => x.Apply("p1", It.IsAny<IReadOnlyList<StockChange>>()))
            .ReturnsAsync([StockErrors.AlreadyApplied]);

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", Mode = "Set", Quantity = 10 }]
        }, default);

        Assert.AreEqual("AlreadyApplied", result[0].Status);
        Assert.AreEqual(10, result[0].Stock);
    }

    [TestMethod]
    public async Task Handle_MixedLines_RejectedLineDoesNotStopOthers()
    {
        await _productRepositoryMock.Object.InsertAsync(new Product {
            Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 10
        });

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
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

    [TestMethod]
    public async Task Handle_SkuOfProduct_AppliedToThatProduct()
    {
        await _productRepositoryMock.Object.InsertAsync(new Product {
            Id = "p1", Sku = "S-1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock
        });

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { Key = "k1", Sku = " S-1 ", Mode = "Change", Quantity = 2 }]
        }, default);

        Assert.AreEqual("Applied", result[0].Status);
        _stockMovementServiceMock.Verify(x => x.Apply("p1", It.Is<IReadOnlyList<StockChange>>(c =>
            c[0].Quantity == 2 && !c[0].SetTo && c[0].Source.Reference == "S-1")), Times.Once);
    }

    [TestMethod]
    public async Task Handle_SkuOfCombination_AppliedWithItsAttributes()
    {
        await _productRepositoryMock.Object.InsertAsync(new Product {
            Id = "p1",
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes,
            ProductAttributeCombinations = {
                new ProductAttributeCombination {
                    Id = "c1", Sku = "C-1", Attributes = [new CustomAttribute { Key = "a1", Value = "v1" }]
                }
            }
        });

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { Key = "k1", Sku = "C-1", Mode = "Set", Quantity = 4 }]
        }, default);

        Assert.AreEqual("Applied", result[0].Status);
        _stockMovementServiceMock.Verify(x => x.Apply("p1", It.Is<IReadOnlyList<StockChange>>(c =>
            c[0].Attributes.Count == 1 && c[0].Attributes[0].Key == "a1" && c[0].Attributes[0].Value == "v1")),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_SkuOnTwoProducts_RejectedAsAmbiguous()
    {
        await _productRepositoryMock.Object.InsertAsync(new Product
            { Id = "p1", Sku = "S-1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock });
        await _productRepositoryMock.Object.InsertAsync(new Product {
            Id = "p2",
            ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes,
            ProductAttributeCombinations = { new ProductAttributeCombination { Id = "c1", Sku = "S-1" } }
        });

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { Key = "k1", Sku = "S-1", Mode = "Change", Quantity = 1 }]
        }, default);

        Assert.AreEqual(StockErrors.SkuAmbiguous, result[0].Error);
        _stockMovementServiceMock.Verify(x => x.Apply(It.IsAny<string>(), It.IsAny<IReadOnlyList<StockChange>>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Handle_UnknownSku_Rejected()
    {
        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { Key = "k1", Sku = "NONE", Mode = "Change", Quantity = 1 }]
        }, default);

        Assert.AreEqual(StockErrors.SkuNotFound, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_WarehouseCode_AppliedToThatWarehouse()
    {
        await _warehouseRepository.InsertAsync(new Warehouse { Id = "w1", Code = "WH-1" });
        await _productRepositoryMock.Object.InsertAsync(new Product {
            Id = "p1",
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            UseMultipleWarehouses = true,
            ProductWarehouseInventory = { new ProductWarehouseInventory { WarehouseId = "w1", StockQuantity = 7 } }
        });

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", WarehouseCode = "WH-1", Mode = "Set", Quantity = 7 }]
        }, default);

        Assert.AreEqual("Applied", result[0].Status);
        Assert.AreEqual(7, result[0].Stock);
        _stockMovementServiceMock.Verify(x => x.Apply("p1", It.Is<IReadOnlyList<StockChange>>(c =>
            c[0].WarehouseId == "w1")), Times.Once);
    }

    [TestMethod]
    public async Task Handle_UnknownWarehouseCode_Rejected()
    {
        await _productRepositoryMock.Object.InsertAsync(new Product
            { Id = "p1", ManageInventoryMethodId = ManageInventoryMethod.ManageStock });

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = [new StockApplyLineDto { Key = "k1", ProductId = "p1", WarehouseCode = "NONE", Mode = "Set", Quantity = 1 }]
        }, default);

        Assert.AreEqual(StockErrors.WarehouseCodeNotFound, result[0].Error);
    }

    [TestMethod]
    public async Task Handle_ManyLines_ResolvesSkusInOneQueryEach()
    {
        for (var i = 0; i < 50; i++)
            await _productRepositoryMock.Object.InsertAsync(new Product
                { Id = $"p{i}", Sku = $"S-{i}", ManageInventoryMethodId = ManageInventoryMethod.ManageStock });

        var result = await _handler.Handle(new ApplyStockCommand { CustomerId = "c1",
            Lines = Enumerable.Range(0, 50).Select(i => new StockApplyLineDto
                { Key = $"k{i}", Sku = $"S-{i}", Mode = "Change", Quantity = 1 }).ToList()
        }, default);

        Assert.IsTrue(result.All(x => x.Status == "Applied"));
        //one query by product SKU and one by combination SKU, whatever the number of lines
        _productRepositoryMock.Verify(x => x.ToListAsync(It.IsAny<IQueryable<Product>>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
