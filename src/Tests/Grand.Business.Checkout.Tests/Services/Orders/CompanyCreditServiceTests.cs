using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class CompanyCreditServiceTests
{
    private const string OwnerId = "owner1";

    private Mock<ICustomerService> _customerServiceMock;
    private IRepository<Order> _orderRepository;
    private CompanyCreditService _service;

    [TestInitialize]
    public void Init()
    {
        _orderRepository = new MongoDBRepositoryTest<Order>();
        _customerServiceMock = new Mock<ICustomerService>();
        _service = new CompanyCreditService(_orderRepository, _customerServiceMock.Object);
    }

    private async Task<Order> AddOrder(double total, double rate = 1, double paid = 0,
        PaymentStatus paymentStatus = PaymentStatus.Pending, string ownerId = OwnerId,
        DateTime? dueDateUtc = null, bool onCredit = true, bool deleted = false,
        OrderStatusSystem orderStatus = OrderStatusSystem.Pending)
    {
        var order = new Order {
            OwnerId = ownerId,
            OrderTotal = total,
            CurrencyRate = rate,
            PaidAmount = paid,
            PaymentStatusId = paymentStatus,
            OrderStatusId = (int)orderStatus,
            Deleted = deleted,
            PaymentDueDateUtc = onCredit ? dueDateUtc ?? DateTime.UtcNow.AddDays(14) : null
        };
        await _orderRepository.InsertAsync(order);
        return order;
    }

    #region Outstanding amount

    [TestMethod]
    public async Task GetOutstandingAmount_SumsUnpaidCreditOrdersInPrimaryCurrency()
    {
        await AddOrder(100);
        await AddOrder(400, 4);
        await AddOrder(50, paid: 20, paymentStatus: PaymentStatus.PartiallyPaid);

        var outstanding = await _service.GetOutstandingAmount(OwnerId);

        Assert.AreEqual(230, outstanding, 0.0001);
    }

    [TestMethod]
    public async Task GetOutstandingAmount_IgnoresPaidCancelledDeletedAndNonCreditOrders()
    {
        await AddOrder(100, paid: 100, paymentStatus: PaymentStatus.Paid);
        await AddOrder(100, orderStatus: OrderStatusSystem.Cancelled);
        await AddOrder(100, deleted: true);
        await AddOrder(100, onCredit: false);

        var outstanding = await _service.GetOutstandingAmount(OwnerId);

        Assert.AreEqual(0, outstanding);
    }

    [TestMethod]
    public async Task GetOutstandingAmount_IgnoresOtherCompanies()
    {
        await AddOrder(100, ownerId: "owner2");
        await AddOrder(70);

        var outstanding = await _service.GetOutstandingAmount(OwnerId);

        Assert.AreEqual(70, outstanding);
    }

    #endregion

    #region Overdue

    [TestMethod]
    public async Task HasOverdue_TrueOnlyWhenUnpaidAndPastDue()
    {
        var now = DateTime.UtcNow;
        await AddOrder(100, dueDateUtc: now.AddDays(-1), paid: 100, paymentStatus: PaymentStatus.Paid);
        await AddOrder(100, dueDateUtc: now.AddDays(1));
        Assert.IsFalse(await _service.HasOverdue(OwnerId, now));

        await AddOrder(100, dueDateUtc: now.AddDays(-1));
        Assert.IsTrue(await _service.HasOverdue(OwnerId, now));
    }

    [TestMethod]
    [DataRow(-1, PaymentStatus.Pending, OrderStatusSystem.Pending, true)]
    [DataRow(-1, PaymentStatus.PartiallyPaid, OrderStatusSystem.Processing, true)]
    [DataRow(-1, PaymentStatus.Paid, OrderStatusSystem.Complete, false)]
    [DataRow(1, PaymentStatus.Pending, OrderStatusSystem.Pending, false)]
    [DataRow(-1, PaymentStatus.Pending, OrderStatusSystem.Cancelled, false)]
    public void IsPaymentOverdue(int dueInDays, PaymentStatus paymentStatus, OrderStatusSystem orderStatus,
        bool expected)
    {
        var now = DateTime.UtcNow;
        var order = new Order {
            PaymentDueDateUtc = now.AddDays(dueInDays),
            PaymentStatusId = paymentStatus,
            OrderStatusId = (int)orderStatus
        };

        Assert.AreEqual(expected, order.IsPaymentOverdue(now));
    }

    [TestMethod]
    public void IsPaymentOverdue_OnTheDueDate_NotYetOverdue()
    {
        //the due date is a calendar day: the buyer has the whole of it to pay
        var order = new Order {
            PaymentDueDateUtc = new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc),
            PaymentStatusId = PaymentStatus.Pending
        };

        Assert.IsFalse(order.IsPaymentOverdue(new DateTime(2026, 11, 13, 23, 59, 0, DateTimeKind.Utc)));
        Assert.IsTrue(order.IsPaymentOverdue(new DateTime(2026, 11, 14, 0, 1, 0, DateTimeKind.Utc)));
    }

    [TestMethod]
    public async Task HasOverdue_OnTheDueDate_False()
    {
        var dueDate = new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);
        await AddOrder(100, dueDateUtc: dueDate);

        Assert.IsFalse(await _service.HasOverdue(OwnerId, dueDate.AddHours(15)));
        Assert.IsTrue(await _service.HasOverdue(OwnerId, dueDate.AddDays(1).AddHours(1)));
    }

    [TestMethod]
    public void IsPaymentOverdue_NotOnCredit_False()
    {
        var order = new Order { PaymentStatusId = PaymentStatus.Pending };

        Assert.IsFalse(order.IsPaymentOverdue(DateTime.UtcNow));
    }

    #endregion

    #region Company credit

    [TestMethod]
    public async Task GetCompanyCredit_SubAccountUsesOwnersNumbers()
    {
        var owner = new Customer { Id = OwnerId, Active = true, CreditLimit = 1000, PaymentTermDays = 30 };
        _customerServiceMock.Setup(x => x.GetCustomerById(OwnerId)).ReturnsAsync(owner);
        await AddOrder(250);

        var credit = await _service.GetCompanyCredit(new Customer { Id = "buyer1", OwnerId = OwnerId, Active = true });

        Assert.IsTrue(credit.Enabled);
        Assert.AreEqual(OwnerId, credit.CompanyOwnerId);
        Assert.AreEqual(1000, credit.CreditLimit);
        Assert.AreEqual(30, credit.PaymentTermDays);
        Assert.AreEqual(250, credit.OutstandingAmount);
        Assert.AreEqual(750, credit.Available);
    }

    [TestMethod]
    public async Task GetCompanyCredit_OwnerUsesOwnNumbers()
    {
        var owner = new Customer { Id = OwnerId, Active = true, CreditLimit = 500, PaymentTermDays = 14 };

        var credit = await _service.GetCompanyCredit(owner);

        Assert.IsTrue(credit.Enabled);
        Assert.AreEqual(OwnerId, credit.CompanyOwnerId);
        _customerServiceMock.Verify(x => x.GetCustomerById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task GetCompanyCredit_InactiveOwner_Disabled()
    {
        var owner = new Customer { Id = OwnerId, Active = false, CreditLimit = 1000, PaymentTermDays = 30 };
        _customerServiceMock.Setup(x => x.GetCustomerById(OwnerId)).ReturnsAsync(owner);

        var credit = await _service.GetCompanyCredit(new Customer { Id = "buyer1", OwnerId = OwnerId, Active = true });

        Assert.IsFalse(credit.Enabled);
        Assert.IsNull(credit.CreditLimit);
        Assert.IsNull(credit.PaymentTermDays);
    }

    [TestMethod]
    public async Task GetCompanyCredit_MissingOwner_Disabled()
    {
        var credit = await _service.GetCompanyCredit(new Customer { Id = "buyer1", OwnerId = OwnerId, Active = true });

        Assert.IsFalse(credit.Enabled);
    }

    [TestMethod]
    public async Task GetCompanyCredit_OnlyLimitSet_Disabled()
    {
        var credit = await _service.GetCompanyCredit(new Customer { Id = OwnerId, Active = true, CreditLimit = 1000 });

        Assert.IsFalse(credit.Enabled);
        Assert.AreEqual(0, credit.Available);
    }

    [TestMethod]
    [DataRow(1000d, 900d, 100d, false, true, true)]
    [DataRow(1000d, 900d, 100.01d, false, true, false)]
    [DataRow(1000d, 0d, 100d, true, true, false)]
    [DataRow(1000d, 0d, 100d, true, false, true)]
    [DataRow(null, 0d, 100d, false, true, false)]
    public void CompanyCredit_Covers(double? limit, double outstanding, double amount, bool hasOverdue,
        bool blockWhenOverdue, bool expected)
    {
        var credit = new CompanyCredit(OwnerId, limit, limit.HasValue ? 30 : null, outstanding, hasOverdue);

        Assert.AreEqual(expected, credit.Covers(amount, blockWhenOverdue));
    }

    #endregion
}
