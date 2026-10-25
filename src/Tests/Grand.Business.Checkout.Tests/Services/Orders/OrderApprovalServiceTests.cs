using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
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

    private Mock<IOrderCalculationService> _calculationServiceMock;
    private CustomerSettings _customerSettings;
    private Mock<IMediator> _mediatorMock;
    private IRepository<OrderApproval> _repository;
    private OrderApprovalService _service;
    private double? _total;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<OrderApproval>();
        _mediatorMock = new Mock<IMediator>();
        _customerSettings = new CustomerSettings();
        _total = 150;
        _calculationServiceMock = new Mock<IOrderCalculationService>();
        _calculationServiceMock
            .Setup(x => x.GetShoppingCartTotal(It.IsAny<IList<ShoppingCartItem>>(), It.IsAny<CalculationContext>(),
                It.IsAny<bool?>(), It.IsAny<bool>()))
            .ReturnsAsync(() => new ShoppingCartTotal(_total, 0, [], [], 0, 0));

        _service = new OrderApprovalService(_repository, _calculationServiceMock.Object, _customerSettings,
            _mediatorMock.Object);
    }

    private static Customer Buyer(double? limit = 100, SubAccountRole role = SubAccountRole.Buyer,
        string ownerId = OwnerId)
    {
        return new Customer {
            Id = BuyerId, OwnerId = ownerId, SubAccountRole = role, OrderApprovalLimit = limit
        };
    }

    private static CalculationContext Context(Customer customer, string storeId = StoreId)
    {
        return new CalculationContext(customer, new Store { Id = storeId }, new Currency(),
            TaxDisplayType.IncludingTax);
    }

    private static List<ShoppingCartItem> Cart(int quantity = 1)
    {
        return [
            new ShoppingCartItem {
                ProductId = "p1", Quantity = quantity, StoreId = StoreId,
                ShoppingCartTypeId = ShoppingCartType.ShoppingCart
            }
        ];
    }

    private async Task<OrderApproval> AddApproval(OrderApprovalStatus status, IList<ShoppingCartItem> cart,
        double amount, string customerId = BuyerId, string storeId = StoreId, string companyOwnerId = OwnerId,
        DateTime? createdOnUtc = null)
    {
        var approval = new OrderApproval {
            StoreId = storeId,
            CompanyOwnerId = companyOwnerId,
            CustomerId = customerId,
            Status = status,
            CartFingerprint = OrderApprovalFingerprint.Compute(cart),
            Amount = amount
        };
        await _repository.InsertAsync(approval);

        // the repository stamps CreatedOnUtc on insert; set the wanted time afterwards
        approval.CreatedOnUtc = createdOnUtc ?? DateTime.UtcNow;
        await _repository.UpdateAsync(approval);
        return approval;
    }

    #region Checkout state

    [TestMethod]
    public async Task GetCheckoutState_NoOwner_NotRequired()
    {
        var customer = Buyer(100, ownerId: null);

        var state = await _service.GetCheckoutState(Cart(), Context(customer));

        Assert.IsFalse(state.Required);
        Assert.IsFalse(state.Blocked);
        _calculationServiceMock.Verify(x => x.GetShoppingCartTotal(It.IsAny<IList<ShoppingCartItem>>(),
            It.IsAny<CalculationContext>(), It.IsAny<bool?>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task GetCheckoutState_NoLimit_NotRequired()
    {
        var state = await _service.GetCheckoutState(Cart(), Context(Buyer(null)));

        Assert.IsFalse(state.Required);
        Assert.IsFalse(state.Blocked);
    }

    [TestMethod]
    public async Task GetCheckoutState_Approver_NotRequired()
    {
        var state = await _service.GetCheckoutState(Cart(), Context(Buyer(100, SubAccountRole.Approver)));

        Assert.IsFalse(state.Required);
        Assert.IsFalse(state.Blocked);
    }

    [TestMethod]
    public async Task GetCheckoutState_FeatureOff_NotRequired()
    {
        _customerSettings.HideSubAccountsTab = true;

        var state = await _service.GetCheckoutState(Cart(), Context(Buyer(100)));

        Assert.IsFalse(state.Required);
        Assert.IsFalse(state.Blocked);
    }

    [TestMethod]
    public async Task GetCheckoutState_TotalUnknown_NotRequired()
    {
        _total = null;

        var state = await _service.GetCheckoutState(Cart(), Context(Buyer(100)));

        Assert.IsFalse(state.Required);
        Assert.IsFalse(state.Blocked);
        Assert.IsNull(state.Total);
        Assert.AreEqual(100, state.Limit);
    }

    [TestMethod]
    public async Task GetCheckoutState_TotalAtLimit_NotRequired()
    {
        _total = 100;

        var state = await _service.GetCheckoutState(Cart(), Context(Buyer(100)));

        Assert.IsFalse(state.Required);
        Assert.IsFalse(state.Blocked);
        Assert.AreEqual(100, state.Total);
    }

    [TestMethod]
    public async Task GetCheckoutState_OverLimit_Blocked()
    {
        var state = await _service.GetCheckoutState(Cart(), Context(Buyer(100)));

        Assert.IsTrue(state.Required);
        Assert.IsTrue(state.Blocked);
        Assert.AreEqual(100, state.Limit);
        Assert.AreEqual(150, state.Total);
        Assert.IsNull(state.Active);
    }

    [TestMethod]
    public async Task GetCheckoutState_OverLimitWithPending_BlockedAndPendingIsActive()
    {
        var cart = Cart();
        var pending = await AddApproval(OrderApprovalStatus.Pending, cart, 150);

        var state = await _service.GetCheckoutState(cart, Context(Buyer(100)));

        Assert.IsTrue(state.Blocked);
        Assert.AreEqual(pending.Id, state.Active?.Id);
    }

    [TestMethod]
    public async Task GetCheckoutState_ApprovedMatching_NotBlocked()
    {
        var cart = Cart();
        var approval = await AddApproval(OrderApprovalStatus.Approved, cart, 150);

        var state = await _service.GetCheckoutState(cart, Context(Buyer(100)));

        Assert.IsTrue(state.Required);
        Assert.IsFalse(state.Blocked);
        Assert.AreEqual(approval.Id, state.Active?.Id);
    }

    [TestMethod]
    public async Task GetCheckoutState_ApprovedButCartChanged_Blocked()
    {
        await AddApproval(OrderApprovalStatus.Approved, Cart(1), 150);

        var state = await _service.GetCheckoutState(Cart(2), Context(Buyer(100)));

        Assert.IsTrue(state.Blocked);
        Assert.IsNull(state.Active);
    }

    [TestMethod]
    public async Task GetCheckoutState_ApprovedButTotalHigher_Blocked()
    {
        var cart = Cart();
        await AddApproval(OrderApprovalStatus.Approved, cart, 150);
        _total = 150.01;

        var state = await _service.GetCheckoutState(cart, Context(Buyer(100)));

        Assert.IsTrue(state.Blocked);
    }

    [TestMethod]
    public async Task GetCheckoutState_ApprovedButTotalLower_NotBlocked()
    {
        var cart = Cart();
        await AddApproval(OrderApprovalStatus.Approved, cart, 150);
        _total = 120;

        var state = await _service.GetCheckoutState(cart, Context(Buyer(100)));

        Assert.IsFalse(state.Blocked);
    }

    [TestMethod]
    public async Task GetCheckoutState_ApprovalOfOtherStore_Blocked()
    {
        var cart = Cart();
        await AddApproval(OrderApprovalStatus.Approved, cart, 150, storeId: "other-store");

        var state = await _service.GetCheckoutState(cart, Context(Buyer(100)));

        Assert.IsTrue(state.Blocked);
    }

    [TestMethod]
    public async Task GetCheckoutState_ApprovalOfOtherCustomer_Blocked()
    {
        var cart = Cart();
        await AddApproval(OrderApprovalStatus.Approved, cart, 150, "another-buyer");

        var state = await _service.GetCheckoutState(cart, Context(Buyer(100)));

        Assert.IsTrue(state.Blocked);
    }

    [TestMethod]
    [DataRow(OrderApprovalStatus.Rejected)]
    [DataRow(OrderApprovalStatus.Withdrawn)]
    [DataRow(OrderApprovalStatus.Used)]
    public async Task GetCheckoutState_ApprovalNoLongerUsable_Blocked(OrderApprovalStatus status)
    {
        var cart = Cart();
        await AddApproval(status, cart, 150);

        var state = await _service.GetCheckoutState(cart, Context(Buyer(100)));

        Assert.IsTrue(state.Blocked);
        Assert.IsNull(state.Active);
    }

    #endregion

    #region Reads and writes

    [TestMethod]
    public async Task GetById_OtherCompany_Null()
    {
        var approval = await AddApproval(OrderApprovalStatus.Pending, Cart(), 150);

        Assert.IsNull(await _service.GetById(approval.Id, "other-company"));
        Assert.IsNull(await _service.GetById(approval.Id, null));
        Assert.IsNull(await _service.GetById(approval.Id, ""));
    }

    [TestMethod]
    public async Task GetById_OwnCompany_ReturnsApproval()
    {
        var approval = await AddApproval(OrderApprovalStatus.Pending, Cart(), 150);

        var result = await _service.GetById(approval.Id, OwnerId);

        Assert.AreEqual(approval.Id, result?.Id);
    }

    [TestMethod]
    public async Task GetActive_ReturnsNewestPendingOrApproved()
    {
        var now = DateTime.UtcNow;
        await AddApproval(OrderApprovalStatus.Rejected, Cart(), 150, createdOnUtc: now);
        await AddApproval(OrderApprovalStatus.Pending, Cart(), 150, createdOnUtc: now.AddHours(-2));
        var newest = await AddApproval(OrderApprovalStatus.Approved, Cart(), 150, createdOnUtc: now.AddHours(-1));
        await AddApproval(OrderApprovalStatus.Approved, Cart(), 150, "another-buyer", createdOnUtc: now);
        await AddApproval(OrderApprovalStatus.Approved, Cart(), 150, storeId: "other-store", createdOnUtc: now);

        var result = await _service.GetActive(BuyerId, StoreId);

        Assert.AreEqual(newest.Id, result?.Id);
    }

    [TestMethod]
    public async Task GetActive_NothingActive_Null()
    {
        await AddApproval(OrderApprovalStatus.Used, Cart(), 150);

        Assert.IsNull(await _service.GetActive(BuyerId, StoreId));
    }

    [TestMethod]
    public async Task GetForCompany_PendingFirst()
    {
        var now = DateTime.UtcNow;
        var oldPending = await AddApproval(OrderApprovalStatus.Pending, Cart(), 150, createdOnUtc: now.AddDays(-3));
        var newApproved = await AddApproval(OrderApprovalStatus.Approved, Cart(), 150, createdOnUtc: now);
        var oldApproved = await AddApproval(OrderApprovalStatus.Approved, Cart(), 150, createdOnUtc: now.AddDays(-1));
        var newPending = await AddApproval(OrderApprovalStatus.Pending, Cart(), 150, createdOnUtc: now.AddDays(-2));
        await AddApproval(OrderApprovalStatus.Pending, Cart(), 150, companyOwnerId: "other-company");
        await AddApproval(OrderApprovalStatus.Pending, Cart(), 150, storeId: "other-store");

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
            await AddApproval(OrderApprovalStatus.Pending, Cart(), 150, createdOnUtc: DateTime.UtcNow.AddMinutes(-i));

        var result = await _service.GetForCompany(OwnerId, StoreId, 1, 2);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(5, result.TotalCount);
    }

    [TestMethod]
    public async Task GetForCustomer_NewestFirstAndOnlyOwn()
    {
        var now = DateTime.UtcNow;
        var older = await AddApproval(OrderApprovalStatus.Rejected, Cart(), 150, createdOnUtc: now.AddDays(-1));
        var newer = await AddApproval(OrderApprovalStatus.Pending, Cart(), 150, createdOnUtc: now);
        await AddApproval(OrderApprovalStatus.Pending, Cart(), 150, "another-buyer");
        await AddApproval(OrderApprovalStatus.Pending, Cart(), 150, storeId: "other-store");

        var result = await _service.GetForCustomer(BuyerId, StoreId, 0, 10);

        CollectionAssert.AreEqual(new[] { newer.Id, older.Id }, result.Select(x => x.Id).ToArray());
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
        var approval = await AddApproval(OrderApprovalStatus.Pending, Cart(), 150);
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

    [TestMethod]
    public void CheckoutState_Blocked_OnlyWhenRequiredAndNotApproved()
    {
        Assert.IsFalse(new OrderApprovalCheckoutState(false, null, null, null).Blocked);
        Assert.IsTrue(new OrderApprovalCheckoutState(true, 1, 2, null).Blocked);
        Assert.IsTrue(new OrderApprovalCheckoutState(true, 1, 2,
            new OrderApproval { Status = OrderApprovalStatus.Pending }).Blocked);
        Assert.IsFalse(new OrderApprovalCheckoutState(true, 1, 2,
            new OrderApproval { Status = OrderApprovalStatus.Approved }).Blocked);
    }

    #endregion
}
