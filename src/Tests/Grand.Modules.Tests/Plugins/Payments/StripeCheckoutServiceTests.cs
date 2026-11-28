using System.Net;
using System.Security.Cryptography;
using System.Text;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payments.StripeCheckout;
using Payments.StripeCheckout.Services;
using Stripe;

namespace Grand.Modules.Tests.Plugins.Payments;

[TestClass]
public class StripeCheckoutServiceTests
{
    private const string WebhookSecret = "whsec_test";

    private FakeStripeHandler _handler;
    private Mock<IMediator> _mediator;
    private Mock<IPaymentTransactionService> _paymentTransactionService;
    private Mock<IRecurringPaymentService> _recurringPaymentService;
    private TestStripeCheckoutService _service;
    private Mock<ISettingService> _settingService;

    public StripeCheckoutServiceTests()
    {
        _handler = new FakeStripeHandler();
        _settingService = new Mock<ISettingService>();
        _settingService.Setup(s => s.LoadSetting<StripeCheckoutPaymentSettings>("s2"))
            .ReturnsAsync(new StripeCheckoutPaymentSettings { ApiKey = "sk_s2" });
        _mediator = new Mock<IMediator>();
        _paymentTransactionService = new Mock<IPaymentTransactionService>();
        _recurringPaymentService = new Mock<IRecurringPaymentService>();
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentHost).Returns(new DomainHost { Url = "https://shop.test/" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.StoreContext).Returns(storeContext.Object);
        _service = new TestStripeCheckoutService(_handler, contextAccessor.Object,
            //the injected settings are the current store's, never the transaction's
            new StripeCheckoutPaymentSettings {
                ApiKey = "sk_current", WebhookEndpointSecret = WebhookSecret, Line = "Order {0}"
            },
            NullLogger<StripeCheckoutService>.Instance, _mediator.Object,
            _paymentTransactionService.Object, _recurringPaymentService.Object, _settingService.Object);
    }

    private static PaymentTransaction Transaction(string? paymentIntentId = "pi_1")
    {
        return new PaymentTransaction { StoreId = "s2", AuthorizationTransactionId = paymentIntentId };
    }

    private const string NoRefunds = """{"object":"list","data":[],"has_more":false,"url":"/v1/refunds"}""";
    private const string Succeeded = """{"id":"re_1","object":"refund","status":"succeeded"}""";

    private static string Refunds(params (long amount, string status)[] refunds)
    {
        var data = string.Join(",", refunds.Select((r, i) =>
            $$"""{"id":"re_{{i}}","object":"refund","amount":{{r.amount}},"status":"{{r.status}}"}"""));
        return $$"""{"object":"list","data":[{{data}}],"has_more":false,"url":"/v1/refunds"}""";
    }

