using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class OrderApprovalServiceTests
{
    private const string StoreId = "store1";
    private const string OwnerId = "owner1";
    private const string BuyerId = "buyer1";

    private Mock<IMediator> _mediatorMock;
    private IRepository<OrderApproval> _repository;
    private OrderApprovalSettings _approvalSettings;
    private OrderApprovalService _service;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<OrderApproval>();
        _mediatorMock = new Mock<IMediator>();

        _approvalSettings = new OrderApprovalSettings { PaymentMethodSystemNames = ["Payments.CashOnDelivery"] };

        _service = new OrderApprovalService(_repository, _approvalSettings, _mediatorMock.Object);
    }

    private async Task<OrderApproval> AddApproval(OrderApprovalStatus status, string orderId = "order1",
        string customerId = BuyerId, string storeId = StoreId, string companyOwnerId = OwnerId,
        DateTime? createdOnUtc = null)
    {
        var approval = new OrderApproval {
            StoreId = storeId,
            CompanyOwnerId = companyOwnerId,
            CustomerId = customerId,
            Status = status,
            OrderId = orderId,
            Amount = 150
        };
        await _repository.InsertAsync(approval);

        // the repository stamps CreatedOnUtc on insert; set the wanted time afterwards
        approval.CreatedOnUtc = createdOnUtc ?? DateTime.UtcNow;
        await _repository.UpdateAsync(approval);
        return approval;
    }

    #region Payment methods

    [TestMethod]
    public void IsPaymentMethodAllowed_ListedMethod_True()
    {
        _approvalSettings.PaymentMethodSystemNames = ["Payments.CashOnDelivery", "Payments.BankTransfer"];

        Assert.IsTrue(_service.IsPaymentMethodAllowed("Payments.BankTransfer"));
    }

    [TestMethod]
    public void IsPaymentMethodAllowed_IgnoresCase()
    {
        _approvalSettings.PaymentMethodSystemNames = ["Payments.CashOnDelivery"];

        Assert.IsTrue(_service.IsPaymentMethodAllowed("payments.cashondelivery"));
    }

    [TestMethod]
    public void IsPaymentMethodAllowed_UnlistedMethod_False()
    {
        _approvalSettings.PaymentMethodSystemNames = ["Payments.CashOnDelivery"];

        Assert.IsFalse(_service.IsPaymentMethodAllowed("Payments.StripeCheckout"));
    }

    [TestMethod]
    public void IsPaymentMethodAllowed_EmptyListOrName_False()
    {
        _approvalSettings.PaymentMethodSystemNames = [];
        Assert.IsFalse(_service.IsPaymentMethodAllowed("Payments.CashOnDelivery"));

        _approvalSettings.PaymentMethodSystemNames = ["Payments.CashOnDelivery"];
        Assert.IsFalse(_service.IsPaymentMethodAllowed(null));
        Assert.IsFalse(_service.IsPaymentMethodAllowed(""));
    }

    [TestMethod]
    public void IsPaymentMethodAllowed_NullList_False()
    {
        _approvalSettings.PaymentMethodSystemNames = null;

        Assert.IsFalse(_service.IsPaymentMethodAllowed("Payments.CashOnDelivery"));
    }

    #endregion

    #region Reads and writes

    [TestMethod]
    public async Task GetById_OtherCompany_Null()
    {
        var approval = await AddApproval(OrderApprovalStatus.Pending);

        Assert.IsNull(await _service.GetById(approval.Id, "other-company"));
        Assert.IsNull(await _service.GetById(approval.Id, null));
        Assert.IsNull(await _service.GetById(approval.Id, ""));
    }

    [TestMethod]
    public async Task GetById_OwnCompany_ReturnsApproval()
    {
        var approval = await AddApproval(OrderApprovalStatus.Pending);

        var result = await _service.GetById(approval.Id, OwnerId);

        Assert.AreEqual(approval.Id, result?.Id);
    }

    [TestMethod]
    public async Task GetByOrderId_ReturnsTheOrdersApproval()
    {
        await AddApproval(OrderApprovalStatus.Pending, "order-other");
        var approval = await AddApproval(OrderApprovalStatus.Pending, "order-2");

        var result = await _service.GetByOrderId("order-2");

        Assert.AreEqual(approval.Id, result?.Id);
    }

    [TestMethod]
    public async Task GetByOrderId_UnknownOrEmpty_Null()
    {
        await AddApproval(OrderApprovalStatus.Pending, "order-1");

        Assert.IsNull(await _service.GetByOrderId("nope"));
        Assert.IsNull(await _service.GetByOrderId(null));
        Assert.IsNull(await _service.GetByOrderId(""));
    }

    [TestMethod]
    public async Task GetForCompanyAndCustomer_EmptyId_GiveAnEmptyPage()
    {
        await _repository.InsertAsync(new OrderApproval { CompanyOwnerId = "", CustomerId = "", StoreId = StoreId });

        Assert.AreEqual(0, (await _service.GetForCompany("", StoreId, 0, 10)).TotalCount);
        Assert.AreEqual(0, (await _service.GetForCompany(OwnerId, "", 0, 10)).TotalCount);
        Assert.AreEqual(0, (await _service.GetForCustomer("", StoreId, 0, 10)).TotalCount);
        Assert.AreEqual(0, (await _service.GetForCustomer(BuyerId, "", 0, 10)).TotalCount);
    }

    [TestMethod]
    public async Task GetForCompany_PendingFirst()
    {
        var now = DateTime.UtcNow;
        var oldPending = await AddApproval(OrderApprovalStatus.Pending, createdOnUtc: now.AddDays(-3));
        var newApproved = await AddApproval(OrderApprovalStatus.Approved, createdOnUtc: now);
        var oldApproved = await AddApproval(OrderApprovalStatus.Approved, createdOnUtc: now.AddDays(-1));
        var newPending = await AddApproval(OrderApprovalStatus.Pending, createdOnUtc: now.AddDays(-2));
        await AddApproval(OrderApprovalStatus.Pending, companyOwnerId: "other-company");
        await AddApproval(OrderApprovalStatus.Pending, storeId: "other-store");

        var result = await _service.GetForCompany(OwnerId, StoreId, 0, 10);

        CollectionAssert.AreEqual(
            new[] { newPending.Id, oldPending.Id, newApproved.Id, oldApproved.Id },
            result.Select(x => x.Id).ToArray());
        Assert.AreEqual(4, result.TotalCount);
    }

    [TestMethod]
    public async Task GetForCompany_Paged()
    {
        for (var i = 0; i < 5; i++)
            await AddApproval(OrderApprovalStatus.Pending, createdOnUtc: DateTime.UtcNow.AddMinutes(-i));

        var result = await _service.GetForCompany(OwnerId, StoreId, 1, 2);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(5, result.TotalCount);
    }

    [TestMethod]
    public async Task GetForCustomer_NewestFirstAndOnlyOwn()
    {
        var now = DateTime.UtcNow;
        var older = await AddApproval(OrderApprovalStatus.Rejected, createdOnUtc: now.AddDays(-1));
        var newer = await AddApproval(OrderApprovalStatus.Pending, createdOnUtc: now);
        await AddApproval(OrderApprovalStatus.Pending, customerId: "another-buyer");
        await AddApproval(OrderApprovalStatus.Pending, storeId: "other-store");

        var result = await _service.GetForCustomer(BuyerId, StoreId, 0, 10);

        CollectionAssert.AreEqual(new[] { newer.Id, older.Id }, result.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task GetForCustomer_LeavesOutStoreDecidedRequests()
    {
        var companyDecided = await AddApproval(OrderApprovalStatus.Pending);
        await _repository.InsertAsync(new OrderApproval {
            CustomerId = BuyerId, StoreId = StoreId, Decider = OrderApprovalDecider.Store,
            Status = OrderApprovalStatus.Pending
        });

        var result = await _service.GetForCustomer(BuyerId, StoreId, 0, 10);

        CollectionAssert.AreEqual(new[] { companyDecided.Id }, result.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task Insert_StoresAndPublishesEvent()
    {
        var approval = new OrderApproval { CustomerId = BuyerId, StoreId = StoreId, CompanyOwnerId = OwnerId };

        await _service.Insert(approval);

        Assert.IsNotNull(await _repository.GetByIdAsync(approval.Id));
        _mediatorMock.Verify(x => x.Publish(It.IsAny<EntityInserted<OrderApproval>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task Update_StoresAndPublishesEvent()
    {
        var approval = await AddApproval(OrderApprovalStatus.Pending);
        approval.Status = OrderApprovalStatus.Approved;

        await _service.Update(approval);

        var stored = await _repository.GetByIdAsync(approval.Id);
        Assert.AreEqual(OrderApprovalStatus.Approved, stored.Status);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<EntityUpdated<OrderApproval>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task Insert_Null_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.Insert(null));
    }

    #endregion

    #region Rules

    [TestMethod]
    public void CompanyId_Owner_IsOwnId()
    {
        Assert.AreEqual("c1", OrderApprovalRules.CompanyId(new Customer { Id = "c1" }));
    }

    [TestMethod]
    public void CompanyId_SubAccount_IsOwnerId()
    {
        Assert.AreEqual(OwnerId, OrderApprovalRules.CompanyId(new Customer { Id = "c1", OwnerId = OwnerId }));
    }

    [TestMethod]
    public void CanApprove_OwnerAndApproverOnly()
    {
        Assert.IsTrue(OrderApprovalRules.CanApprove(new Customer()));
        Assert.IsTrue(OrderApprovalRules.CanApprove(new Customer
            { OwnerId = OwnerId, SubAccountRole = SubAccountRole.Approver }));
        Assert.IsFalse(OrderApprovalRules.CanApprove(new Customer
            { OwnerId = OwnerId, SubAccountRole = SubAccountRole.Buyer }));
    }

    #endregion
}
