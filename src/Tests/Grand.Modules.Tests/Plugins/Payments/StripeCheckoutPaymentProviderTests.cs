using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Payments;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payments.StripeCheckout;
using Payments.StripeCheckout.Services;

namespace Grand.Modules.Tests.Plugins.Payments;

[TestClass]
public class StripeCheckoutPaymentProviderTests
{
    private StripeCheckoutPaymentProvider _provider;
    private Mock<IStripeCheckoutService> _stripeCheckoutService;
    private PaymentTransaction _transaction;

    [TestInitialize]
    public void Init()
    {
        _transaction = new PaymentTransaction { AuthorizationTransactionId = "pi_1" };
        _stripeCheckoutService = new Mock<IStripeCheckoutService>();
        _stripeCheckoutService.Setup(s => s.Refund(It.IsAny<PaymentTransaction>(), It.IsAny<double?>()))
            .ReturnsAsync(new List<string>());
        _provider = new StripeCheckoutPaymentProvider(Mock.Of<IRequestTranslationService>(),
            Mock.Of<IOrderService>(), new StripeCheckoutPaymentSettings(), _stripeCheckoutService.Object);
    }

    [TestMethod]
    public async Task SupportRefund_And_SupportPartiallyRefund_AreTrue()
    {
        Assert.IsTrue(await _provider.SupportRefund());
        Assert.IsTrue(await _provider.SupportPartiallyRefund());
        Assert.IsFalse(await _provider.SupportCapture());
        Assert.IsFalse(await _provider.SupportVoid());
    }

    [TestMethod]
    public async Task Refund_Full_LeavesTheAmountToStripeAndSetsRefunded()
    {
        var result = await _provider.Refund(new RefundPaymentRequest {
            PaymentTransaction = _transaction, AmountToRefund = 50, IsPartialRefund = false
        });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(TransactionStatus.Refunded, result.NewTransactionStatus);
        //Stripe refunds what is left of the charge, so a rounding difference cannot make it refuse
        _stripeCheckoutService.Verify(s => s.Refund(_transaction, null), Times.Once);
    }

    [TestMethod]
    public async Task Refund_Partial_LeavesStatusToHandler()
    {
        var result = await _provider.Refund(new RefundPaymentRequest {
            PaymentTransaction = _transaction, AmountToRefund = 20, IsPartialRefund = true
        });

        Assert.IsTrue(result.Success);
        Assert.AreEqual(TransactionStatus.Pending, result.NewTransactionStatus);
        _stripeCheckoutService.Verify(s => s.Refund(_transaction, 20), Times.Once);
    }

    [TestMethod]
    public async Task Refund_ServiceErrors_AreReturned()
    {
        _stripeCheckoutService.Setup(s => s.Refund(_transaction, null))
            .ReturnsAsync(new List<string> { "Charge has already been refunded." });

        var result = await _provider.Refund(new RefundPaymentRequest {
            PaymentTransaction = _transaction, AmountToRefund = 50
        });

        Assert.IsFalse(result.Success);
        CollectionAssert.Contains(result.Errors.ToList(), "Charge has already been refunded.");
    }
}
