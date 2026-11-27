using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
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
    private Mock<IMediator> _mediatorMock;
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
        _mediatorMock = new Mock<IMediator>();
        _mediator = _mediatorMock.Object;
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

        Assert.AreEqual(RecurringReportOutcome.Recorded, claim.Outcome);
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
    public async Task ClaimCycle_SameCycleReferenceAgain_IsADuplicate()
    {
        var rp = await Insert(RecurringPaymentStatus.Pending, 0);

        Assert.AreEqual(RecurringReportOutcome.Recorded, (await _service.ClaimCycle(rp.Id, "sub_1", "in_1", 10, DateTime.UtcNow, null)).Outcome);
        Assert.AreEqual(RecurringReportOutcome.Duplicate, (await _service.ClaimCycle(rp.Id, "sub_1", "in_1", 10, DateTime.UtcNow, null)).Outcome);

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

        Assert.AreEqual(1, claims.Count(c => c.Outcome == RecurringReportOutcome.Recorded));
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

        Assert.AreEqual(RecurringReportOutcome.Recorded, claim.Outcome);
        Assert.AreEqual(2, claim.Cycle.CycleNumber);
        Assert.IsFalse(claim.Completed);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, (await _service.GetRecurringPaymentById(rp.Id)).Status);
    }

    [TestMethod]
    public async Task ClaimCycle_ReachingTotalCyclesAfterCancelled_KeepsTheStatusAndIsNotCompleted()
    {
        var rp = await Insert(RecurringPaymentStatus.Cancelled, 1, 2);

        var claim = await _service.ClaimCycle(rp.Id, "sub_1", "in_2", 10, DateTime.UtcNow, null);

        Assert.AreEqual(RecurringReportOutcome.Recorded, claim.Outcome);
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

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            _service.RecordFailure(rp.Id, "sub_1", "", "declined"));
    }

    [TestMethod]
    public async Task ClaimCycle_UnknownRecurringPayment_IsNotFound()
    {
        Assert.AreEqual(RecurringReportOutcome.NotFound, (await _service.ClaimCycle(UniqueIdentifier.New, "sub_1", "in_1", 10, DateTime.UtcNow, null)).Outcome);
    }

    [TestMethod]
    public async Task RecordFailure_Active_EntersPastDueAndKeepsTheLastError()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1);

        Assert.AreEqual(RecurringReportOutcome.Recorded, (await _service.RecordFailure(rp.Id, "sub_1", "in_2", "declined")).Outcome);
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.PastDue, saved.Status);
        Assert.AreEqual("declined", saved.LastError);
        Assert.IsNotNull(saved.LastErrorDateUtc);

        Assert.AreEqual(RecurringReportOutcome.Recorded, (await _service.RecordFailure(rp.Id, "sub_1", "in_2", "expired")).Outcome);
        saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.PastDue, saved.Status);
        Assert.AreEqual("expired", saved.LastError);
    }

    [TestMethod]
    public async Task RecordFailure_ForACyclePaidAlready_IsIgnored()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 2);

        Assert.AreEqual(RecurringReportOutcome.Duplicate, (await _service.RecordFailure(rp.Id, "sub_1", "in_2", "late")).Outcome);

        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Active, saved.Status);
        Assert.IsNull(saved.LastError);
    }

    [TestMethod]
    public async Task RecordFailure_Cancelled_RecordsOnlyTheMissingReference()
    {
        // cancelled while pending: the gateway still charges, and its reference is needed to stop it
        var rp = await Insert(RecurringPaymentStatus.Cancelled, 1);

        var report = await _service.RecordFailure(rp.Id, "sub_1", "in_2", "declined");

        Assert.AreEqual(RecurringReportOutcome.Ended, report.Outcome);
        Assert.AreEqual("sub_1", report.RecurringPayment.ProviderReference);
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, saved.Status);
        Assert.AreEqual("sub_1", saved.ProviderReference);
        Assert.IsNull(saved.LastError);
    }

    [TestMethod]
    public async Task RecordFailure_UnknownRecurringPayment_IsNotFound()
    {
        Assert.AreEqual(RecurringReportOutcome.NotFound, (await _service.RecordFailure(UniqueIdentifier.New, "sub_1", "in_2", "declined")).Outcome);
    }

    [TestMethod]
    public async Task RecordEnded_Active_CancelsByTheProvider()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1);
        rp.PaymentMethodSystemName = "Payments.Test";
        await _repository.UpdateAsync(rp);

        Assert.AreEqual(RecurringReportOutcome.Recorded, (await _service.RecordEnded(rp.Id, "sub_1")).Outcome);

        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, saved.Status);
        Assert.AreEqual("sub_1", saved.ProviderReference);
        Assert.AreEqual("Payments.Test", saved.CancelledBy);
        Assert.IsNotNull(saved.CancelledOnUtc);
    }

    [TestMethod]
    public async Task RecordEnded_CompletedOrCancelled_ChangesNothing()
    {
        var completed = await Insert(RecurringPaymentStatus.Completed, 12);
        Assert.AreEqual(RecurringReportOutcome.Ended, (await _service.RecordEnded(completed.Id, "sub_1")).Outcome);
        Assert.AreEqual(RecurringPaymentStatus.Completed,
            (await _service.GetRecurringPaymentById(completed.Id)).Status);

        var active = await Insert(RecurringPaymentStatus.Active, 1);
        Assert.AreEqual(RecurringReportOutcome.Recorded, (await _service.RecordEnded(active.Id, "sub_1")).Outcome);
        Assert.AreEqual(RecurringReportOutcome.Ended, (await _service.RecordEnded(active.Id, "sub_1")).Outcome);
    }

    [TestMethod]
    public async Task RecordEnded_UnknownRecurringPayment_IsNotFound()
    {
        Assert.AreEqual(RecurringReportOutcome.NotFound, (await _service.RecordEnded(UniqueIdentifier.New, "sub_1")).Outcome);
    }

    [TestMethod]
    public async Task ClaimCycle_OtherProviderReference_RecordsNothing()
    {
        // a second subscription started for the same order reports with the same recurring payment id
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");

        Assert.AreEqual(RecurringReportOutcome.OtherReference, (await _service.ClaimCycle(rp.Id, "sub_2", "in_9", 10, DateTime.UtcNow, null)).Outcome);

        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(1, saved.CyclesPaid);
        Assert.AreEqual("sub_1", saved.ProviderReference);
    }

    [TestMethod]
    public async Task ClaimCycle_ProviderReferenceNotGiven_IsNotChecked()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");

        Assert.AreEqual(RecurringReportOutcome.Recorded, (await _service.ClaimCycle(rp.Id, null, "in_2", 10, DateTime.UtcNow, null)).Outcome);
    }

    [TestMethod]
    public async Task RecordFailure_OtherProviderReference_ChangesNothing()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");

        Assert.AreEqual(RecurringReportOutcome.OtherReference, (await _service.RecordFailure(rp.Id, "sub_2", "in_9", "declined")).Outcome);

        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Active, saved.Status);
        Assert.IsNull(saved.LastError);
    }

    [TestMethod]
    public async Task RecordEnded_OtherProviderReference_KeepsThePaymentRunning()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");

        Assert.AreEqual(RecurringReportOutcome.OtherReference, (await _service.RecordEnded(rp.Id, "sub_2")).Outcome);

        Assert.AreEqual(RecurringPaymentStatus.Active, (await _service.GetRecurringPaymentById(rp.Id)).Status);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<RecurringPaymentCancelledEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task RecordProviderReference_Pending_RecordsItSoACancelAsksTheGateway()
    {
        var rp = await Insert(RecurringPaymentStatus.Pending, 0);

        var report = await _service.RecordProviderReference(rp.Id, "sub_1");

        Assert.AreEqual(RecurringReportOutcome.Recorded, report.Outcome);
        Assert.AreEqual("sub_1", report.RecurringPayment.ProviderReference);
        Assert.AreEqual(RecurringPaymentStatus.Pending, report.RecurringPayment.Status);
        Assert.AreEqual("sub_1", (await _service.GetRecurringPaymentById(rp.Id)).ProviderReference);

        var asked = false;
        await _service.Cancel(rp.Id, "customer", _ =>
        {
            asked = true;
            return Task.FromResult(new CancelRecurringPaymentResult());
        });
        Assert.IsTrue(asked);
    }

    [TestMethod]
    public async Task RecordProviderReference_AnotherOneRecorded_KeepsIt()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");

        var report = await _service.RecordProviderReference(rp.Id, "sub_2");

        Assert.AreEqual(RecurringReportOutcome.OtherReference, report.Outcome);
        Assert.AreEqual("sub_1", report.RecurringPayment.ProviderReference);
        Assert.AreEqual("sub_1", (await _service.GetRecurringPaymentById(rp.Id)).ProviderReference);
    }

    [TestMethod]
    public async Task RecordProviderReference_SameOneAgain_IsADuplicate()
    {
        var rp = await Insert(RecurringPaymentStatus.Cancelled, 0);

        Assert.AreEqual(RecurringReportOutcome.Recorded, (await _service.RecordProviderReference(rp.Id, "sub_1")).Outcome);
        Assert.AreEqual(RecurringReportOutcome.Duplicate, (await _service.RecordProviderReference(rp.Id, "sub_1")).Outcome);
    }

    [TestMethod]
    public async Task RecordEnded_BeforeTheReferenceIsKnown_RunsOnTheReportedOne()
    {
        // the first reference reported is the one the payment runs on, whichever report brings it
        var rp = await Insert(RecurringPaymentStatus.Pending, 0);

        Assert.AreEqual(RecurringReportOutcome.Recorded, (await _service.RecordEnded(rp.Id, "sub_2")).Outcome);

        Assert.AreEqual("sub_2", (await _service.GetRecurringPaymentById(rp.Id)).ProviderReference);
        Assert.AreEqual(RecurringReportOutcome.OtherReference, (await _service.RecordProviderReference(rp.Id, "sub_1")).Outcome);
    }

    [TestMethod]
    public async Task RecordFailure_BeforeTheReferenceIsKnown_RunsOnTheReportedOne()
    {
        var rp = await Insert(RecurringPaymentStatus.Pending, 0);

        Assert.AreEqual(RecurringReportOutcome.Recorded, (await _service.RecordFailure(rp.Id, "sub_2", "in_1", "declined")).Outcome);

        Assert.AreEqual("sub_2", (await _service.GetRecurringPaymentById(rp.Id)).ProviderReference);
        Assert.AreEqual(RecurringReportOutcome.OtherReference, (await _service.ClaimCycle(rp.Id, "sub_1", "in_1", 10, DateTime.UtcNow, null)).Outcome);
    }

    [TestMethod]
    public async Task RecordProviderReference_Cancelled_RecordsItAndKeepsTheStatus()
    {
        var rp = await Insert(RecurringPaymentStatus.Cancelled, 0);

        var report = await _service.RecordProviderReference(rp.Id, "sub_1");

        Assert.AreEqual(RecurringReportOutcome.Recorded, report.Outcome);
        Assert.AreEqual("sub_1", report.RecurringPayment.ProviderReference);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, report.RecurringPayment.Status);
    }

    [TestMethod]
    public async Task RecordProviderReference_UnknownRecurringPayment_IsNotFound()
    {
        Assert.AreEqual(RecurringReportOutcome.NotFound, (await _service.RecordProviderReference(UniqueIdentifier.New, "sub_1")).Outcome);
    }

    [TestMethod]
    public async Task RecordProviderReference_Empty_Throws()
    {
        var rp = await Insert(RecurringPaymentStatus.Pending, 0);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _service.RecordProviderReference(rp.Id, ""));
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

    private static readonly TimeSpan ConcurrentTimeout = TimeSpan.FromSeconds(30);

    [TestMethod]
    public async Task Cancel_CycleWebhookDuringTheGatewayCall_IsNotHeldUpAndIsKept()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");
        var other = new RecurringPaymentService(_repository, _productService.Object, _mediator, _resourceLock);
        Task<RecurringCycleClaim> claimTask = null;

        var result = await _service.Cancel(rp.Id, "jane@example.com", async _ =>
        {
            //the gateway has not answered yet, and the webhook must not wait for it
            claimTask = other.ClaimCycle(rp.Id, "sub_1", "in_2", 10, DateTime.UtcNow, null);
            await claimTask.WaitAsync(ConcurrentTimeout);
            return new CancelRecurringPaymentResult();
        });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(RecurringReportOutcome.Recorded, (await claimTask).Outcome);
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, saved.Status);
        Assert.AreEqual(2, saved.Cycles.Count);
        Assert.AreEqual(2, saved.CyclesPaid);
        Assert.AreEqual(1, Published<RecurringPaymentCancelledEvent>().Count);
    }

    [TestMethod]
    public async Task Cancel_EndedAtTheGatewayDuringTheCall_PublishesOnce()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");
        rp.PaymentMethodSystemName = "Payments.Test";
        await _repository.UpdateAsync(rp);
        var other = new RecurringPaymentService(_repository, _productService.Object, _mediator, _resourceLock);
        Task<RecurringReportResult> endedTask = null;

        var result = await _service.Cancel(rp.Id, "jane@example.com", async _ =>
        {
            endedTask = other.RecordEnded(rp.Id, "sub_1");
            await endedTask.WaitAsync(ConcurrentTimeout);
            return new CancelRecurringPaymentResult();
        });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(RecurringReportOutcome.Recorded, (await endedTask).Outcome);
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, saved.Status);
        Assert.AreEqual("Payments.Test", saved.CancelledBy);
        Assert.AreEqual(1, Published<RecurringPaymentCancelledEvent>().Count);
    }

    [TestMethod]
    public async Task Cancel_GatewayRefusesBecauseAnotherCancelStoppedItFirst_Succeeds()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");
        var other = new RecurringPaymentService(_repository, _productService.Object, _mediator, _resourceLock);

        var result = await _service.Cancel(rp.Id, "jane@example.com", async _ =>
        {
            var first = await other.Cancel(rp.Id, "staff@example.com",
                _ => Task.FromResult(new CancelRecurringPaymentResult())).WaitAsync(ConcurrentTimeout);
            Assert.IsTrue(first.Success);
            return Refusal("the subscription is already cancelled");
        });

        Assert.IsTrue(result.Success);
        var saved = await _service.GetRecurringPaymentById(rp.Id);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, saved.Status);
        Assert.AreEqual("staff@example.com", saved.CancelledBy);
        Assert.AreEqual(1, Published<RecurringPaymentCancelledEvent>().Count);
    }

    private List<T> Published<T>()
    {
        return _mediatorMock.Invocations.Where(x => x.Method.Name == nameof(IMediator.Publish))
            .Select(x => x.Arguments[0]).OfType<T>().ToList();
    }

    [TestMethod]
    public async Task Cancel_PublishesCancelledOnce()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");

        Assert.IsTrue((await _service.Cancel(rp.Id, "jane@example.com", _ => Task.FromResult(new CancelRecurringPaymentResult()))).Success);
        Assert.IsTrue((await _service.Cancel(rp.Id, "jane@example.com", _ => Task.FromResult(new CancelRecurringPaymentResult()))).Success);

        var published = Published<RecurringPaymentCancelledEvent>();
        Assert.AreEqual(1, published.Count);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, published[0].RecurringPayment.Status);
        Assert.AreEqual(0, Published<RecurringPaymentPastDueEvent>().Count);
    }

    [TestMethod]
    public async Task Cancel_GatewayRefuses_PublishesNothing()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, providerReference: "sub_1");

        var result = await _service.Cancel(rp.Id, "jane@example.com", _ => Task.FromResult(Refusal("declined")));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, Published<RecurringPaymentCancelledEvent>().Count);
        Assert.AreEqual(0, Published<RecurringPaymentPastDueEvent>().Count);
    }

    [TestMethod]
    public async Task RecordEnded_PublishesCancelledOnlyOnTheTransition()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1);

        Assert.AreEqual(RecurringReportOutcome.Recorded, (await _service.RecordEnded(rp.Id, "sub_1")).Outcome);
        Assert.AreEqual(RecurringReportOutcome.Ended, (await _service.RecordEnded(rp.Id, "sub_1")).Outcome);

        var published = Published<RecurringPaymentCancelledEvent>();
        Assert.AreEqual(1, published.Count);
        Assert.AreEqual(RecurringPaymentStatus.Cancelled, published[0].RecurringPayment.Status);
    }

    [TestMethod]
    public async Task RecordFailure_PublishesPastDueOnlyWhenEnteringIt()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1);

        await _service.RecordFailure(rp.Id, "sub_1", "in_2", "a");
        await _service.RecordFailure(rp.Id, "sub_1", "in_2", "b");

        var published = Published<RecurringPaymentPastDueEvent>();
        Assert.AreEqual(1, published.Count);
        Assert.AreEqual("a", published[0].RecurringPayment.LastError);
    }

    [TestMethod]
    public async Task ClaimCycle_Completing_PublishesNeitherEvent()
    {
        var rp = await Insert(RecurringPaymentStatus.Active, 1, 2);

        var claim = await _service.ClaimCycle(rp.Id, "sub_1", "in_2", 10, DateTime.UtcNow, null);

        Assert.IsTrue(claim.Completed);
        Assert.AreEqual(0, Published<RecurringPaymentCancelledEvent>().Count);
        Assert.AreEqual(0, Published<RecurringPaymentPastDueEvent>().Count);
    }

    [TestMethod]
    public async Task GetCustomerRecurringPayments_ReturnsOnlyTheCustomersInTheStore_NewestFirst()
    {
        await _repository.InsertAsync(new RecurringPayment {
            Id = "old", CustomerId = "c1", StoreId = "s1", CreatedOnUtc = new DateTime(2026, 1, 1)
        });
        await _repository.InsertAsync(new RecurringPayment {
            Id = "new", CustomerId = "c1", StoreId = "s1", CreatedOnUtc = new DateTime(2026, 2, 1)
        });
        await _repository.InsertAsync(new RecurringPayment { Id = "other-customer", CustomerId = "c2", StoreId = "s1" });
        await _repository.InsertAsync(new RecurringPayment { Id = "other-store", CustomerId = "c1", StoreId = "s2" });

        var result = await _service.GetCustomerRecurringPayments("c1", "s1");

        CollectionAssert.AreEqual(new[] { "new", "old" }, result.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task HasCustomerRecurringPayments_OnlyTheCustomersInTheStore()
    {
        await _repository.InsertAsync(new RecurringPayment { Id = "rp1", CustomerId = "c1", StoreId = "s1" });

        Assert.IsTrue(await _service.HasCustomerRecurringPayments("c1", "s1"));
        Assert.IsFalse(await _service.HasCustomerRecurringPayments("c1", "s2"));
        Assert.IsFalse(await _service.HasCustomerRecurringPayments("c2", "s1"));
    }

    [TestMethod]
    public async Task HasCustomerRecurringPayments_EmptyCustomerOrStore_False()
    {
        await _repository.InsertAsync(new RecurringPayment { CustomerId = "", StoreId = "" });

        Assert.IsFalse(await _service.HasCustomerRecurringPayments("", ""));
    }

    [TestMethod]
    public async Task GetCustomerRecurringPayments_EmptyCustomerOrStore_ReturnsNothing()
    {
        await _repository.InsertAsync(new RecurringPayment { CustomerId = "", StoreId = "" });

        Assert.AreEqual(0, (await _service.GetCustomerRecurringPayments("", "s1")).Count);
        Assert.AreEqual(0, (await _service.GetCustomerRecurringPayments("c1", "")).Count);
    }
}
