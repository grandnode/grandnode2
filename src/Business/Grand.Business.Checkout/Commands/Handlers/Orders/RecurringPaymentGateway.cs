using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Orders;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

/// <summary>
///     What the provider commands do when the gateway reports on a recurring payment the shop already ended, or on
///     another reference than the one it runs on
/// </summary>
internal static class RecurringPaymentGateway
{
    public static async Task Stop(IPaymentService paymentService, IOrderService orderService, ILogger logger,
        RecurringPayment recurringPayment)
    {
        var errors = await AskToStop(paymentService, logger, recurringPayment);

        // the status stays as it is: every cycle is paid or the payment ended, only the gateway schedule may still run
        if (errors.Any())
            await AddNote(orderService, logger, recurringPayment,
                $"The recurring payment is {recurringPayment.Status.ToString().ToLowerInvariant()}, but the payment gateway did not confirm it stopped charging: {string.Join(", ", errors)}");
    }

    /// <summary>
    ///     Stops another subscription the gateway reported on for the same recurring payment (for example a second
    ///     checkout of the order): the recurring payment runs on the reference recorded first, so the other one must
    ///     not charge the customer
    /// </summary>
    public static async Task StopOtherReference(IPaymentService paymentService, IOrderService orderService,
        ILogger logger, RecurringPayment recurringPayment, string? providerReference, string reported,
        string? advice = null)
    {
        logger.LogWarning("Recurring payment {RecurringPaymentId} runs on {ProviderReference}, but the payment gateway reported {Reported} on {ReportedReference}",
            recurringPayment.Id, recurringPayment.ProviderReference, reported, providerReference);

        // only the reference differs: the store and the payment method decide how the provider stops it
        var errors = await AskToStop(paymentService, logger, new RecurringPayment {
            Id = recurringPayment.Id,
            StoreId = recurringPayment.StoreId,
            CustomerId = recurringPayment.CustomerId,
            InitialOrderId = recurringPayment.InitialOrderId,
            PaymentMethodSystemName = recurringPayment.PaymentMethodSystemName,
            Status = recurringPayment.Status,
            ProviderReference = providerReference
        });

        var outcome = errors.Any()
            ? $"could not be stopped ({string.Join(", ", errors)}): cancel it at the payment gateway"
            : "was stopped";
        // a gateway resends its reports, so the same note is written once
        await AddNote(orderService, logger, recurringPayment,
            $"The payment gateway reported {reported} on {providerReference}, but the recurring payment runs on {recurringPayment.ProviderReference}, so nothing was recorded and {providerReference} {outcome}{(advice == null ? "" : $"; {advice}")}",
            once: true);
    }

    private static async Task<IList<string>> AskToStop(IPaymentService paymentService, ILogger logger,
        RecurringPayment recurringPayment)
    {
        try
        {
            var result = await paymentService.CancelRecurringPayment(recurringPayment);
            return result.Success ? [] : result.Errors;
        }
        catch (Exception e)
        {
            // a throw is treated as a refusal: the report is recorded and a resend is a no-op, so the note is the only trace
            logger.LogError(e, "Recurring payment {RecurringPaymentId}: the payment gateway could not be asked to stop charging {ProviderReference}",
                recurringPayment.Id, recurringPayment.ProviderReference);
            return [e.Message];
        }
    }

    public static async Task AddNote(IOrderService orderService, ILogger logger, RecurringPayment recurringPayment,
        string note, bool once = false)
    {
        try
        {
            if (once &&
                (await orderService.GetOrderNotes(recurringPayment.InitialOrderId)).Any(x => x.Note == note))
                return;

            await orderService.InsertOrderNote(new OrderNote {
                OrderId = recurringPayment.InitialOrderId, Note = note, DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
        }
        catch (Exception e)
        {
            // the note is a courtesy to staff; losing it must not hide the original failure or fail a recorded report
            logger.LogError(e, "Recurring payment {RecurringPaymentId}: the order note could not be written: {Note}",
                recurringPayment.Id, note);
        }
    }
}
