using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Checkout;
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
        string lastError = null, string providerReference = null)
    {
        var rp = new RecurringPayment {
            InitialOrderId = "o1", Status = status, CyclesPaid = cyclesPaid, TotalCycles = totalCycles,
            LastError = lastError, ProviderReference = providerReference
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
    public async Task ClaimCycle_ReachingTotalCyclesAfterCancelled_KeepsTheStatusAndIsNotCompleted()
    {
        var rp = await Insert(RecurringPaymentStatus.Cancelled, 1, 2);

        var claim = await _service.ClaimCycle(rp.Id, "sub_1", "in_2", 10, DateTime.UtcNow, null);

        Assert.IsNotNull(claim);
        Assert.IsFalse(claim.Completed);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, (await _service.GetRecurringPaymentById(rp.Id)).Status);
    }

    [TestMethod]
    public async Task ClaimCycle_WithoutCycleReference_Throws()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            _service.ClaimCycle(rp.Id, "sub_1", "", 10, DateTime.UtcNow, null));
    }

    [TestMethod]
    public async Task RecordFailure_WithoutCycleReference_Throws()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _service.RecordFailure(rp.Id, "", "declined"));
    }

    [TestMethod]
    public async Task ClaimCycle_UnknownRecurringPayment_ReturnsNull()
    {
        Assert.IsNull(await _service.ClaimCycle(UniqueIdentifier.New, "sub_1", "in_1", 10, DateTime.UtcNow, null));
    }

    [TestMethod]
    public async Task RecordFailure_Active_EntersPastDueOnlyOnce()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1);

        Assert.IsTrue(await _service.RecordFailure(rp.Id, "in_2", "declined"));
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.PastDue, saved.Status);
        Assert.AreEqual("declined", saved.LastError);
        Assert.IsNotNull(saved.LastErrorDateUtc);

        Assert.IsFalse(await _service.RecordFailure(rp.Id, "in_2", "expired"));
        saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.PastDue, saved.Status);
        Assert.AreEqual("expired", saved.LastError);
    }

    [TestMethod]
    public async Task RecordFailure_ForACyclePaidAlready_IsIgnored()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 2);

        Assert.IsFalse(await _service.RecordFailure(rp.Id, "in_2", "late"));

        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Active, saved.Status);
        Assert.IsNull(saved.LastError);
    }

    [TestMethod]
    public async Task RecordFailure_Cancelled_ChangesNothing()
    {
        var rp = await Insert(RecurringPaymentStatus.Cancelled, 1);

        Assert.IsFalse(await _service.RecordFailure(rp.Id, "in_2", "declined"));

        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, saved.Status);
        Assert.IsNull(saved.LastError);
    }

    [TestMethod]
    public async Task RecordFailure_UnknownRecurringPayment_ReturnsFalse()
    {
        Assert.IsFalse(await _service.RecordFailure(UniqueIdentifier.New, "in_2", "declined"));
    }

    [TestMethod]
    public async Task RecordEnded_Active_CancelsByTheProvider()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1);
        rp.PaymentMethodSystemName = "Payments.Test";
        await _repository.UpdateAsync(rp);

        Assert.IsTrue(await _service.RecordEnded(rp.Id));

        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, saved.Status);
        Assert.AreEqual("Payments.Test", saved.CancelledBy);
        Assert.IsNotNull(saved.CancelledOnUtc);
    }

    [TestMethod]
    public async Task RecordEnded_CompletedOrCancelled_ChangesNothing()
    {
        var completed = await Insert(RecurringPaymentStatus.Completed, 12);
        Assert.IsFalse(await _service.RecordEnded(completed.Id));
        Assert.AreEqual(RecurringPaymentStatus.Completed,
            (await _service.GetRecurringPaymentById(completed.Id)).Status);

        var active = await Insert(RecurringPaymentStatus.Active, 1);
        Assert.IsTrue(await _service.RecordEnded(active.Id));
        Assert.IsFalse(await _service.RecordEnded(active.Id));
    }

    [TestMethod]
    public async Task RecordEnded_UnknownRecurringPayment_ReturnsFalse()
    {
        Assert.IsFalse(await _service.RecordEnded(UniqueIdentifier.New));
    }

    private static CancelRecurringPaymentResult Refusal(string error)
    {
        var result = new CancelRecurringPaymentResult();
        result.AddError(error);
        return result;
    }

    [TestMethod]
    public async Task Cancel_PendingWithoutProviderReference_CancelsWithoutTheGateway()
    {
        var rp = await Insert(RecurringPaymentStatus.Pending, 0);
        var calls = 0;

        var result = await _service.Cancel(rp.Id, "jane@example.com", _ =>
        {
            calls++;
            return Task.FromResult(new CancelRecurringPaymentResult());
        });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, calls);
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, saved.Status);
        Assert.AreEqual("jane@example.com", saved.CancelledBy);
        Assert.IsNotNull(saved.CancelledOnUtc);
    }

    [TestMethod]
    public async Task Cancel_Active_AsksTheGatewayFirst()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");
        RecurringPayment asked = null;

        var result = await _service.Cancel(rp.Id, "jane@example.com", r =>
        {
            asked = r;
            return Task.FromResult(new CancelRecurringPaymentResult());
        });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(rp.Id, asked?.Id);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, (await _service.GetRecurringPaymentById(rp.Id)).Status);
    }

    [TestMethod]
    public async Task Cancel_PendingWithProviderReference_AsksTheGateway()
    {
        var rp = await Insert(RecurringPaymentStatus.Pending, 0, providerReference: "sub_1");
        var calls = 0;

        var result = await _service.Cancel(rp.Id, "jane@example.com", _ =>
        {
            calls++;
            return Task.FromResult(new CancelRecurringPaymentResult());
        });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, calls);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, (await _service.GetRecurringPaymentById(rp.Id)).Status);
    }

    [TestMethod]
    public async Task Cancel_ActiveWithoutProviderReference_AsksTheGateway()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1);
        var calls = 0;

        var result = await _service.Cancel(rp.Id, "jane@example.com", _ =>
        {
            calls++;
            return Task.FromResult(new CancelRecurringPaymentResult());
        });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, calls);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, (await _service.GetRecurringPaymentById(rp.Id)).Status);
    }

    [TestMethod]
    public async Task Cancel_CallbackThrows_ReleasesTheLock()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _service.Cancel(rp.Id, "jane@example.com",
            _ => throw new InvalidOperationException("boom")));
        Assert.AreEqual(RecurringPaymentStatus.Active, (await _service.GetRecurringPaymentById(rp.Id)).Status);

        var result = await _service.Cancel(rp.Id, "jane@example.com",
            _ => Task.FromResult(new CancelRecurringPaymentResult()));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, (await _service.GetRecurringPaymentById(rp.Id)).Status);
    }

    [TestMethod]
    public async Task Cancel_GatewayRefuses_ReturnsTheErrorsAndKeepsTheStatus()
    {
        var rp = await Insert(RecurringPaymentStatus.PastDue, 1, providerReference: "sub_1");

        var result = await _service.Cancel(rp.Id, "jane@example.com",
            _ => Task.FromResult(Refusal("declined by gateway")));

        Assert.IsFalse(result.Success);
        CollectionAssert.Contains(result.Errors.ToList(), "declined by gateway");
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.PastDue, saved.Status);
        Assert.IsNull(saved.CancelledOnUtc);
    }

    [TestMethod]
    [DataRow(RecurringPaymentStatus.Cancelled)]
    [DataRow(RecurringPaymentStatus.Completed)]
    public async Task Cancel_CancelledOrCompleted_IsANoOp(RecurringPaymentStatus status)
    {
        var rp = await Insert(status, 1, providerReference: "sub_1");
        rp.CancelledBy = "earlier";
        await _repository.UpdateAsync(rp);
        var calls = 0;

        var result = await _service.Cancel(rp.Id, "jane@example.com", _ =>
        {
            calls++;
            return Task.FromResult(new CancelRecurringPaymentResult());
        });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, calls);
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(status, saved.Status);
        Assert.AreEqual("earlier", saved.CancelledBy);
    }

    [TestMethod]
    public async Task Cancel_UnknownRecurringPayment_ReturnsAnError()
    {
        var result = await _service.Cancel(UniqueIdentifier.New, "jane@example.com",
            _ => Task.FromResult(new CancelRecurringPaymentResult()));

        CollectionAssert.AreEqual(new[] { "The recurring payment was not found" }, result.Errors.ToList());
    }

    [TestMethod]
    public async Task Cancel_CycleWebhookDuringTheGatewayCall_WaitsAndIsRecordedOnCancelled()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");
        var other = new RecurringPaymentService(_repository, _productService.Object, _mediator, _resourceLock);
        var gatewayAsked = new TaskCompletionSource();
        var gatewayAnswer = new TaskCompletionSource<CancelRecurringPaymentResult>();

        var cancel = _service.Cancel(rp.Id, "jane@example.com", _ =>
        {
            gatewayAsked.SetResult();
            return gatewayAnswer.Task;
        });
        await gatewayAsked.Task;
        var claimTask = other.ClaimCycle(rp.Id, "sub_1", "in_2", 10, DateTime.UtcNow, null);
        await Task.Delay(200);
        Assert.IsFalse(claimTask.IsCompleted);

        gatewayAnswer.SetResult(new CancelRecurringPaymentResult());
        var result = await cancel;
        var claim = await claimTask;

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(claim);
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, saved.Status);
        Assert.AreEqual(2, saved.Cycles.Count);
    }
}
