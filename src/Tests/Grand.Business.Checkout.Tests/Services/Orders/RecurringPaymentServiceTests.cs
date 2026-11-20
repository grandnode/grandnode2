using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain;
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
    private IMediator _mediator;
    private IResourceLock _resourceLock;
    private RecurringPaymentService _service;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<RecurringPayment>();
        _productService = new Mock<IProductService>();
        _productService.Setup(x => x.GetProductById("standard", false)).ReturnsAsync(new Product { Id = "standard" });
        _productService.Setup(x => x.GetProductById("recurring", false))
            .ReturnsAsync(new Product { Id = "recurring", IsRecurring = true });
        _mediator = new Mock<IMediator>().Object;
        _resourceLock = new MongoResourceLockTest();
        _service = new RecurringPaymentService(_repository, _productService.Object, _mediator, _resourceLock);
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

    private async Task<RecurringPayment> Insert(RecurringPaymentStatus status, int cyclesPaid, int totalCycles = 12,
        string lastError = null)
    {
        var rp = new RecurringPayment {
            InitialOrderId = "o1", Status = status, CyclesPaid = cyclesPaid, TotalCycles = totalCycles,
            LastError = lastError
        };
        for (var i = 1; i <= cyclesPaid; i++)
            rp.Cycles.Add(new RecurringPaymentCycle {
                CycleNumber = i, OrderId = $"o{i}", ProviderReference = $"in_{i}"
            });
        await _service.InsertRecurringPayment(rp);
        return rp;
    }

    [TestMethod]
    public async Task ClaimCycle_FirstCycle_IsTheInitialOrderAndActivates()
    {
        var rp = await Insert(RecurringPaymentStatus.Pending, 0);
        var paid = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var next = paid.AddMonths(1);

        var claim = await _service.ClaimCycle(rp.Id, "sub_1", "in_1", 10, paid, next);

        Assert.IsNotNull(claim);
        Assert.AreEqual(1, claim.Cycle.CycleNumber);
        Assert.AreEqual("o1", claim.Cycle.OrderId);
        Assert.AreEqual("in_1", claim.Cycle.ProviderReference);
        Assert.AreEqual(10, claim.Cycle.Amount);
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Active, saved.Status);
        Assert.AreEqual(1, saved.CyclesPaid);
        Assert.AreEqual("sub_1", saved.ProviderReference);
        Assert.AreEqual(next, saved.NextPaymentDateUtc);
    }

    [TestMethod]
    public async Task ClaimCycle_LaterCycle_GetsANewOrderIdAndLeavesPastDue()
    {
        var rp = await Insert(RecurringPaymentStatus.PastDue, 1, lastError: "card declined");

        var claim = await _service.ClaimCycle(rp.Id, "sub_1", "in_2", 10, DateTime.UtcNow, null);

        Assert.AreEqual(2, claim.Cycle.CycleNumber);
        Assert.IsFalse(string.IsNullOrEmpty(claim.Cycle.OrderId));
        Assert.AreNotEqual("o1", claim.Cycle.OrderId);
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Active, saved.Status);
        Assert.AreEqual("card declined", saved.LastError);
    }

    [TestMethod]
    public async Task ClaimCycle_SameCycleReferenceAgain_ReturnsNull()
    {
        var rp = await Insert(RecurringPaymentStatus.Pending, 0);

        Assert.IsNotNull(await _service.ClaimCycle(rp.Id, "sub_1", "in_1", 10, DateTime.UtcNow, null));
        Assert.IsNull(await _service.ClaimCycle(rp.Id, "sub_1", "in_1", 10, DateTime.UtcNow, null));

        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(1, saved.Cycles.Count);
        Assert.AreEqual(1, saved.CyclesPaid);
    }

    [TestMethod]
    public async Task ClaimCycle_ConcurrentDeliveries_ClaimOnce()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1);
        var other = new RecurringPaymentService(_repository, _productService.Object, _mediator, _resourceLock);

        var claims = await Task.WhenAll(
            _service.ClaimCycle(rp.Id, "sub_1", "in_2", 10, DateTime.UtcNow, null),
            other.ClaimCycle(rp.Id, "sub_1", "in_2", 10, DateTime.UtcNow, null));

        Assert.AreEqual(1, claims.Count(c => c != null));
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(1, saved.Cycles.Count(c => c.ProviderReference == "in_2"));
    }

    [TestMethod]
    public async Task ClaimCycle_ReachingTotalCycles_Completes()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, 2);

        var claim = await _service.ClaimCycle(rp.Id, "sub_1", "in_2", 10, DateTime.UtcNow, null);

        Assert.IsTrue(claim.Completed);
        Assert.AreEqual(RecurringPaymentStatus.Completed, (await _service.GetRecurringPaymentById(rp.Id)).Status);
    }

    [TestMethod]
    public async Task ClaimCycle_TotalCyclesZero_NeverCompletes()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 11, 0);

        var claim = await _service.ClaimCycle(rp.Id, "sub_1", "in_12", 10, DateTime.UtcNow, null);

        Assert.IsFalse(claim.Completed);
        Assert.AreEqual(RecurringPaymentStatus.Active, (await _service.GetRecurringPaymentById(rp.Id)).Status);
    }

    [TestMethod]
    public async Task ClaimCycle_AfterCancelled_RecordsTheCycleAndKeepsTheStatus()
    {
        var rp = await Insert(RecurringPaymentStatus.Cancelled, 1);

        var claim = await _service.ClaimCycle(rp.Id, "sub_1", "in_2", 10, DateTime.UtcNow, null);

        Assert.IsNotNull(claim);
        Assert.AreEqual(2, claim.Cycle.CycleNumber);
        Assert.IsFalse(claim.Completed);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, (await _service.GetRecurringPaymentById(rp.Id)).Status);
    }

    [TestMethod]
    public async Task ClaimCycle_UnknownRecurringPayment_ReturnsNull()
    {
        Assert.IsNull(await _service.ClaimCycle(UniqueIdentifier.New, "sub_1", "in_1", 10, DateTime.UtcNow, null));
    }
}
