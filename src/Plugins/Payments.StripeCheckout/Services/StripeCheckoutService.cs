using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace Payments.StripeCheckout.Services;

public class StripeCheckoutService : IStripeCheckoutService
{
    private readonly ILogger<StripeCheckoutService> _logger;
    private readonly IMediator _mediator;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly ISettingService _settingService;
    private readonly StripeCheckoutPaymentSettings _stripeCheckoutPaymentSettings;
    private readonly IContextAccessor _contextAccessor;

    public StripeCheckoutService(
        IContextAccessor contextAccessor,
        StripeCheckoutPaymentSettings stripeCheckoutPaymentSettings,
        ILogger<StripeCheckoutService> logger,
        IMediator mediator,
        IPaymentTransactionService paymentTransactionService,
        ISettingService settingService)
    {
        _contextAccessor = contextAccessor;
        _stripeCheckoutPaymentSettings = stripeCheckoutPaymentSettings;
        _logger = logger;
        _mediator = mediator;
        _paymentTransactionService = paymentTransactionService;
        _settingService = settingService;
    }

    public async Task<string> CreateRedirectUrl(Order order)
    {
        var session = await CreateUrlSession(order);
        return session.Url;
    }

    public async Task<IList<string>> Refund(PaymentTransaction paymentTransaction, double amount)
    {
        //a payment staff marked as paid by hand never went through Stripe
        if (string.IsNullOrEmpty(paymentTransaction.AuthorizationTransactionId))
            return ["The payment has no Stripe payment id. Check the Stripe dashboard: if Stripe took the money, refund it there, then record the refund offline."];

        //the refund is made in the panel, whose store is not the order's: the key comes from the
        //transaction's store, as two stores may use two Stripe accounts
        var settings = await _settingService.LoadSetting<StripeCheckoutPaymentSettings>(paymentTransaction.StoreId);
        //in the smallest unit, as the session was created with the order total * 100
        var amountInCents = (long)Math.Round(amount * 100);
        try
        {
            var refund = await new RefundService(CreateStripeClient(settings.ApiKey)).CreateAsync(
                new RefundCreateOptions {
                    PaymentIntent = paymentTransaction.AuthorizationTransactionId,
                    Amount = amountInCents
                },
                //a retry after Stripe refunded but the refund was not saved sends the same key, so Stripe
                //returns that refund instead of paying out again; once saved, the refunded amount changes the key
                new RequestOptions {
                    IdempotencyKey = $"{paymentTransaction.Id}-refund-{paymentTransaction.RefundedAmount}-{amountInCents}"
                });
            //a pending refund is accepted: Stripe settles card refunds later
            if (refund.Status is "failed" or "canceled")
                return [$"Stripe refund {refund.Id} {refund.Status}."];
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe refund of order #{OrderCode} failed", paymentTransaction.OrderCode);
            return [e.StripeError?.Message ?? e.Message];
        }

        return [];
    }

    public async Task<bool> WebHookProcessPayment(string stripeSignature, string json)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature,
                _stripeCheckoutPaymentSettings.WebhookEndpointSecret);
            // Handle the event
            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
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
                await _mediator.Send(new MarkAsPaidCommand {
                    PaymentTransaction = paymentTransaction,
                    CreatedBy = StripeCheckoutDefaults.ProviderSystemName,
                    //in the smallest unit, as the session was created with the order total * 100
                    Amount = paymentIntent.Amount / 100.0,
                    Reference = paymentIntent.Id
                });
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
        var storeLocation = _contextAccessor.StoreContext.CurrentHost.Url.TrimEnd('/');

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
        //the api key is store-specific, so it travels with the client - the static
        //StripeConfiguration.ApiKey is process-wide and two stores checking out at the same time
        //would overwrite each other's credentials
        var service = new SessionService(CreateStripeClient(_stripeCheckoutPaymentSettings.ApiKey));
        var session = await service.CreateAsync(options);

        return session;
    }

    protected virtual IStripeClient CreateStripeClient(string apiKey)
    {
        return new StripeClient(apiKey);
    }
}