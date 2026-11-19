using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Extensions;

[TestClass]
public class PaymentExtensionsTests
{
    private Mock<IPaymentProvider> _providerMock;

    [TestInitialize]
    public void Init()
    {
        _providerMock = new Mock<IPaymentProvider>();
    }

    [TestMethod]
    public void IsPaymentMethodActive_ReturnExpectedResult()
    {
        var settings = new PaymentSettings();
        _providerMock.Setup(c => c.SystemName).Returns("stripe");
        settings.ActivePaymentProviderSystemNames.Add("stripe");
        Assert.IsTrue(_providerMock.Object.IsPaymentMethodActive(settings));
        _providerMock.Setup(c => c.SystemName).Returns("klarna");
        Assert.IsFalse(_providerMock.Object.IsPaymentMethodActive(settings));
    }

    [TestMethod]
    public void IsPaymentMethodActive_NullSettings_ThrowException()
    {
        PaymentSettings settings = null;
        Assert.ThrowsExactly<ArgumentNullException>(() => _providerMock.Object.IsPaymentMethodActive(settings));
    }

    [TestMethod]
    public void IsPaymentMethodActive_NullProvider_ThrowException()
    {
        IPaymentProvider provider = null;
        var settings = new PaymentSettings();
        Assert.ThrowsExactly<ArgumentNullException>(() => provider.IsPaymentMethodActive(settings));
    }

    [TestMethod]
    public void GetPaymentMethodName_OrderLanguage_ReturnNameInOrderLanguage()
    {
        _providerMock.Setup(c => c.FriendlyName).Returns("Request language");
        _providerMock.Setup(c => c.GetFriendlyName("1")).Returns("Order language");
        var order = new Order { CustomerLanguageId = "1", PaymentMethodSystemName = "Payments.Test" };
        Assert.AreEqual("Order language", _providerMock.Object.GetPaymentMethodName(order));
    }

    [TestMethod]
    public void GetPaymentMethodName_NoOrderLanguage_ReturnFriendlyName()
    {
        _providerMock.Setup(c => c.FriendlyName).Returns("Request language");
        var order = new Order { CustomerLanguageId = null, PaymentMethodSystemName = "Payments.Test" };
        Assert.AreEqual("Request language", _providerMock.Object.GetPaymentMethodName(order));
        _providerMock.Verify(c => c.GetFriendlyName(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void GetPaymentMethodName_NoTranslation_ReturnFriendlyName()
    {
        _providerMock.Setup(c => c.FriendlyName).Returns("Request language");
        _providerMock.Setup(c => c.GetFriendlyName("1")).Returns("");
        var order = new Order { CustomerLanguageId = "1", PaymentMethodSystemName = "Payments.Test" };
        Assert.AreEqual("Request language", _providerMock.Object.GetPaymentMethodName(order));
    }

    [TestMethod]
    public void GetPaymentMethodName_NoName_ReturnSystemName()
    {
        _providerMock.Setup(c => c.FriendlyName).Returns("");
        _providerMock.Setup(c => c.GetFriendlyName("1")).Returns("");
        var order = new Order { CustomerLanguageId = "1", PaymentMethodSystemName = "Payments.Test" };
        Assert.AreEqual("Payments.Test", _providerMock.Object.GetPaymentMethodName(order));
    }

    [TestMethod]
    public void GetPaymentMethodName_NullProvider_ReturnSystemName()
    {
        IPaymentProvider provider = null;
        var order = new Order { PaymentMethodSystemName = "Payments.Test" };
        Assert.AreEqual("Payments.Test", provider.GetPaymentMethodName(order));
    }

    [TestMethod]
    public void AmountLeftToPay_PaidInPartsToTheFullAmount_NothingLeft()
    {
        //0.7 + 0.1 + 0.1 + 0.1 adds up to a hair below 1
        var paymentTransaction = new PaymentTransaction { TransactionAmount = 1, PaidAmount = 0.7 + 0.1 + 0.1 + 0.1 };
        Assert.AreEqual(0, paymentTransaction.AmountLeftToPay());
    }

    [TestMethod]
    public void AmountLeftToPay_KeepsAThirdDecimal()
    {
        var paymentTransaction = new PaymentTransaction { TransactionAmount = 10.005, PaidAmount = 10 };
        Assert.AreEqual(0.005, paymentTransaction.AmountLeftToPay(), 1e-9);
    }

    [TestMethod]
    public void AmountLeftToRefund_IsTheTransactionAmountLessTheRefunds_NeverBelowZero()
    {
        Assert.AreEqual(90, new PaymentTransaction { TransactionAmount = 100, PaidAmount = 40, RefundedAmount = 10 }
            .AmountLeftToRefund());
        Assert.AreEqual(0, new PaymentTransaction { TransactionAmount = 100, RefundedAmount = 120 }.AmountLeftToRefund());
    }

    [TestMethod]
    public void WherePaymentOverdue_MatchesIsPaymentOverdue()
    {
        var now = new DateTime(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);
        var orders = new List<Order> {
            new() { Id = "overdue", PaymentDueDateUtc = now.Date.AddDays(-1), PaymentStatusId = PaymentStatus.Pending },
            new() { Id = "dueToday", PaymentDueDateUtc = now.Date, PaymentStatusId = PaymentStatus.Pending },
            new() { Id = "paid", PaymentDueDateUtc = now.Date.AddDays(-1), PaymentStatusId = PaymentStatus.Paid },
            new() { Id = "paidAtOnce", PaymentStatusId = PaymentStatus.Pending }
        };

        var overdue = orders.AsQueryable().WherePaymentOverdue(now).Select(o => o.Id).ToList();

        CollectionAssert.AreEqual(new[] { "overdue" }, overdue);
        CollectionAssert.AreEqual(orders.Where(o => o.IsPaymentOverdue(now)).Select(o => o.Id).ToList(), overdue);
    }

    [TestMethod]
    public void WherePurchaseOrderNumber_MatchesTheTrimmedNumberExactly()
    {
        var orders = new List<Order> {
            new() { Id = "a", PurchaseOrderNumber = "PO-7" },
            new() { Id = "b", PurchaseOrderNumber = "PO-77" }
        };

        CollectionAssert.AreEqual(new[] { "a" },
            orders.AsQueryable().WherePurchaseOrderNumber(" PO-7 ").Select(o => o.Id).ToList());
    }
}
