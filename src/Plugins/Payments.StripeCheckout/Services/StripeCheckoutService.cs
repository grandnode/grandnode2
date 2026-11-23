using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Stripe;
using Stripe.Checkout;

namespace Payments.StripeCheckout.Services;

public class StripeCheckoutService : IStripeCheckoutService
{
    private const string OrderGuidKey = "order_guid";
    private const string RecurringPaymentIdKey = "recurring_payment_id";

    //the first invoice of a subscription pays the order the customer placed, each renewal one cycle; any other
    //(an update or a one-off invoice made in the Stripe dashboard) is not a cycle of the recurring payment
    private const string SubscriptionCreate = "subscription_create";
    private const string SubscriptionCycle = "subscription_cycle";

    //one client for every store: it keeps the connections, and a call to Stripe never holds a webhook, a checkout or
    //a cancel for long; no retry, so a slow Stripe costs one timeout and the webhook is resent or the action repeated
    private static readonly IHttpClient SharedHttpClient = new SystemNetHttpClient(
        new HttpClient { Timeout = StripeCheckoutDefaults.RequestTimeout }, 0);

    private readonly ILogger<StripeCheckoutService> _logger;
    private readonly IMediator _mediator;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly IRecurringPaymentService _recurringPaymentService;
    private readonly ISettingService _settingService;
    private readonly StripeCheckoutPaymentSettings _stripeCheckoutPaymentSettings;
    private readonly IContextAccessor _contextAccessor;

    public StripeCheckoutService(
        IContextAccessor contextAccessor,
        StripeCheckoutPaymentSettings stripeCheckoutPaymentSettings,
        ILogger<StripeCheckoutService> logger,
        IMediator mediator,
        IPaymentTransactionService paymentTransactionService,
        IRecurringPaymentService recurringPaymentService,
        ISettingService settingService)
    {
        _contextAccessor = contextAccessor;
        _stripeCheckoutPaymentSettings = stripeCheckoutPaymentSettings;
        _logger = logger;
        _mediator = mediator;
        _paymentTransactionService = paymentTransactionService;
        _recurringPaymentService = recurringPaymentService;
        _settingService = settingService;
    }

    public async Task<string> CreateRedirectUrl(Order order)
    {
        var session = await CreateUrlSession(order);
        return session.Url;
    }

    public async Task<IList<string>> Refund(PaymentTransaction paymentTransaction, double? amount)
    {
        //a payment staff marked as paid by hand never went through Stripe
        if (string.IsNullOrEmpty(paymentTransaction.AuthorizationTransactionId))
            return ["The payment has no Stripe payment id. Check the Stripe dashboard: if Stripe took the money, refund it there, then record the refund offline."];

        //the refund is made in the panel, whose store is not the order's: the key comes from the
        //transaction's store, as two stores may use two Stripe accounts
        var settings = await _settingService.LoadSetting<StripeCheckoutPaymentSettings>(paymentTransaction.StoreId);
        if (string.IsNullOrEmpty(settings.ApiKey))
            return ["The Stripe API key is not set for the store of this payment."];

        //in the smallest unit, as the session was created with the order total * 100; a full refund
        //leaves the amount to Stripe, which refunds what is left of the charge
        var amountInCents = amount.HasValue ? (long)Math.Round(amount.Value * 100) : (long?)null;
        var recordedInCents = (long)Math.Round(paymentTransaction.RefundedAmount * 100);
        try
        {
            var refundService = new RefundService(CreateStripeClient(settings.ApiKey));
            var refunds = await refundService.ListAsync(new RefundListOptions {
                PaymentIntent = paymentTransaction.AuthorizationTransactionId,
                Limit = 100
            });
            //Stripe keeps an idempotency key for 24 hours only, so a refund Stripe made but the shop did not
            //save is caught here instead: refunding again would pay it out twice
            var refundedInCents = refunds.Data.Where(r => r.Status is not ("failed" or "canceled")).Sum(r => r.Amount);
            if (refundedInCents > recordedInCents)
                return [$"Stripe has refunded {FormatCents(refundedInCents)} of this payment, more than the {FormatCents(recordedInCents)} recorded here. Check the Stripe dashboard and record the missing refund offline."];

            var refund = await refundService.CreateAsync(
                new RefundCreateOptions {
                    PaymentIntent = paymentTransaction.AuthorizationTransactionId,
                    Amount = amountInCents
                },
                //a double submit sends the same key, so Stripe pays out once; any refund made since, a failed
                //one included, changes the key, as Stripe would replay a failure saved under the old one
                new RequestOptions {
                    IdempotencyKey =
                        $"{paymentTransaction.Id}-refund-{refunds.Data.Count}-{amountInCents?.ToString() ?? "full"}"
                });
            //the request that made the refund records it; a replay must not record it a second time
            if (refund.StripeResponse?.Headers != null
                && refund.StripeResponse.Headers.TryGetValues("Idempotent-Replayed", out var replayed)
                && replayed.Contains("true"))
                return [$"Stripe refund {refund.Id} was already made by an earlier request. Reload the payment before refunding again."];
            //a pending refund is accepted: Stripe settles card refunds later
            if (refund.Status is "failed" or "canceled")
                return [$"Stripe refund {refund.Id} {refund.Status}."];
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Stripe refund of order #{OrderCode} failed", paymentTransaction.OrderCode);
            return [e.StripeError?.Message ?? e.Message];
        }
        catch (Exception e)
        {
            //a timeout leaves it unknown whether Stripe refunded; the check above catches a refund made after all
            _logger.LogError(e, "Stripe refund of order #{OrderCode} failed", paymentTransaction.OrderCode);
            return [$"Stripe could not be reached ({e.Message}). Check the Stripe dashboard before refunding again."];
        }

        return [];
    }

