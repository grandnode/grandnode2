using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class MerchandiseReturnRefundServiceTests
{
    private List<string> _locks;
    private Mock<IMediator> _mediator;
    private Mock<IMerchandiseReturnService> _merchandiseReturnService;
    private Order _order;
    private List<MerchandiseReturn> _orderReturns;
    private Mock<IOrderService> _orderService;
    private MerchandiseReturnRefundService _service;
    private List<string> _steps;
    private MerchandiseReturn _stored;
    private PaymentTransaction _transaction;
    private Mock<IMerchandiseReturnWorkflowService> _workflowService;

    [TestInitialize]
    public void Init()
    {
        _steps = [];
        _locks = [];
        _order = new Order {
            Id = "o1", OrderGuid = Guid.NewGuid(), CustomerCurrencyCode = "EUR", OrderTotal = 300
        };
        //a line of 2 for 200 (item discount and tax in the price) and a line of 3 for 10
        _order.OrderItems.Add(new OrderItem {
            Id = "a", Quantity = 2, UnitPriceWithoutDiscInclTax = 120, PriceInclTax = 200, PriceExclTax = 162.6
        });
        _order.OrderItems.Add(new OrderItem { Id = "b", Quantity = 3, PriceInclTax = 10, PriceExclTax = 8.13 });
        _stored = Return("r1", MerchandiseReturnStatus.Received, ("a", 2));
        _orderReturns = [_stored];
        _transaction = new PaymentTransaction {
            Id = "t1", TransactionAmount = 300, TransactionStatus = TransactionStatus.Paid
        };

        _merchandiseReturnService = new Mock<IMerchandiseReturnService>();
        _merchandiseReturnService.Setup(x => x.GetMerchandiseReturnById(It.IsAny<string>()))
            .ReturnsAsync((string id) => _orderReturns.FirstOrDefault(x => x.Id == id));
        _merchandiseReturnService.Setup(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()))
            .Callback<MerchandiseReturn>(x => _steps.Add(x.Refund == null
                ? "save:cleared"
                : x.Refund.RefundedOnUtc == null
                    ? "save:under way"
                    : "save:refunded"))
            .Returns(Task.CompletedTask);
        _orderService = new Mock<IOrderService>();
        _orderService.Setup(x => x.GetOrderById("o1")).ReturnsAsync(_order);
        var paymentTransactionService = new Mock<IPaymentTransactionService>();
        paymentTransactionService.Setup(x => x.GetOrderByGuid(_order.OrderGuid)).ReturnsAsync(() => _transaction);
        paymentTransactionService.Setup(x => x.GetById("t1")).ReturnsAsync(() => _transaction);
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(new Currency
            { CurrencyCode = "EUR", NumberDecimal = 2, MidpointRoundId = MidpointRounding.AwayFromZero });

        _mediator = new Mock<IMediator>();
        _mediator.Setup(x => x.Send(It.IsAny<GetMerchandiseReturnQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new PagedList<MerchandiseReturn>(_orderReturns, 0, int.MaxValue));
        SetupCanRefund(true, true);
        //the payment gives the money back: the transaction counts it
        _mediator.Setup(x => x.Send(It.IsAny<PartiallyRefundCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IList<string>>, CancellationToken>((command, _) =>
            {
                _steps.Add("pay online");
                _transaction.RefundedAmount += ((PartiallyRefundCommand)command).AmountToRefund;
            })
            .ReturnsAsync(new List<string>());
        _mediator.Setup(x => x.Send(It.IsAny<PartiallyRefundOfflineCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<bool>, CancellationToken>((command, _) =>
            {
                _steps.Add("pay offline");
                _transaction.RefundedAmount += ((PartiallyRefundOfflineCommand)command).AmountToRefund;
            })
            .ReturnsAsync(true);

        var resourceLock = new Mock<IResourceLock>();
        resourceLock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<string>, CancellationToken>((keys, _) => _locks.AddRange(keys))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        _workflowService = new Mock<IMerchandiseReturnWorkflowService>();

        _service = new MerchandiseReturnRefundService(_merchandiseReturnService.Object, _workflowService.Object,
            _orderService.Object, paymentTransactionService.Object, currencyService.Object, _mediator.Object,
            resourceLock.Object, NullLogger<MerchandiseReturnRefundService>.Instance);
    }

    private static MerchandiseReturn Return(string id, MerchandiseReturnStatus status,
        params (string OrderItemId, int Quantity)[] items)
    {
        var merchandiseReturn = new MerchandiseReturn
            { Id = id, OrderId = "o1", ReturnNumber = 7, MerchandiseReturnStatus = status };
        foreach (var item in items)
            merchandiseReturn.MerchandiseReturnItems.Add(new MerchandiseReturnItem
                { OrderItemId = item.OrderItemId, Quantity = item.Quantity });
        return merchandiseReturn;
    }

    private static MerchandiseReturn Refunded(string id, params (string OrderItemId, int Quantity)[] items)
    {
        var merchandiseReturn = Return(id, MerchandiseReturnStatus.Completed, items);
        merchandiseReturn.Refund = new MerchandiseReturnRefund
            { Amount = 1, PaymentTransactionId = "t1", RefundedOnUtc = DateTime.UtcNow };
        return merchandiseReturn;
    }

    private void SetupCanRefund(bool online, bool offline)
    {
        _mediator.Setup(x => x.Send(It.IsAny<CanPartiallyRefundQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(online);
        _mediator.Setup(x => x.Send(It.IsAny<CanPartiallyRefundOfflineQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(offline);
    }

    private void VerifyNothingPaid()
    {
        _mediator.Verify(x => x.Send(It.IsAny<PartiallyRefundCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mediator.Verify(x => x.Send(It.IsAny<PartiallyRefundOfflineCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #region Amount

    [TestMethod]
    public async Task GetQuote_FullReturn_IsWhatWasPaidForTheItems_ItemDiscountAndTaxIncluded()
    {
        var quote = await _service.GetQuote(_stored, _order);

        //the price paid incl. tax, not the price before the item discount (2 x 120) nor the one without tax
        Assert.IsNull(quote.Error);
        Assert.AreEqual(200, quote.ItemsAmount);
        Assert.AreEqual(200, quote.Amount);
        Assert.AreEqual(200, quote.MaxAmount);
        Assert.AreEqual("EUR", quote.CurrencyCode);
    }

    [TestMethod]
    public async Task GetQuote_PartialReturn_IsTheShareOfTheLine()
    {
        _stored.MerchandiseReturnItems[0].Quantity = 1;

        var quote = await _service.GetQuote(_stored, _order);

        Assert.AreEqual(100, quote.ItemsAmount);
    }

    [TestMethod]
    public void RefundItemsAmount_ReturnsOneByOne_AddUpToTheLinePriceExactly()
    {
        //10.00 for 3: the cent that cannot be shared evenly goes to one of the returns, never twice or never
        var first = Return("r1", MerchandiseReturnStatus.Received, ("b", 1));
        var second = Return("r2", MerchandiseReturnStatus.Received, ("b", 1));
        var third = Return("r3", MerchandiseReturnStatus.Received, ("b", 1));

        var amounts = new[] {
            first.RefundItemsAmount(_order, [], 2, MidpointRounding.AwayFromZero),
            second.RefundItemsAmount(_order, [first], 2, MidpointRounding.AwayFromZero),
            third.RefundItemsAmount(_order, [first, second], 2, MidpointRounding.AwayFromZero)
        };

        CollectionAssert.AreEqual(new[] { 3.33, 3.34, 3.33 }, amounts);
        Assert.AreEqual(10m, amounts.Sum(x => (decimal)x));
    }

    [TestMethod]
    public void RefundItemsAmount_TwoOfThreeThenOne_AddUpToTheLinePriceExactly()
    {
        var first = Return("r1", MerchandiseReturnStatus.Received, ("b", 2));
        var second = Return("r2", MerchandiseReturnStatus.Received, ("b", 1));

        Assert.AreEqual(6.67, first.RefundItemsAmount(_order, [], 2, MidpointRounding.AwayFromZero));
        Assert.AreEqual(3.33, second.RefundItemsAmount(_order, [first], 2, MidpointRounding.AwayFromZero));
    }

    [TestMethod]
    public void RefundItemsAmount_MoreThanTheLine_NeverMoreThanItsPrice()
    {
        //returns that took more than was ordered, e.g. saved before the quantity was checked
        var before = Return("r0", MerchandiseReturnStatus.Received, ("a", 2));
        var merchandiseReturn = Return("r1", MerchandiseReturnStatus.Received, ("a", 1));

        Assert.AreEqual(0, merchandiseReturn.RefundItemsAmount(_order, [before], 2, MidpointRounding.AwayFromZero));
    }

    [TestMethod]
    public async Task GetQuote_ItemRefundedInAnotherReturn_TakesTheShareLeft()
    {
        _stored.MerchandiseReturnItems[0].Quantity = 1;
        _stored.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "b", Quantity = 1 });
        _orderReturns.Add(Refunded("r0", ("b", 2)));

        var quote = await _service.GetQuote(_stored, _order);

        //100 for one of a; the last of b's 10.00 after 6.67
        Assert.AreEqual(103.33, quote.ItemsAmount);
    }

    [TestMethod]
    public async Task GetQuote_OrderDiscount_IsNotSharedOut_ButNeverMoreThanLeftOnThePayment()
    {
        //the order paid 180 after a discount of the whole order: the items' price is not lowered by it, and the
        //payment caps what goes back
        _order.OrderDiscount = 20;
        _transaction.TransactionAmount = 180;

        var quote = await _service.GetQuote(_stored, _order);

        Assert.AreEqual(200, quote.ItemsAmount);
        Assert.AreEqual(200, quote.Amount);
        Assert.AreEqual(180, quote.MaxAmount);
    }

    [TestMethod]
    public async Task GetQuote_ReturnShippingCost_IsDeductedInTheOrderCurrency()
    {
        //5 in the primary currency at the order's rate of 4.1234
        _stored.ReturnShipping = new MerchandiseReturnShipping { Cost = 5, CurrencyRate = 4.1234 };

        var quote = await _service.GetQuote(_stored, _order);

        Assert.AreEqual(20.62, quote.ReturnShippingCost);
        Assert.AreEqual(179.38, quote.Amount);
        Assert.AreEqual(179.38, quote.MaxAmount);
    }

    [TestMethod]
    public async Task GetQuote_ReturnShippingCostAboveTheItems_NothingToRefund()
    {
        _stored.MerchandiseReturnItems[0].OrderItemId = "b";
        _stored.MerchandiseReturnItems[0].Quantity = 1;
        _stored.ReturnShipping = new MerchandiseReturnShipping { Cost = 5, CurrencyRate = 1 };

        var quote = await _service.GetQuote(_stored, _order);

        Assert.AreEqual(0, quote.Amount);
        Assert.AreEqual(MerchandiseReturnErrors.NothingToRefund, quote.Error);
    }

    [TestMethod]
    public async Task GetQuote_PaymentRefundedAlready_NothingToRefund()
    {
        _transaction.RefundedAmount = 300;

        var quote = await _service.GetQuote(_stored, _order);

        Assert.AreEqual(MerchandiseReturnErrors.NothingToRefund, quote.Error);
    }

    [TestMethod]
    public async Task GetQuote_AnotherReturnsRefundUnderWay_IsNotCountedTwice()
    {
        var other = Return("r0", MerchandiseReturnStatus.Received, ("b", 3));
        other.Refund = new MerchandiseReturnRefund { Amount = 250, PaymentTransactionId = "t1" };
        _orderReturns.Add(other);

        var quote = await _service.GetQuote(_stored, _order);

        Assert.AreEqual(50, quote.MaxAmount);
    }

    [TestMethod]
    public async Task GetQuote_NotReceived_RefundNotAllowed()
    {
        _stored.MerchandiseReturnStatus = MerchandiseReturnStatus.Accepted;

        var quote = await _service.GetQuote(_stored, _order);

        Assert.AreEqual(MerchandiseReturnErrors.RefundNotAllowed, quote.Error);
    }

    [TestMethod]
    public async Task GetQuote_PaymentCannotBeRefundedAtAll_RefundNotAllowed()
    {
        SetupCanRefund(false, false);

        var quote = await _service.GetQuote(_stored, _order);

        Assert.AreEqual(MerchandiseReturnErrors.RefundNotAllowed, quote.Error);
    }

    #endregion

    #region Refund

    [TestMethod]
    public async Task Refund_Online_SavedUnderWayBeforePaying_ThenRefundedAndCompleted()
    {
        var error = await _service.Refund("r1", 200, false, "staff@shop.com");

        Assert.IsNull(error);
        CollectionAssert.AreEqual(new[] { "save:under way", "pay online", "save:refunded" }, _steps);
        _mediator.Verify(x => x.Send(It.Is<PartiallyRefundCommand>(c =>
            c.AmountToRefund == 200 && c.PaymentTransaction.Id == "t1"), It.IsAny<CancellationToken>()), Times.Once);
        Assert.AreEqual(200, _stored.Refund.Amount);
        Assert.AreEqual(200, _stored.Refund.ItemsAmount);
        Assert.AreEqual("t1", _stored.Refund.PaymentTransactionId);
        Assert.AreEqual("staff@shop.com", _stored.Refund.RefundedBy);
        Assert.IsFalse(_stored.Refund.Offline);
        Assert.IsNotNull(_stored.Refund.RefundedOnUtc);
        _workflowService.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Completed, null,
            "staff@shop.com"), Times.Once);
        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == "o1" && n.Note.Contains("#7") && n.Note.Contains("200.00") && !n.DisplayToCustomer)),
            Times.Once);
    }

    [TestMethod]
    public async Task Refund_HoldsTheOrdersRefundLock_ThenItsQuantityLock()
    {
        await _service.Refund("r1", 200, false);

        Assert.AreEqual(MerchandiseReturnExtensions.RefundLockKey("o1"), _locks[0]);
        Assert.AreEqual(MerchandiseReturnExtensions.QuantityLockKey("o1"), _locks[1]);
    }

    [TestMethod]
    public async Task Refund_Offline_UsesTheOfflinePartialRefund()
    {
        SetupCanRefund(false, true);

        var error = await _service.Refund("r1", 200, true);

        Assert.IsNull(error);
        CollectionAssert.AreEqual(new[] { "save:under way", "pay offline", "save:refunded" }, _steps);
        Assert.IsTrue(_stored.Refund.Offline);
        Assert.AreEqual(200, _transaction.RefundedAmount);
    }

    [TestMethod]
    public async Task Refund_OnlineNotSupportedByTheProvider_RefundNotAllowed_NothingPaidOrSaved()
    {
        SetupCanRefund(false, true);

        var error = await _service.Refund("r1", 200, false);

        Assert.AreEqual(MerchandiseReturnErrors.RefundNotAllowed, error);
        VerifyNothingPaid();
        Assert.IsNull(_stored.Refund);
        Assert.AreEqual(0, _steps.Count);
    }

    [TestMethod]
    public async Task Refund_LoweredByTheStaff_RefundsTheLowerAmount_KeepsWhatItCameTo()
    {
        _stored.ReturnShipping = new MerchandiseReturnShipping { Cost = 5, CurrencyRate = 1 };

        var error = await _service.Refund("r1", 150.004, false);

        Assert.IsNull(error);
        Assert.AreEqual(150, _stored.Refund.Amount);
        Assert.AreEqual(200, _stored.Refund.ItemsAmount);
        Assert.AreEqual(5, _stored.Refund.ReturnShippingCost);
        Assert.AreEqual(150, _transaction.RefundedAmount);
    }

    [TestMethod]
    [DataRow(195.01)]
    [DataRow(0.004)]
    [DataRow(0)]
    [DataRow(-5)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    [DataRow(1e30)]
    public async Task Refund_AmountAboveWhatItComesToOrNotAboveZero_Refused_NothingPaid(double amount)
    {
        _stored.ReturnShipping = new MerchandiseReturnShipping { Cost = 5, CurrencyRate = 1 };

        var error = await _service.Refund("r1", amount, false);

        Assert.AreEqual(MerchandiseReturnErrors.RefundAmountNotAllowed, error);
        VerifyNothingPaid();
        Assert.IsNull(_stored.Refund);
    }

    [TestMethod]
    public async Task Refund_AboveWhatIsLeftOnThePayment_Refused()
    {
        _transaction.RefundedAmount = 250;

        var error = await _service.Refund("r1", 60, false);

        Assert.AreEqual(MerchandiseReturnErrors.RefundAmountNotAllowed, error);
        VerifyNothingPaid();
    }

    [TestMethod]
    public async Task Refund_Twice_SecondIsAlreadyRefunded_PaidOnce()
    {
        Assert.IsNull(await _service.Refund("r1", 200, false));

        var error = await _service.Refund("r1", 200, false);

        Assert.AreEqual(MerchandiseReturnErrors.AlreadyRefunded, error);
        _mediator.Verify(x => x.Send(It.IsAny<PartiallyRefundCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.AreEqual(200, _transaction.RefundedAmount);
    }

    [TestMethod]
    public async Task Refund_NotReceived_RefundNotAllowed()
    {
        _stored.MerchandiseReturnStatus = MerchandiseReturnStatus.Completed;

        var error = await _service.Refund("r1", 200, false);

        Assert.AreEqual(MerchandiseReturnErrors.RefundNotAllowed, error);
        VerifyNothingPaid();
    }

    [TestMethod]
    public async Task Refund_UnknownReturn_NotFound()
    {
        Assert.AreEqual(MerchandiseReturnErrors.NotFound, await _service.Refund("nope", 200, false));
    }

    [TestMethod]
    public async Task Refund_PaymentFails_WaitsForTheCheck_LeavesTheReturnReceived_WithANote()
    {
        //a provider's error may come after it gave the money back: the refund is neither cleared nor done
        _mediator.Setup(x => x.Send(It.IsAny<PartiallyRefundCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "card expired" });

        var error = await _service.Refund("r1", 200, false, "staff@shop.com");

        Assert.AreEqual(MerchandiseReturnErrors.RefundUnconfirmed, error);
        Assert.IsTrue(_stored.Refund.OutcomeUnknown);
        Assert.IsNull(_stored.Refund.RefundedOnUtc);
        Assert.IsTrue(_stored.Refund.AwaitsCheck(DateTime.UtcNow));
        CollectionAssert.AreEqual(new[] { "save:under way", "save:under way" }, _steps);
        _workflowService.Verify(x => x.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturnNote(It.Is<MerchandiseReturnNote>(n =>
            n.Note.Contains("card expired") && !n.DisplayToCustomer)), Times.Once);
    }

    [TestMethod]
    public async Task Refund_PaymentThrows_WaitsForTheCheck()
    {
        _mediator.Setup(x => x.Send(It.IsAny<PartiallyRefundCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("timeout"));

        var error = await _service.Refund("r1", 200, false);

        Assert.AreEqual(MerchandiseReturnErrors.RefundUnconfirmed, error);
        Assert.IsTrue(_stored.Refund.OutcomeUnknown);
    }

    [TestMethod]
    public async Task Refund_PaymentFails_IsNotPaidAgain_UntilTheStaffSayTheMoneyDidNotGoBack()
    {
        //e.g. the provider refunded, then saving the transaction failed: it does not count the money
        _mediator.Setup(x => x.Send(It.IsAny<PartiallyRefundCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "database down" });
        await _service.Refund("r1", 200, false);

        var error = await _service.Refund("r1", 200, false);

        Assert.AreEqual(MerchandiseReturnErrors.RefundInProgress, error);
        _mediator.Verify(x => x.Send(It.IsAny<PartiallyRefundCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Refund_UnderWayByAnotherCall_RefundInProgress_NothingPaid()
    {
        _stored.Refund = new MerchandiseReturnRefund {
            Amount = 200, PaymentTransactionId = "t1", CreatedOnUtc = DateTime.UtcNow.AddMinutes(-1)
        };

        var error = await _service.Refund("r1", 200, false);

        Assert.AreEqual(MerchandiseReturnErrors.RefundInProgress, error);
        VerifyNothingPaid();
        Assert.IsNull(_stored.Refund.RefundedOnUtc);
    }

    [TestMethod]
    public async Task Refund_UnderWay_WhileTheTransactionGrewByAnotherRefund_IsNotTakenAsRefunded()
    {
        //another return of the order, or a refund by hand, put the money on the transaction: not this refund's
        _transaction.RefundedAmount = 250;
        _stored.Refund = new MerchandiseReturnRefund {
            Amount = 200, PaymentTransactionId = "t1", CreatedOnUtc = DateTime.UtcNow.AddHours(-1)
        };

        var error = await _service.Refund("r1", 200, false);

        Assert.AreEqual(MerchandiseReturnErrors.RefundInProgress, error);
        VerifyNothingPaid();
        Assert.IsNull(_stored.Refund.RefundedOnUtc);
        _workflowService.Verify(x => x.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Resolve

    private void UnderWay(bool outcomeUnknown, DateTime createdOnUtc)
    {
        _stored.Refund = new MerchandiseReturnRefund {
            ItemsAmount = 200, Amount = 200, PaymentTransactionId = "t1", OutcomeUnknown = outcomeUnknown,
            CreatedOnUtc = createdOnUtc, RefundedBy = "staff@shop.com"
        };
    }

    [TestMethod]
    public async Task ResolveRefund_WentBack_RecordsTheRefund_AndCompletesTheReturn()
    {
        UnderWay(true, DateTime.UtcNow);

        var error = await _service.ResolveRefund("r1", true, "boss@shop.com");

        Assert.IsNull(error);
        Assert.IsNotNull(_stored.Refund.RefundedOnUtc);
        VerifyNothingPaid();
        _workflowService.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Completed, null,
            "boss@shop.com"), Times.Once);
        _orderService.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n => n.Note.Contains("#7"))), Times.Once);
        _merchandiseReturnService.Verify(x => x.InsertMerchandiseReturnNote(It.Is<MerchandiseReturnNote>(n =>
            n.Note.Contains("went back") && n.Note.Contains("boss@shop.com"))), Times.Once);
    }

    [TestMethod]
    public async Task ResolveRefund_DidNotGoBack_ClearsTheRefund_AndTheReturnCanBeRefundedAgain()
    {
        UnderWay(true, DateTime.UtcNow);

        var error = await _service.ResolveRefund("r1", false, "boss@shop.com");

        Assert.IsNull(error);
        Assert.IsNull(_stored.Refund);
        _workflowService.Verify(x => x.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        Assert.IsNull(await _service.Refund("r1", 200, true));
        Assert.AreEqual(200, _transaction.RefundedAmount);
    }

    [TestMethod]
    public async Task ResolveRefund_LeftByACallThatEndedLongAgo_CanBeResolved()
    {
        UnderWay(false, DateTime.UtcNow - MerchandiseReturnExtensions.RefundRunsAtMost - TimeSpan.FromMinutes(1));

        var error = await _service.ResolveRefund("r1", true);

        Assert.IsNull(error);
        Assert.IsNotNull(_stored.Refund.RefundedOnUtc);
    }

    [TestMethod]
    public async Task ResolveRefund_StillRunning_RefundInProgress_ChangesNothing()
    {
        UnderWay(false, DateTime.UtcNow.AddMinutes(-1));

        var error = await _service.ResolveRefund("r1", false);

        Assert.AreEqual(MerchandiseReturnErrors.RefundInProgress, error);
        Assert.IsNotNull(_stored.Refund);
        Assert.AreEqual(0, _steps.Count);
    }

    [TestMethod]
    public async Task ResolveRefund_Refunded_AlreadyRefunded()
    {
        _stored.Refund = new MerchandiseReturnRefund { Amount = 200, RefundedOnUtc = DateTime.UtcNow };

        Assert.AreEqual(MerchandiseReturnErrors.AlreadyRefunded, await _service.ResolveRefund("r1", false));
        Assert.IsNotNull(_stored.Refund);
    }

    [TestMethod]
    public async Task ResolveRefund_NoRefund_RefundNotAllowed()
    {
        Assert.AreEqual(MerchandiseReturnErrors.RefundNotAllowed, await _service.ResolveRefund("r1", true));
        Assert.AreEqual(MerchandiseReturnErrors.NotFound, await _service.ResolveRefund("nope", true));
    }

    #endregion
}
