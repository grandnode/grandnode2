using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class RecurringPaymentServiceTests
{
    private Mock<IProductService> _productService;
    private IRepository<RecurringPayment> _repository;
    private RecurringPaymentService _service;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<RecurringPayment>();
        _productService = new Mock<IProductService>();
        _productService.Setup(x => x.GetProductById("standard", false)).ReturnsAsync(new Product { Id = "standard" });
        _productService.Setup(x => x.GetProductById("recurring", false))
            .ReturnsAsync(new Product { Id = "recurring", IsRecurring = true });
        _service = new RecurringPaymentService(_repository, _productService.Object, new Mock<IMediator>().Object);
    }

    [TestMethod]
    public async Task InsertRecurringPayment_CanBeReadByIdAndByInitialOrder()
    {
        var rp = new RecurringPayment {
            InitialOrderId = "o1", CycleLength = 1, CyclePeriodId = RecurringCyclePeriod.Months, TotalCycles = 12
        };
        await _service.InsertRecurringPayment(rp);

        Assert.AreEqual("o1", (await _service.GetRecurringPaymentById(rp.Id)).InitialOrderId);
        Assert.AreEqual(rp.Id, (await _service.GetRecurringPaymentByInitialOrderId("o1")).Id);
    }

    [TestMethod]
    public async Task GetRecurringPaymentByInitialOrderId_OtherOrder_ReturnsNull()
    {
        await _service.InsertRecurringPayment(new RecurringPayment { InitialOrderId = "o1" });
        Assert.IsNull(await _service.GetRecurringPaymentByInitialOrderId("o2"));
    }

    [TestMethod]
    public async Task UpdateRecurringPayment_SavesStatusAndCycles()
    {
        var rp = new RecurringPayment { InitialOrderId = "o1" };
        await _service.InsertRecurringPayment(rp);
        rp.Status = RecurringPaymentStatus.Active;
        rp.Cycles.Add(new RecurringPaymentCycle { CycleNumber = 1, OrderId = "o1", ProviderReference = "in_1" });
        await _service.UpdateRecurringPayment(rp);

        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Active, saved.Status);
        Assert.AreEqual("in_1", saved.Cycles.Single().ProviderReference);
    }

    [TestMethod]
    public async Task HasRecurringProducts_CartWithRecurringProduct_ReturnsTrue()
    {
        Assert.IsTrue(await _service.HasRecurringProducts([
            new ShoppingCartItem { ProductId = "standard" }, new ShoppingCartItem { ProductId = "recurring" }
        ]));
    }

    [TestMethod]
    public async Task HasRecurringProducts_StandardOrMissingProducts_ReturnsFalse()
    {
        Assert.IsFalse(await _service.HasRecurringProducts([
            new ShoppingCartItem { ProductId = "standard" }, new ShoppingCartItem { ProductId = "deleted" }
        ]));
    }

    [TestMethod]
    public async Task InsertRecurringPayment_Null_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _service.InsertRecurringPayment(null));
    }
}