    [TestMethod]
    public async Task Refund_PostsPaymentIntentAndAmountInCents_WithTheTransactionStoreKey()
    {
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, NoRefunds);
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK, Succeeded);

        var errors = await _service.Refund(Transaction(), 19.99);

        Assert.AreEqual(0, errors.Count);
        var list = _handler.Requests[0];
        Assert.AreEqual(HttpMethod.Get, list.Request.Method);
        StringAssert.Contains(list.Request.RequestUri!.Query, "payment_intent=pi_1");
        var create = _handler.Requests[1];
        Assert.AreEqual("/v1/refunds", create.Request.RequestUri!.AbsolutePath);
        Assert.AreEqual("Bearer sk_s2", create.Request.Headers.Authorization!.ToString());
        StringAssert.Contains(create.Body, "payment_intent=pi_1");
        StringAssert.Contains(create.Body, "amount=1999");
    }

    [TestMethod]
    public async Task Refund_Full_LeavesTheAmountToStripe()
    {
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, NoRefunds);
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK, Succeeded);

        var errors = await _service.Refund(Transaction(), null);

        Assert.AreEqual(0, errors.Count);
        Assert.IsFalse(_handler.Requests[1].Body.Contains("amount="));
    }

    [TestMethod]
    public async Task Refund_NoPaymentIntent_ReturnsErrorWithoutCallingStripe()
    {
        var errors = await _service.Refund(Transaction(null), 10);

        Assert.AreEqual(1, errors.Count);
        //the money may still be in Stripe (staff marked the order paid before the webhook came)
        StringAssert.Contains(errors[0], "Stripe dashboard");
        Assert.AreEqual(0, _handler.Requests.Count);
    }

    [TestMethod]
    public async Task Refund_NoApiKey_ReturnsErrorWithoutCallingStripe()
    {
        _settingService.Setup(s => s.LoadSetting<StripeCheckoutPaymentSettings>("s2"))
            .ReturnsAsync(new StripeCheckoutPaymentSettings());

        var errors = await _service.Refund(Transaction(), 10);

        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual(0, _handler.Requests.Count);
    }

    [TestMethod]
    public async Task Refund_StripeRefundedMoreThanRecorded_RefusesWithoutRefunding()
    {
        //an earlier refund went through in Stripe but was not saved - a retry a day later included
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, Refunds((1000, "succeeded"), (500, "pending")));
        var transaction = Transaction();
        transaction.RefundedAmount = 10;

        var errors = await _service.Refund(transaction, 10);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains(errors[0], "Stripe dashboard");
        Assert.AreEqual(1, _handler.Requests.Count);
    }

    [TestMethod]
    public async Task Refund_RefundsRecordedOffline_DoNotBlockTheRefund()
    {
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, NoRefunds);
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK, Succeeded);
        var transaction = Transaction();
        transaction.RefundedAmount = 5;

        var errors = await _service.Refund(transaction, 10);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual(2, _handler.Requests.Count);
    }

    [TestMethod]
    public async Task Refund_IdempotencyKey_IsTheTransactionTheRefundsInStripeAndTheAmount()
    {
        //no culture-formatted number in the key: a Polish and an English admin send the same key
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, NoRefunds);
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK, Succeeded);
        var transaction = Transaction();
        transaction.Id = "t1";
        transaction.RefundedAmount = 5.5;

        await _service.Refund(transaction, 10);

        Assert.AreEqual("t1-refund-0-1000",
            _handler.Requests[1].Request.Headers.GetValues("Idempotency-Key").Single());
    }

    [TestMethod]
    public async Task Refund_AfterAFailedRefund_SendsANewIdempotencyKey()
    {
        //Stripe replays the saved response of a key for 24 hours, a failed refund included
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, Refunds((1000, "failed")));
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK, Succeeded);
        var transaction = Transaction();
        transaction.Id = "t1";

        var errors = await _service.Refund(transaction, 10);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual("t1-refund-1-1000",
            _handler.Requests[1].Request.Headers.GetValues("Idempotency-Key").Single());
    }

    [TestMethod]
    public async Task Refund_ReplayedRefund_ReturnsErrorSoItIsNotRecordedTwice()
    {
        //a double submit: both requests send the same key, Stripe pays once and replays the refund to the second
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, NoRefunds);
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK, Succeeded, ("Idempotent-Replayed", "true"));

        var errors = await _service.Refund(Transaction(), 10);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains(errors[0], "re_1");
    }

    [TestMethod]
    public async Task Refund_StripeRefusal_ReturnsStripeMessage()
    {
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, NoRefunds);
        _handler.Respond(HttpMethod.Post, HttpStatusCode.BadRequest,
            """{"error":{"type":"invalid_request_error","message":"Charge has already been refunded."}}""");

        var errors = await _service.Refund(Transaction(), 10);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains(errors[0], "Charge has already been refunded.");
    }

    [TestMethod]
    public async Task Refund_StripeUnreachable_ReturnsErrorInsteadOfThrowing()
    {
        _handler.Fail(new HttpRequestException("No such host is known."));

        var errors = await _service.Refund(Transaction(), 10);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains(errors[0], "Stripe dashboard");
    }

    [TestMethod]
    [DataRow("failed")]
    [DataRow("canceled")]
    public async Task Refund_FailedOrCanceledStatus_ReturnsError(string status)
    {
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, NoRefunds);
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK,
            $$"""{"id":"re_1","object":"refund","status":"{{status}}"}""");

        var errors = await _service.Refund(Transaction(), 10);

        Assert.AreEqual(1, errors.Count);
    }

    [TestMethod]
    public async Task Refund_PendingStatus_Succeeds()
    {
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, NoRefunds);
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK,
            """{"id":"re_1","object":"refund","status":"pending"}""");

        var errors = await _service.Refund(Transaction(), 10);

        Assert.AreEqual(0, errors.Count);
    }

    private static Order Order()
    {
        return new Order {
            Id = "o1", OrderGuid = Guid.Parse("11111111-1111-1111-1111-111111111111"), OrderNumber = 7,
            OrderTotal = 19.99, CustomerCurrencyCode = "USD", CustomerEmail = "c@shop.test"
        };
    }

    private const string Session = """{"id":"cs_1","object":"checkout.session","url":"https://checkout.stripe.test/cs_1"}""";

    [TestMethod]
    public async Task CreateRedirectUrl_StandardOrder_IsAOneOffPaymentNamingTheOrder()
    {
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK, Session);

        Assert.AreEqual("https://checkout.stripe.test/cs_1", await _service.CreateRedirectUrl(Order()));

        var body = Uri.UnescapeDataString(_handler.Requests[0].Body);
        StringAssert.Contains(body, "mode=payment");
        StringAssert.Contains(body, "payment_intent_data[metadata][order_guid]=11111111-1111-1111-1111-111111111111");
        Assert.IsFalse(body.Contains("recurring"));
        Assert.IsFalse(body.Contains("subscription_data"));
    }

    [TestMethod]
    public async Task CreateRedirectUrl_RecurringOrder_StartsASubscriptionOfTheOrderTotalEveryCycle()
    {
        _recurringPaymentService.Setup(x => x.GetRecurringPaymentByInitialOrderId("o1")).ReturnsAsync(
            new RecurringPayment {
                Id = "rp1", CycleLength = 3, CyclePeriodId = RecurringCyclePeriod.Months, TotalCycles = 4
            });
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK, Session);

        await _service.CreateRedirectUrl(Order());

        var body = Uri.UnescapeDataString(_handler.Requests[0].Body);
        StringAssert.Contains(body, "mode=subscription");
        StringAssert.Contains(body, "line_items[0][price_data][recurring][interval]=month");
        StringAssert.Contains(body, "line_items[0][price_data][recurring][interval_count]=3");
        StringAssert.Contains(body, "line_items[0][price_data][unit_amount_decimal]=1999");
        StringAssert.Contains(body, "subscription_data[metadata][recurring_payment_id]=rp1");
        StringAssert.Contains(body, "subscription_data[metadata][order_guid]=11111111-1111-1111-1111-111111111111");
        StringAssert.Contains(body, "metadata[recurring_payment_id]=rp1");
        //Stripe refuses payment intent data on a subscription session
        Assert.IsFalse(body.Contains("payment_intent_data"));
    }

    [TestMethod]
    [DataRow(RecurringCyclePeriod.Days, "day")]
    [DataRow(RecurringCyclePeriod.Weeks, "week")]
    [DataRow(RecurringCyclePeriod.Years, "year")]
    public async Task CreateRedirectUrl_RecurringOrder_MapsTheCyclePeriod(RecurringCyclePeriod period, string interval)
    {
        _recurringPaymentService.Setup(x => x.GetRecurringPaymentByInitialOrderId("o1")).ReturnsAsync(
            new RecurringPayment { Id = "rp1", CycleLength = 1, CyclePeriodId = period });
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK, Session);

        await _service.CreateRedirectUrl(Order());

        StringAssert.Contains(Uri.UnescapeDataString(_handler.Requests[0].Body),
            $"line_items[0][price_data][recurring][interval]={interval}");
    }

    [TestMethod]
    [DataRow(RecurringPaymentStatus.Cancelled)]
    [DataRow(RecurringPaymentStatus.Completed)]
    public async Task CreateRedirectUrl_RecurringPaymentEnded_PaysTheOrderOnce(RecurringPaymentStatus status)
    {
        _recurringPaymentService.Setup(x => x.GetRecurringPaymentByInitialOrderId("o1")).ReturnsAsync(
            new RecurringPayment { Id = "rp1", CycleLength = 1, Status = status });
        _handler.Respond(HttpMethod.Post, HttpStatusCode.OK, Session);

        await _service.CreateRedirectUrl(Order());

        var body = Uri.UnescapeDataString(_handler.Requests[0].Body);
        StringAssert.Contains(body, "mode=payment");
        Assert.IsFalse(body.Contains("recurring"));
    }

    private static RecurringPayment Subscribed(string? subscriptionId = "sub_1")
    {
        return new RecurringPayment { Id = "rp1", StoreId = "s2", ProviderReference = subscriptionId };
    }

    private static string SubscriptionJson(string status)
    {
        return $$"""{"id":"sub_1","object":"subscription","status":"{{status}}"}""";
    }

    [TestMethod]
    public async Task CancelSubscription_CancelsItWithTheRecurringPaymentStoreKey()
    {
        _handler.Respond(HttpMethod.Delete, HttpStatusCode.OK, SubscriptionJson("canceled"));

        var errors = await _service.CancelSubscription(Subscribed());

        Assert.AreEqual(0, errors.Count);
        var request = _handler.Requests.Single().Request;
        Assert.AreEqual(HttpMethod.Delete, request.Method);
        Assert.AreEqual("/v1/subscriptions/sub_1", request.RequestUri!.AbsolutePath);
        Assert.AreEqual("Bearer sk_s2", request.Headers.Authorization!.ToString());
    }

    [TestMethod]
    public async Task CancelSubscription_StripeRefusesOneCancelledAlready_Succeeds()
    {
        _handler.Respond(HttpMethod.Delete, HttpStatusCode.BadRequest,
            """{"error":{"type":"invalid_request_error","message":"No such subscription: 'sub_1'"}}""");
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, SubscriptionJson("canceled"));

        Assert.AreEqual(0, (await _service.CancelSubscription(Subscribed())).Count);
    }

    [TestMethod]
    public async Task CancelSubscription_StripeRefusesOneStillRunning_ReturnsStripeMessage()
    {
        _handler.Respond(HttpMethod.Delete, HttpStatusCode.BadRequest,
            """{"error":{"type":"invalid_request_error","message":"Something is wrong."}}""");
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, SubscriptionJson("active"));

        var errors = await _service.CancelSubscription(Subscribed());

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains(errors[0], "Something is wrong.");
    }

    [TestMethod]
    public async Task CancelSubscription_StripeUnreachable_ReturnsErrorInsteadOfThrowing()
    {
        _handler.Fail(new TaskCanceledException("timeout"));

        var errors = await _service.CancelSubscription(Subscribed());

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains(errors[0], "Stripe dashboard");
    }

    [TestMethod]
    public async Task CancelSubscription_NoSubscriptionOrNoApiKey_ReturnsErrorWithoutCallingStripe()
    {
        Assert.AreEqual(1, (await _service.CancelSubscription(Subscribed(null))).Count);

        _settingService.Setup(s => s.LoadSetting<StripeCheckoutPaymentSettings>("s2"))
            .ReturnsAsync(new StripeCheckoutPaymentSettings());
        Assert.AreEqual(1, (await _service.CancelSubscription(Subscribed())).Count);

        Assert.AreEqual(0, _handler.Requests.Count);
    }

    private static string SignatureFor(string json)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        var signature = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{json}")));
        return $"t={timestamp},v1={signature}";
    }

    private Task<bool> Deliver(string type, string dataObject)
    {
        var json = $$$"""{"id":"evt_1","object":"event","api_version":"{{{StripeConfiguration.ApiVersion}}}","type":"{{{type}}}","data":{"object":{{{dataObject}}}}}""";
        return _service.WebHookProcessPayment(SignatureFor(json), json);
    }

    private const string SubscriptionMetadata =
        """{"recurring_payment_id":"rp1","order_guid":"11111111-1111-1111-1111-111111111111"}""";

    private static string InvoiceJson(string billingReason, string subscriptionMetadata = SubscriptionMetadata)
    {
        //paid on 2026-01-01, the period it paid for ends on 2026-02-01
        return $$$"""
                  {"id":"in_2","object":"invoice","billing_reason":"{{{billingReason}}}","amount_paid":1999,
                   "currency":"usd","number":"INV-2","attempt_count":2,"status_transitions":{"paid_at":1767225600},
                   "lines":{"object":"list","data":[{"id":"il_1","object":"line_item","period":{"start":1767225600,"end":1769904000}}]},
                   "parent":{"type":"subscription_details","subscription_details":{"subscription":"sub_1",
                   "metadata":{{{subscriptionMetadata}}} } } }
                  """;
    }

    [TestMethod]
    public async Task WebHook_InvoicePaidForARenewal_RecordsTheCycle()
    {
        Assert.IsTrue(await Deliver("invoice.paid", InvoiceJson("subscription_cycle")));

        _mediator.Verify(x => x.Send(It.Is<RecordRecurringPaymentCycleCommand>(c =>
            c.RecurringPaymentId == "rp1" && c.ProviderReference == "sub_1" && c.CycleReference == "in_2" &&
            c.Amount == 19.99 && c.PaidOnUtc == new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) &&
            c.NextPaymentDateUtc == new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
            It.IsAny<CancellationToken>()), Times.Once);
        //only the first invoice pays the order the customer placed
        _mediator.Verify(x => x.Send(It.IsAny<MarkAsPaidCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private PaymentTransaction SetupInitialPayment(bool canMarkPaid = true, double amount = 19.99,
        string paymentMethod = StripeCheckoutDefaults.ProviderSystemName)
    {
        var transaction = new PaymentTransaction {
            Id = "t1", StoreId = "s2", CurrencyCode = "USD", TransactionAmount = amount,
            PaymentMethodSystemName = paymentMethod
        };
        _paymentTransactionService.Setup(x => x.GetOrderByGuid(Order().OrderGuid)).ReturnsAsync(transaction);
        _mediator.Setup(x => x.Send(It.IsAny<CanMarkPaymentTransactionAsPaidQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(canMarkPaid);
        return transaction;
    }

    [TestMethod]
    public async Task WebHook_InvoicePaidForTheSubscriptionStart_MarksTheOrderPaidAndRecordsCycleOne()
    {
        var transaction = SetupInitialPayment();
        _handler.Respond(HttpMethod.Get, HttpStatusCode.OK, """
            {"object":"list","data":[{"id":"inpay_1","object":"invoice_payment","status":"paid",
             "payment":{"type":"payment_intent","payment_intent":"pi_9"}}],"has_more":false,"url":"/v1/invoice_payments"}
            """);

        Assert.IsTrue(await Deliver("invoice.paid", InvoiceJson("subscription_create")));

        //a refund goes to the payment intent; the subscription's invoice is read with the order store's key
        Assert.AreEqual("pi_9", transaction.AuthorizationTransactionId);
        var list = _handler.Requests.Single().Request;
        StringAssert.Contains(list.RequestUri!.Query, "invoice=in_2");
        Assert.AreEqual("Bearer sk_s2", list.Headers.Authorization!.ToString());
        _mediator.Verify(x => x.Send(It.Is<MarkAsPaidCommand>(c =>
            c.PaymentTransaction == transaction && c.Amount == 19.99 && c.Reference == "pi_9" &&
            c.CreatedBy == StripeCheckoutDefaults.ProviderSystemName), It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(x => x.Send(It.Is<RecordRecurringPaymentCycleCommand>(c => c.CycleReference == "in_2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    //a smaller payment (an amount changed in the dashboard) leaves the order unpaid for staff to check
    [TestMethod]
    public async Task WebHook_InvoicePaidForTheSubscriptionStart_LessThanTheOrder_OrderLeftUnpaid()
    {
        SetupInitialPayment(amount: 25);

        Assert.IsTrue(await Deliver("invoice.paid", InvoiceJson("subscription_create")));

        _mediator.Verify(x => x.Send(It.IsAny<MarkAsPaidCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(x => x.Send(It.IsAny<RecordRecurringPaymentCycleCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private const string PaymentIntentOfTheOrder =
        """{"id":"pi_5","object":"payment_intent","amount":1999,"currency":"usd","metadata":{"order_guid":"11111111-1111-1111-1111-111111111111"}}""";

    [TestMethod]
    public async Task WebHook_PaymentIntentPaysTheOrder_MarksItPaid()
    {
        var transaction = SetupInitialPayment();

        Assert.IsTrue(await Deliver("payment_intent.succeeded", PaymentIntentOfTheOrder));

        Assert.AreEqual("pi_5", transaction.AuthorizationTransactionId);
        _mediator.Verify(x => x.Send(It.Is<MarkAsPaidCommand>(c =>
            c.PaymentTransaction == transaction && c.Amount == 19.99 && c.Reference == "pi_5"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    //Stripe rounds the order total * 100 to a whole cent, so it may take one cent less
    [TestMethod]
    public async Task WebHook_PaymentIntentOneCentLessByRounding_MarksItPaid()
    {
        SetupInitialPayment(amount: 20);

        Assert.IsTrue(await Deliver("payment_intent.succeeded", PaymentIntentOfTheOrder));

        _mediator.Verify(x => x.Send(It.IsAny<MarkAsPaidCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task WebHook_PaymentIntentLessThanTheOrder_OrderLeftUnpaid()
    {
        SetupInitialPayment(amount: 25);

        Assert.IsTrue(await Deliver("payment_intent.succeeded", PaymentIntentOfTheOrder));

        _mediator.Verify(x => x.Send(It.IsAny<MarkAsPaidCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task WebHook_PaymentIntentForAnOrderPaidWithAnotherMethod_OrderLeftUnpaid()
    {
        SetupInitialPayment(paymentMethod: "Payments.CashOnDelivery");

        Assert.IsTrue(await Deliver("payment_intent.succeeded", PaymentIntentOfTheOrder));

        _mediator.Verify(x => x.Send(It.IsAny<MarkAsPaidCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task WebHook_InvoicePaidForTheSubscriptionStartAgain_RecordsTheCycleWithoutPayingTwice()
    {
        SetupInitialPayment(canMarkPaid: false);

        Assert.IsTrue(await Deliver("invoice.paid", InvoiceJson("subscription_create")));

        _mediator.Verify(x => x.Send(It.IsAny<MarkAsPaidCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.AreEqual(0, _handler.Requests.Count);
        _mediator.Verify(x => x.Send(It.IsAny<RecordRecurringPaymentCycleCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task WebHook_InvoicePaidCommandFails_Throws_SoStripeSendsItAgain()
    {
        _mediator.Setup(x => x.Send(It.IsAny<RecordRecurringPaymentCycleCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database down"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            Deliver("invoice.paid", InvoiceJson("subscription_cycle")));
    }

    [TestMethod]
    [DataRow("subscription_update")]
    [DataRow("manual")]
    public async Task WebHook_InvoiceNotACycle_RecordsNothing(string billingReason)
    {
        await Deliver("invoice.paid", InvoiceJson(billingReason));
        await Deliver("invoice.payment_failed", InvoiceJson(billingReason));

        _mediator.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task WebHook_InvoiceOfASubscriptionTheShopDidNotStart_RecordsNothing()
    {
        await Deliver("invoice.paid", InvoiceJson("subscription_cycle", "{}"));

        _mediator.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task WebHook_InvoicePaymentFailed_RecordsTheFailure()
    {
        Assert.IsTrue(await Deliver("invoice.payment_failed", InvoiceJson("subscription_cycle")));

        _mediator.Verify(x => x.Send(It.Is<RecordRecurringPaymentFailureCommand>(c =>
            c.RecurringPaymentId == "rp1" && c.ProviderReference == "sub_1" && c.CycleReference == "in_2" &&
            c.Reason == "Stripe could not charge invoice INV-2 (attempt 2)"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task WebHook_SubscriptionDeleted_RecordsTheEndWithTheSubscription()
    {
        Assert.IsTrue(await Deliver("customer.subscription.deleted",
            $$$"""{"id":"sub_1","object":"subscription","status":"canceled","metadata":{{{SubscriptionMetadata}}}}"""));

        _mediator.Verify(x => x.Send(It.Is<RecordRecurringPaymentEndedCommand>(c =>
            c.RecurringPaymentId == "rp1" && c.ProviderReference == "sub_1"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task WebHook_SubscriptionCheckoutCompleted_RecordsTheSubscriptionAtOnce()
    {
        Assert.IsTrue(await Deliver("checkout.session.completed",
            $$$"""{"id":"cs_1","object":"checkout.session","mode":"subscription","subscription":"sub_1","metadata":{{{SubscriptionMetadata}}}}"""));

        _mediator.Verify(x => x.Send(It.Is<RecordRecurringPaymentReferenceCommand>(c =>
            c.RecurringPaymentId == "rp1" && c.ProviderReference == "sub_1"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task WebHook_OneOffCheckoutCompleted_RecordsNothing()
    {
        await Deliver("checkout.session.completed",
            """{"id":"cs_1","object":"checkout.session","mode":"payment","metadata":{}}""");

        _mediator.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task WebHook_PaymentIntentOfASubscription_IsLeftToTheInvoice()
    {
        await Deliver("payment_intent.succeeded",
            """{"id":"pi_9","object":"payment_intent","amount":1999,"currency":"usd","metadata":{}}""");

        _paymentTransactionService.VerifyNoOtherCalls();
        _mediator.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task WebHook_BadSignature_IsRefused()
    {
        Assert.IsFalse(await _service.WebHookProcessPayment("t=1,v1=00", """{"id":"evt_1","object":"event"}"""));

        _mediator.VerifyNoOtherCalls();
    }

    private class TestStripeCheckoutService(
        FakeStripeHandler handler,
        IContextAccessor contextAccessor,
        StripeCheckoutPaymentSettings settings,
        Microsoft.Extensions.Logging.ILogger<StripeCheckoutService> logger,
        IMediator mediator,
        IPaymentTransactionService paymentTransactionService,
        IRecurringPaymentService recurringPaymentService,
        ISettingService settingService)
        : StripeCheckoutService(contextAccessor, settings, logger, mediator, paymentTransactionService,
            recurringPaymentService, settingService)
    {
        protected override IStripeClient CreateStripeClient(string apiKey)
        {
            return new StripeClient(apiKey, httpClient: new SystemNetHttpClient(new HttpClient(handler)));
        }
    }

    private class FakeStripeHandler : HttpMessageHandler
    {
        private readonly Dictionary<HttpMethod, (HttpStatusCode status, string json, (string name, string value)[] headers)>
            _responses = new();

        private Exception? _failure;

        public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

        public void Respond(HttpMethod method, HttpStatusCode status, string json,
            params (string name, string value)[] headers)
        {
            _responses[method] = (status, json, headers);
        }

        public void Fail(Exception failure)
        {
            _failure = failure;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add((request,
                request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken)));
            if (_failure != null)
                throw _failure;

            var (status, json, headers) = _responses[request.Method];
            var response = new HttpResponseMessage(status) {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            foreach (var (name, value) in headers)
                response.Headers.Add(name, value);
            return response;
        }
    }
}