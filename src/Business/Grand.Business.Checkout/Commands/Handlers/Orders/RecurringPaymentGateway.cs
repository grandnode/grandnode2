using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Domain.Orders;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

/// <summary>
///     What the provider commands do when the gateway reports on a recurring payment the shop already ended
/// </summary>
internal static class RecurringPaymentGateway
{
    public static async Task Stop(IPaymentService paymentService, IOrderService orderService, ILogger logger,
        RecurringPayment recurringPayment)
    {
        IEnumerable<string> errors;
        try
        {
            var result = await paymentService.CancelRecurringPayment(recurringPayment);
            errors = result.Success ? [] : result.Errors;
        }
        catch (Exception e)
        {
            // a throw is treated as a refusal: the report is recorded and a resend is a no-op, so the note is the only trace
            logger.LogError(e, "Recurring payment {RecurringPaymentId}: the payment gateway could not be asked to stop charging",
                recurringPayment.Id);
            errors = [e.Message];
        }

        // the status stays as it is: every cycle is paid or the payment ended, only the gateway schedule may still run
        if (errors.Any())
            await AddNote(orderService, logger, recurringPayment,
                $"The recurring payment is {recurringPayment.Status.ToString().ToLowerInvariant()}, but the payment gateway did not confirm it stopped charging: {string.Join(", ", errors)}");
    }

    public static Task NoteOtherReference(IOrderService orderService, ILogger logger,
        RecurringPayment recurringPayment, string providerReference)
    {
        logger.LogWarning("Recurring payment {RecurringPaymentId} runs on {ProviderReference}, but the payment gateway reported on {ReportedReference}",
            recurringPayment.Id, recurringPayment.ProviderReference, providerReference);
        return AddNote(orderService, logger, recurringPayment,
            $"The payment gateway reported on {providerReference}, but the recurring payment runs on {recurringPayment.ProviderReference}, so nothing was recorded: check the payment gateway for a second subscription charging the customer");
    }

    public static async Task AddNote(IOrderService orderService, ILogger logger, RecurringPayment recurringPayment,
        string note)
    {
        try
        {
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