    private static string FormatCents(long cents)
    {
        return (cents / 100.0).ToString("0.00", CultureInfo.InvariantCulture);
    }

    public async Task<IList<string>> CancelSubscription(RecurringPayment recurringPayment)
    {
        if (string.IsNullOrEmpty(recurringPayment.ProviderReference))
            return ["The recurring payment has no Stripe subscription id. Check the Stripe dashboard and cancel the subscription there."];

        //cancelled from the panel or My account, or by the shop when the cycles are paid: the key comes from the
        //recurring payment's store, as two stores may use two Stripe accounts
        var settings = await _settingService.LoadSetting<StripeCheckoutPaymentSettings>(recurringPayment.StoreId);
        if (string.IsNullOrEmpty(settings.ApiKey))
            return ["The Stripe API key is not set for the store of this recurring payment."];

        var subscriptionService = new SubscriptionService(CreateStripeClient(settings.ApiKey));
        try
        {
            //at once and without a refund: the cycle paid last stays paid, nothing more is charged
            var subscription = await subscriptionService.CancelAsync(recurringPayment.ProviderReference);
            return subscription.Status == "canceled"
                ? []
                : [$"Stripe left subscription {subscription.Id} {subscription.Status}."];
        }
        catch (StripeException e)
        {
            //Stripe refuses to cancel a subscription twice: one it ended already is what was asked for
            if (await IsSubscriptionCanceled(subscriptionService, recurringPayment.ProviderReference))
                return [];

            _logger.LogError(e, "Stripe subscription {SubscriptionId} could not be cancelled",
                recurringPayment.ProviderReference);
            return [e.StripeError?.Message ?? e.Message];
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Stripe subscription {SubscriptionId} could not be cancelled",
                recurringPayment.ProviderReference);
            return [$"Stripe could not be reached ({e.Message}). Try again, or cancel the subscription in the Stripe dashboard."];
        }
    }

    private async Task<bool> IsSubscriptionCanceled(SubscriptionService subscriptionService, string subscriptionId)
    {
        try
        {
            return (await subscriptionService.GetAsync(subscriptionId)).Status == "canceled";
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Stripe subscription {SubscriptionId} could not be read", subscriptionId);
            return false;
        }
    }

    public async Task<bool> WebHookProcessPayment(string stripeSignature, string json)
    {
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignature,
                _stripeCheckoutPaymentSettings.WebhookEndpointSecret);
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "StripeException");
            return false;
        }

        //a recurring payment event that fails throws, so the webhook answers with an error and Stripe sends it
        //again; the recurring payment commands record each report once
        switch (stripeEvent.Type)
        {
            case EventTypes.PaymentIntentSucceeded:
                await CreatePaymentTransaction(stripeEvent.Data.Object as PaymentIntent);
                return true;
            case EventTypes.CheckoutSessionCompleted:
                await RecordSubscriptionStarted(stripeEvent.Data.Object as Session);
                return true;
            case EventTypes.InvoicePaid:
                await RecordInvoicePaid(stripeEvent.Data.Object as Invoice);
                return true;
            case EventTypes.InvoicePaymentFailed:
                await RecordInvoiceFailed(stripeEvent.Data.Object as Invoice);
                return true;
            case EventTypes.CustomerSubscriptionDeleted:
                await RecordSubscriptionEnded(stripeEvent.Data.Object as Subscription);
                return true;
        }

        return false;
    }

    private async Task RecordSubscriptionStarted(Session session)
    {
        //a one-off payment has no subscription; its payment intent marks the order paid
        if (session is not { Mode: "subscription" } || string.IsNullOrEmpty(session.SubscriptionId) ||
            session.Metadata?.TryGetValue(RecurringPaymentIdKey, out var recurringPaymentId) != true)
            return;

        await _mediator.Send(new RecordRecurringPaymentReferenceCommand {
            RecurringPaymentId = recurringPaymentId, ProviderReference = session.SubscriptionId
        });
    }

    private async Task RecordInvoicePaid(Invoice invoice)
    {
        if (!IsRecurringCycle(invoice, out var subscription, out var recurringPaymentId))
            return;

        //in the smallest unit, as the price was created with the order total * 100
        var amount = invoice.AmountPaid / 100.0;
        if (invoice.BillingReason == SubscriptionCreate)
            await MarkInitialOrderPaid(invoice, subscription.Metadata, amount);

        await _mediator.Send(new RecordRecurringPaymentCycleCommand {
            RecurringPaymentId = recurringPaymentId,
            ProviderReference = subscription.SubscriptionId,
            CycleReference = invoice.Id,
            Amount = amount,
            PaidOnUtc = invoice.StatusTransitions?.PaidAt ?? DateTime.UtcNow,
            //the end of the period the invoice paid for is when Stripe charges the next one
            NextPaymentDateUtc = invoice.Lines?.Data?.FirstOrDefault()?.Period?.End
        });
    }

    private async Task MarkInitialOrderPaid(Invoice invoice, Dictionary<string, string> metadata, double amount)
    {
        if (metadata?.TryGetValue(OrderGuidKey, out var value) != true || !Guid.TryParse(value, out var orderGuid))
        {
            _logger.LogError("Stripe invoice {InvoiceId}: the subscription names no order", invoice.Id);
            return;
        }

        var paymentTransaction = await _paymentTransactionService.GetOrderByGuid(orderGuid);
        if (paymentTransaction == null ||
            !invoice.Currency.Equals(paymentTransaction.CurrencyCode, StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogError("Stripe invoice {InvoiceId}: paymentTransaction is null or currency is not equal",
                invoice.Id);
            return;
        }

        if (!PaysTheOrder(paymentTransaction, invoice.AmountPaid, invoice.Id))
            return;

        //paid by an earlier delivery of this event, or by staff
        if (!await _mediator.Send(new CanMarkPaymentTransactionAsPaidQuery { PaymentTransaction = paymentTransaction }))
            return;

        //a refund of the order goes to the payment intent, which a subscription's invoice does not carry
        var paymentIntentId = await GetInvoicePaymentIntentId(invoice.Id, paymentTransaction.StoreId);
        paymentTransaction.AuthorizationTransactionId = paymentIntentId;
        await _mediator.Send(new MarkAsPaidCommand {
            PaymentTransaction = paymentTransaction,
            CreatedBy = StripeCheckoutDefaults.ProviderSystemName,
            Amount = amount,
            Reference = paymentIntentId ?? invoice.Id
        });
    }

    private async Task<string> GetInvoicePaymentIntentId(string invoiceId, string storeId)
    {
        var settings = await _settingService.LoadSetting<StripeCheckoutPaymentSettings>(storeId);
        var payments = await new InvoicePaymentService(CreateStripeClient(settings.ApiKey))
            .ListAsync(new InvoicePaymentListOptions { Invoice = invoiceId, Status = "paid" });
        return payments.Data.FirstOrDefault()?.Payment?.PaymentIntentId;
    }

    private async Task RecordInvoiceFailed(Invoice invoice)
    {
        if (!IsRecurringCycle(invoice, out var subscription, out var recurringPaymentId))
            return;

        await _mediator.Send(new RecordRecurringPaymentFailureCommand {
            RecurringPaymentId = recurringPaymentId,
            ProviderReference = subscription.SubscriptionId,
            CycleReference = invoice.Id,
            Reason = $"Stripe could not charge invoice {invoice.Number ?? invoice.Id} (attempt {invoice.AttemptCount})"
        });
    }

    private async Task RecordSubscriptionEnded(Subscription subscription)
    {
        if (subscription?.Metadata?.TryGetValue(RecurringPaymentIdKey, out var recurringPaymentId) != true)
            return;

        await _mediator.Send(new RecordRecurringPaymentEndedCommand {
            RecurringPaymentId = recurringPaymentId, ProviderReference = subscription.Id
        });
    }

    /// <summary>
    ///     Whether a payment Stripe reports pays the order: the order is paid with Stripe, and Stripe took what was left
    ///     to pay. A payment the order did not ask for, or a smaller one (an amount changed in the dashboard), leaves it
    ///     unpaid for staff to check.
    /// </summary>
    /// <param name="amountInSmallestUnit">What Stripe took, in the smallest unit of the currency</param>
    private bool PaysTheOrder(PaymentTransaction paymentTransaction, long amountInSmallestUnit, string stripeId)
    {
        if (paymentTransaction.PaymentMethodSystemName != StripeCheckoutDefaults.ProviderSystemName)
        {
            _logger.LogError("Stripe {StripeId}: order {OrderCode} is paid with {PaymentMethod}, not with Stripe",
                stripeId, paymentTransaction.OrderCode, paymentTransaction.PaymentMethodSystemName);
            return false;
        }

        //the session asks for the order total * 100, which Stripe rounds to a whole unit: one unit less is the rounding
        var dueInSmallestUnit = (long)Math.Round(paymentTransaction.AmountLeftToPay() * 100);
        if (amountInSmallestUnit >= dueInSmallestUnit - 1)
            return true;

        _logger.LogError("Stripe {StripeId}: took {Paid} of the {Due} order {OrderCode} has left to pay; the order stays unpaid",
            stripeId, amountInSmallestUnit / 100.0, dueInSmallestUnit / 100.0, paymentTransaction.OrderCode);
        return false;
    }

    private static bool IsRecurringCycle(Invoice invoice, out InvoiceParentSubscriptionDetails subscription,
        out string recurringPaymentId)
    {
        subscription = invoice?.Parent?.SubscriptionDetails;
        recurringPaymentId = null;
        return invoice?.BillingReason is SubscriptionCreate or SubscriptionCycle &&
               !string.IsNullOrEmpty(subscription?.SubscriptionId) &&
               subscription.Metadata?.TryGetValue(RecurringPaymentIdKey, out recurringPaymentId) == true;
    }

    private async Task CreatePaymentTransaction(PaymentIntent paymentIntent)
    {
        //a subscription's payments carry no order: its invoices record them
        if (paymentIntent.Metadata?.ContainsKey(OrderGuidKey) != true)
            return;

        if (paymentIntent.Metadata.TryGetValue(OrderGuidKey, out var order_guid)
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

            if (!PaysTheOrder(paymentTransaction, paymentIntent.Amount, paymentIntent.Id))
                return;

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

        //a recurring payment cancelled before the order was paid leaves an order to pay once
        var recurringPayment = await _recurringPaymentService.GetRecurringPaymentByInitialOrderId(order.Id);
        if (recurringPayment != null && recurringPayment.Status.HasEnded())
            recurringPayment = null;

        var metadata = new Dictionary<string, string> { { OrderGuidKey, order.OrderGuid.ToString() } };
        var options = new SessionCreateOptions {
            LineItems = [
                new SessionLineItemOptions {
                    PriceData = new SessionLineItemPriceDataOptions {
                        UnitAmountDecimal = (decimal?)order.OrderTotal * 100,
                        ProductData = new SessionLineItemPriceDataProductDataOptions {
                            Name = string.Format(_stripeCheckoutPaymentSettings.Line, order.OrderNumber)
                        },
                        Currency = order.CustomerCurrencyCode,
                        //every cycle charges the order total again, as each cycle order is a copy of this one
                        Recurring = recurringPayment == null
                            ? null
                            : new SessionLineItemPriceDataRecurringOptions {
                                Interval = Interval(recurringPayment.CyclePeriodId),
                                IntervalCount = recurringPayment.CycleLength
                            }
                    },
                    Quantity = 1
                }
            ],
            ClientReferenceId = order.Id,
            CustomerEmail = order.CustomerEmail,
            Mode = recurringPayment == null ? "payment" : "subscription",
            SuccessUrl = $"{storeLocation}/orderdetails/{order.Id}",
            CancelUrl = $"{storeLocation}/Plugins/PaymentStripeCheckout/CancelOrder/{order.Id}"
        };
        if (recurringPayment == null)
        {
            options.PaymentIntentData = new SessionPaymentIntentDataOptions { Metadata = metadata };
        }
        else
        {
            //the session names the recurring payment for the subscription's reference, the subscription for its
            //invoices; the total number of cycles stays with the shop, which cancels the subscription when the
            //last one is paid
            metadata[RecurringPaymentIdKey] = recurringPayment.Id;
            options.Metadata = metadata;
            options.SubscriptionData = new SessionSubscriptionDataOptions { Metadata = metadata };
        }
        //the api key is store-specific, so it travels with the client - the static
        //StripeConfiguration.ApiKey is process-wide and two stores checking out at the same time
        //would overwrite each other's credentials
        var service = new SessionService(CreateStripeClient(_stripeCheckoutPaymentSettings.ApiKey));
        var session = await service.CreateAsync(options);

        return session;
    }

    private static string Interval(RecurringCyclePeriod period)
    {
        return period switch {
            RecurringCyclePeriod.Days => "day",
            RecurringCyclePeriod.Weeks => "week",
            RecurringCyclePeriod.Months => "month",
            RecurringCyclePeriod.Years => "year",
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown recurring cycle period")
        };
    }

    protected virtual IStripeClient CreateStripeClient(string apiKey)
    {
        return new StripeClient(apiKey, httpClient: SharedHttpClient);
    }
}