using System.Net;
using System.Text;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Payments;
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
    private FakeStripeHandler _handler;
    private TestStripeCheckoutService _service;
    private Mock<ISettingService> _settingService;

    [TestInitialize]
    public void Init()
    {
        _handler = new FakeStripeHandler();
        _settingService = new Mock<ISettingService>();
        _settingService.Setup(s => s.LoadSetting<StripeCheckoutPaymentSettings>("s2"))
            .ReturnsAsync(new StripeCheckoutPaymentSettings { ApiKey = "sk_s2" });
        _service = new TestStripeCheckoutService(_handler, Mock.Of<IContextAccessor>(),
            //the injected settings are the current store's, never the transaction's
            new StripeCheckoutPaymentSettings { ApiKey = "sk_current" },
            NullLogger<StripeCheckoutService>.Instance, Mock.Of<IMediator>(),
            Mock.Of<IPaymentTransactionService>(), _settingService.Object);
    }

    private static PaymentTransaction Transaction(string paymentIntentId = "pi_1")
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

    private class TestStripeCheckoutService(
        FakeStripeHandler handler,
        IContextAccessor contextAccessor,
        StripeCheckoutPaymentSettings settings,
        Microsoft.Extensions.Logging.ILogger<StripeCheckoutService> logger,
        IMediator mediator,
        IPaymentTransactionService paymentTransactionService,
        ISettingService settingService)
        : StripeCheckoutService(contextAccessor, settings, logger, mediator, paymentTransactionService,
            settingService)
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

        private Exception _failure;

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