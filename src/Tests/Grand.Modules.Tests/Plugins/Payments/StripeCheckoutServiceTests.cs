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

    [TestMethod]
    public async Task Refund_PostsPaymentIntentAndAmountInCents_WithTheTransactionStoreKey()
    {
        _handler.Respond(HttpStatusCode.OK, """{"id":"re_1","object":"refund","status":"succeeded"}""");

        var errors = await _service.Refund(Transaction(), 19.99);

        Assert.AreEqual(0, errors.Count);
        Assert.AreEqual("/v1/refunds", _handler.Request.RequestUri!.AbsolutePath);
        Assert.AreEqual("Bearer sk_s2", _handler.Request.Headers.Authorization!.ToString());
        StringAssert.Contains(_handler.Body, "payment_intent=pi_1");
        StringAssert.Contains(_handler.Body, "amount=1999");
    }

    [TestMethod]
    public async Task Refund_NoPaymentIntent_ReturnsErrorWithoutCallingStripe()
    {
        var errors = await _service.Refund(Transaction(null), 10);

        Assert.AreEqual(1, errors.Count);
        Assert.IsNull(_handler.Request);
    }

    [TestMethod]
    public async Task Refund_StripeRefusal_ReturnsStripeMessage()
    {
        _handler.Respond(HttpStatusCode.BadRequest,
            """{"error":{"type":"invalid_request_error","message":"Charge has already been refunded."}}""");

        var errors = await _service.Refund(Transaction(), 10);

        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains(errors[0], "Charge has already been refunded.");
    }

    [TestMethod]
    [DataRow("failed")]
    [DataRow("canceled")]
    public async Task Refund_FailedOrCanceledStatus_ReturnsError(string status)
    {
        _handler.Respond(HttpStatusCode.OK, $$"""{"id":"re_1","object":"refund","status":"{{status}}"}""");

        var errors = await _service.Refund(Transaction(), 10);

        Assert.AreEqual(1, errors.Count);
    }

    [TestMethod]
    public async Task Refund_PendingStatus_Succeeds()
    {
        _handler.Respond(HttpStatusCode.OK, """{"id":"re_1","object":"refund","status":"pending"}""");

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
        private string _json;
        private HttpStatusCode _status;

        public HttpRequestMessage Request { get; private set; }
        public string Body { get; private set; }

        public void Respond(HttpStatusCode status, string json)
        {
            _status = status;
            _json = json;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_status) {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            };
        }
    }
}
