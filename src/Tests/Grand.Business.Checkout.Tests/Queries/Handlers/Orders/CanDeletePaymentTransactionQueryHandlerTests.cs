using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class CanDeletePaymentTransactionQueryHandlerTests
{
    private CanDeletePaymentTransactionQueryHandler _handler;
    private LiteDBRepositoryMock<Order> _orders;

    public CanDeletePaymentTransactionQueryHandlerTests()
    {
        _orders = new LiteDBRepositoryMock<Order>();
        _handler = new CanDeletePaymentTransactionQueryHandler(_orders);
    }

    //what a failed checkout leaves: pending, temporary, last changed well before now
    private static PaymentTransaction Abandoned()
    {
        return new PaymentTransaction {
            Id = "pt-1", OrderGuid = Guid.NewGuid(), TransactionStatus = TransactionStatus.Pending, Temp = true,
            TransactionAmount = 100, CreatedOnUtc = DateTime.UtcNow.AddHours(-2)
        };
    }

    private Task<bool> Handle(PaymentTransaction paymentTransaction)
    {
        return _handler.Handle(new CanDeletePaymentTransactionQuery { PaymentTransaction = paymentTransaction },
            CancellationToken.None);
    }

    [TestMethod]
    public async Task AbandonedCheckout_CanDelete()
    {
        Assert.IsTrue(await Handle(Abandoned()));
    }

    [TestMethod]
    public async Task WithOrder_CannotDelete()
    {
        var paymentTransaction = Abandoned();
        _orders.Insert(new Order { OrderGuid = paymentTransaction.OrderGuid });

        Assert.IsFalse(await Handle(paymentTransaction));
    }

    [TestMethod]
    public async Task WithDeletedOrder_CanDelete()
    {
        var paymentTransaction = Abandoned();
        paymentTransaction.Temp = false;
        _orders.Insert(new Order { OrderGuid = paymentTransaction.OrderGuid, Deleted = true });

        Assert.IsTrue(await Handle(paymentTransaction));
    }

    [TestMethod]
    public async Task EmptyOrderGuid_IgnoresTheOrderNumberPlaceholder()
    {
        var paymentTransaction = Abandoned();
        paymentTransaction.OrderGuid = Guid.Empty;
        _orders.Insert(new Order { OrderNumber = 1000, Deleted = true });

        Assert.IsTrue(await Handle(paymentTransaction));
    }

    [TestMethod]
    [DataRow(TransactionStatus.Authorized)]
    [DataRow(TransactionStatus.PartialPaid)]
    [DataRow(TransactionStatus.Paid)]
    [DataRow(TransactionStatus.PartiallyRefunded)]
    [DataRow(TransactionStatus.Refunded)]
    [DataRow(TransactionStatus.Voided)]
    [DataRow(TransactionStatus.Canceled)]
    public async Task NotPending_CannotDelete(TransactionStatus status)
    {
        var paymentTransaction = Abandoned();
        paymentTransaction.TransactionStatus = status;

        Assert.IsFalse(await Handle(paymentTransaction));
    }

    [TestMethod]
    public async Task PendingWithMoneyMoved_CannotDelete()
    {
        var paid = Abandoned();
        paid.PaidAmount = 10;
        var refunded = Abandoned();
        refunded.RefundedAmount = 10;
        var authorized = Abandoned();
        authorized.AuthorizationTransactionId = "auth-1";
        var captured = Abandoned();
        captured.CaptureTransactionId = "capture-1";

        Assert.IsFalse(await Handle(paid));
        Assert.IsFalse(await Handle(refunded));
        Assert.IsFalse(await Handle(authorized));
        Assert.IsFalse(await Handle(captured));
    }

    [TestMethod]
    public async Task CheckoutStillInProgress_CannotDelete()
    {
        var created = Abandoned();
        created.CreatedOnUtc = DateTime.UtcNow.AddMinutes(-1);
        //a retry reuses the transaction, so its last change counts, not its creation
        var retried = Abandoned();
        retried.UpdatedOnUtc = DateTime.UtcNow.AddMinutes(-1);

        Assert.IsFalse(await Handle(created));
        Assert.IsFalse(await Handle(retried));
    }

    [TestMethod]
    public async Task PaymentJustRecorded_CannotDelete()
    {
        //the payment is recorded just before the order is written
        var recorded = Abandoned();
        recorded.Temp = false;
        recorded.UpdatedOnUtc = DateTime.UtcNow.AddSeconds(-1);

        Assert.IsFalse(await Handle(recorded));
    }

    [TestMethod]
    public async Task NullTransaction_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => Handle(null!));
    }
}
