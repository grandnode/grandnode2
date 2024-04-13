using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace Payments.StripeCheckout.Services;

public class StripeCheckoutService : IStripeCheckoutService
{
    private readonly ILogger<StripeCheckoutService> _logger;
    private readonly IMediator _mediator;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly StripeCheckoutPaymentSettings _stripeCheckoutPaymentSettings;
    private readonly IWorkContext _workContext;

    public StripeCheckoutService(
        IWorkContext workContext,
        StripeCheckoutPaymentSettings stripeCheckoutPaymentSettings,
        ILogger<StripeCheckoutService> logger,
        IMediator mediator,
        IPaymentTransactionService paymentTransactionService)
    {
        _workContext = workContext;
        _stripeCheckoutPaymentSettings = stripeCheckoutPaymentSettings;
        _logger = logger;
        _mediator = mediator;
        _paymentTransactionService = paymentTransactionService;
    }

    public async Task<string> CreateRedirectUrl(Order order)
    {
        var session = await CreateUrlSession(order);
        return session.Url;
    }

    public async Task<bool> WebHookProcessPayment(string stripeSignature, string json)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature,
                _stripeCheckoutPaymentSettings.WebhookEndpointSecret);
            // Handle the event
            if (stripeEvent.Type == Events.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                await CreatePaymentTransaction(paymentIntent);
                return true;
            }
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "StripeException");
            return false;
        }

        return false;
    }

    private async Task CreatePaymentTransaction(PaymentIntent paymentIntent)
    {
        if (paymentIntent.Metadata.TryGetValue("order_guid", out var order_guid)
            && Guid.TryParse(order_guid, out var orderGuid))
        {
            var paymentTransaction = await _paymentTransactionService.GetOrderByGuid(orderGuid);
            if (paymentTransaction == null ||
                !paymentIntent.Currency.Equals(paymentTransaction.CurrencyCode,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogError("paymentTransaction is null or currency is not equal");
                return;
            }

            try
            {
                paymentTransaction.AuthorizationTransactionId = paymentIntent.Id;
                paymentTransaction.PaidAmount += paymentIntent.Amount / 100;
                await _mediator.Send(new MarkAsPaidCommand { PaymentTransaction = paymentTransaction });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in CreatePaymentTransaction");
            }
        }
        else
        {
            _logger.LogError("order_guid can't get from metadata or orderGuid is not valid");
        }
    }

    private async Task<Session> CreateUrlSession(Order order)
    {
        StripeConfiguration.ApiKey = _stripeCheckoutPaymentSettings.ApiKey;

        var storeLocation = _workContext.CurrentHost.Url.TrimEnd('/');

        var options = new SessionCreateOptions {
            LineItems = [
                new SessionLineItemOptions {
                    PriceData = new SessionLineItemPriceDataOptions {
                        UnitAmountDecimal = (decimal?)order.OrderTotal * 100,
                        ProductData = new SessionLineItemPriceDataProductDataOptions {
                            Name = string.Format(_stripeCheckoutPaymentSettings.Line, order.OrderNumber)
                        },
                        Currency = order.CustomerCurrencyCode
                    },
                    Quantity = 1
                }
            ],
            ClientReferenceId = order.Id,
            CustomerEmail = order.CustomerEmail,
            PaymentIntentData = new SessionPaymentIntentDataOptions {
                Metadata = new Dictionary<string, string> { { "order_guid", order.OrderGuid.ToString() } }
            },
            Mode = "payment",
            SuccessUrl = $"{storeLocation}/orderdetails/{order.Id}",
            CancelUrl = $"{storeLocation}/Plugins/PaymentStripeCheckout/CancelOrder/{order.Id}"
        };
        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session;
    }
}