using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Grand.Business.Checkout.Services.Orders;

public class MerchandiseReturnRefundService(
    IMerchandiseReturnService merchandiseReturnService,
    IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService,
    IOrderService orderService,
    IPaymentTransactionService paymentTransactionService,
    ICurrencyService currencyService,
    IMediator mediator,
    IResourceLock resourceLock,
    ILogger<MerchandiseReturnRefundService> logger) : IMerchandiseReturnRefundService
{
    public async Task<MerchandiseReturnRefundQuote> GetQuote(MerchandiseReturn merchandiseReturn, Order order)
    {
        ArgumentNullException.ThrowIfNull(merchandiseReturn);
        ArgumentNullException.ThrowIfNull(order);

        return (await Quote(merchandiseReturn, order)).Quote;
    }

    public async Task<string> Refund(string merchandiseReturnId, double amount, bool offline,
        string refundedBy = null)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null)
            return MerchandiseReturnErrors.NotFound;

        return await UnderRefundLock(merchandiseReturn, async () =>
        {
            var start = await Start(merchandiseReturn, amount, offline, refundedBy);
            if (start.Error != null)
                return start.Error;

            var errors = await PayBack(start.Transaction, start.Refund);
            var saved = await Finish(merchandiseReturn, start.Refund, errors);

            //the payment may have given the money back before it failed: the staff check it and say so
            if (errors.Count > 0)
                return MerchandiseReturnErrors.RefundUnconfirmed;
            //the money went back but the return does not say so yet: it waits for the staff to check it
            if (!saved)
                return MerchandiseReturnErrors.RefundInProgress;

            await Complete(merchandiseReturn, start.Refund, start.Refund.RefundedBy);
            return null;
        });
    }

    public async Task<string> ResolveRefund(string merchandiseReturnId, bool wentBack, string checkedBy = null)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null)
            return MerchandiseReturnErrors.NotFound;

        var by = string.IsNullOrWhiteSpace(checkedBy) ? null : checkedBy.Trim();
        return await UnderRefundLock(merchandiseReturn, async () =>
        {
            MerchandiseReturnRefund refunded = null;
            var error = await UnderOrderLock(merchandiseReturn, async stored =>
            {
                var refund = stored.Refund;
                if (refund == null)
                    return MerchandiseReturnErrors.RefundNotAllowed;
                if (refund.RefundedOnUtc != null)
                    return MerchandiseReturnErrors.AlreadyRefunded;
                //a refund still running is settled by its own call
                if (!refund.AwaitsCheck(DateTime.UtcNow))
                    return MerchandiseReturnErrors.RefundInProgress;

                var started = $"The refund of {Format(refund.Amount)} started on {refund.CreatedOnUtc:u}";
                if (wentBack)
                {
                    refund.RefundedOnUtc = DateTime.UtcNow;
                    await merchandiseReturnService.UpdateMerchandiseReturn(stored);
                    await InsertStaffNote(stored, $"{started} went back, as checked by {by ?? "system"}");
                    refunded = refund;
                    return null;
                }

                stored.Refund = null;
                await merchandiseReturnService.UpdateMerchandiseReturn(stored);
                await InsertStaffNote(stored, $"{started} did not go back, as checked by {by ?? "system"}; the " +
                                              "return can be refunded again");
                return null;
            });

            if (refunded != null)
                await Complete(merchandiseReturn, refunded, by);
            return error;
        });
    }

    /// <summary>
    ///     Checks the refund and saves it on the return as under way before the payment is asked: a refund that waited
    ///     for the lock, or one asked again after a crash, finds it there and pays nothing twice
    /// </summary>
    private async Task<RefundStart> Start(MerchandiseReturn merchandiseReturn, double amount, bool offline,
        string refundedBy)
    {
        var start = new RefundStart();
        start.Error = await UnderOrderLock(merchandiseReturn, async stored =>
        {
            //a refund made, under way or waiting for its check is not started again
            if (stored.Refund != null)
                return stored.Refund.RefundedOnUtc != null
                    ? MerchandiseReturnErrors.AlreadyRefunded
                    : MerchandiseReturnErrors.RefundInProgress;

            var order = await orderService.GetOrderById(stored.OrderId);
            if (order == null)
                return MerchandiseReturnErrors.NotFound;

            var (quote, transaction, rounding) = await Quote(stored, order);
            if (quote.Error != null)
                return quote.Error;

            //checked before the rounding as well, which cannot take NaN, infinity or amounts beyond a decimal
            if (double.IsNaN(amount) || amount <= 0 || amount > quote.MaxAmount)
                return MerchandiseReturnErrors.RefundAmountNotAllowed;
            amount = rounding.Round(amount);
            if (amount <= 0)
                return MerchandiseReturnErrors.RefundAmountNotAllowed;
            if (offline ? !quote.CanRefundOffline : !quote.CanRefundOnline)
                return MerchandiseReturnErrors.RefundNotAllowed;

            stored.Refund = new MerchandiseReturnRefund {
                ItemsAmount = quote.ItemsAmount,
                ReturnShippingCost = quote.ReturnShippingCost,
                Amount = amount,
                Offline = offline,
                PaymentTransactionId = transaction.Id,
                RefundedBy = string.IsNullOrWhiteSpace(refundedBy) ? null : refundedBy.Trim(),
                CreatedOnUtc = DateTime.UtcNow
            };
            await merchandiseReturnService.UpdateMerchandiseReturn(stored);
            start.Refund = stored.Refund;
            start.Transaction = transaction;
            return null;
        });
        return start;
    }

    /// <summary>
    ///     What the refund comes to now: the items less the return shipping cost, at most what is left to refund of
    ///     the order's payment
    /// </summary>
    private async Task<(MerchandiseReturnRefundQuote Quote, PaymentTransaction Transaction, Rounding Rounding)> Quote(
        MerchandiseReturn merchandiseReturn, Order order)
    {
        var quote = new MerchandiseReturnRefundQuote { CurrencyCode = order.CustomerCurrencyCode };
        var currency = await currencyService.GetCurrencyByCode(order.CustomerCurrencyCode);
        var rounding = currency == null
            ? new Rounding(2, MidpointRounding.AwayFromZero)
            : new Rounding(currency.NumberDecimal, currency.MidpointRoundId);

        if (merchandiseReturn.Refund != null)
        {
            quote.Error = merchandiseReturn.Refund.RefundedOnUtc != null
                ? MerchandiseReturnErrors.AlreadyRefunded
                : MerchandiseReturnErrors.RefundInProgress;
            return (quote, null, rounding);
        }

        //the money goes back for goods that came back
        if (merchandiseReturn.MerchandiseReturnStatus != MerchandiseReturnStatus.Received)
        {
            quote.Error = MerchandiseReturnErrors.RefundNotAllowed;
            return (quote, null, rounding);
        }

        var refunded = (await mediator.Send(new GetMerchandiseReturnQuery { OrderId = order.Id }))
            .Where(x => x.Id != merchandiseReturn.Id && x.Refund != null)
            .ToList();
        quote.ItemsAmount = merchandiseReturn.RefundItemsAmount(order, refunded, rounding.Decimals,
            rounding.MidpointRounding);
        //the cost is kept in the primary store currency, with the rate of the order's currency
        var shipping = merchandiseReturn.ReturnShipping;
        quote.ReturnShippingCost = shipping == null ? 0 : rounding.Round(shipping.Cost * shipping.CurrencyRate);
        quote.Amount = Math.Max(0, rounding.Round(quote.ItemsAmount - quote.ReturnShippingCost));

        var transaction = await paymentTransactionService.GetOrderByGuid(order.OrderGuid);
        if (transaction == null)
        {
            quote.Error = MerchandiseReturnErrors.RefundNotAllowed;
            return (quote, null, rounding);
        }

        //the refunds of the order's other returns not settled yet are kept off what is left, whether the transaction
        //counts them already or not: what is left is never more than it is
        var underWay = refunded
            .Where(x => x.Refund.RefundedOnUtc == null && x.Refund.PaymentTransactionId == transaction.Id)
            .Sum(x => x.Refund.Amount);
        var left = rounding.Round(transaction.TransactionAmount - transaction.RefundedAmount - underWay);
        quote.MaxAmount = Math.Max(0, Math.Min(quote.Amount, left));
        if (quote.MaxAmount <= 0)
        {
            quote.Error = MerchandiseReturnErrors.NothingToRefund;
            return (quote, transaction, rounding);
        }

        //the payment transaction's own rules: its status, what is left of it, the provider's support
        quote.CanRefundOnline = await mediator.Send(new CanPartiallyRefundQuery
            { PaymentTransaction = transaction, AmountToRefund = quote.MaxAmount });
        quote.CanRefundOffline = await mediator.Send(new CanPartiallyRefundOfflineQuery
            { PaymentTransaction = transaction, AmountToRefund = quote.MaxAmount });
        if (!quote.CanRefundOnline && !quote.CanRefundOffline)
            quote.Error = MerchandiseReturnErrors.RefundNotAllowed;

        return (quote, transaction, rounding);
    }

    /// <summary>
    ///     Gives the money back with the payment transaction's partial refund command
    /// </summary>
    /// <returns>the errors the command gave</returns>
    private async Task<IList<string>> PayBack(PaymentTransaction transaction, MerchandiseReturnRefund refund)
    {
        try
        {
            if (refund.Offline)
            {
                await mediator.Send(new PartiallyRefundOfflineCommand
                    { PaymentTransaction = transaction, AmountToRefund = refund.Amount });
                return [];
            }

            return await mediator.Send(new PartiallyRefundCommand
                { PaymentTransaction = transaction, AmountToRefund = refund.Amount }) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The refund of payment transaction {PaymentTransactionId} failed", transaction.Id);
            return [ex.Message];
        }
    }

    /// <summary>
    ///     Saves the refund as done when the payment gave no errors. With errors it is left waiting for the staff: the
    ///     command reports an error also when the provider refunded and only what came after failed, and nothing else
    ///     tells this refund's money from the order's other refunds
    /// </summary>
    /// <returns>false when it could not be saved</returns>
    private async Task<bool> Finish(MerchandiseReturn merchandiseReturn, MerchandiseReturnRefund refund,
        IList<string> errors)
    {
        var wentBack = errors.Count == 0;
        if (!wentBack)
            logger.LogWarning("The refund of merchandise return {MerchandiseReturnId} gave errors, it waits for " +
                              "the payment to be checked: {Errors}", merchandiseReturn.Id, string.Join("; ", errors));

        var by = refund.RefundedBy ?? "system";
        try
        {
            var error = await UnderOrderLock(merchandiseReturn, async stored =>
            {
                //this call's refund: nothing else settles one before it waits for its check
                if (stored.Refund is not { RefundedOnUtc: null })
                    return MerchandiseReturnErrors.RefundInProgress;

                if (wentBack)
                {
                    stored.Refund.RefundedOnUtc = DateTime.UtcNow;
                    await merchandiseReturnService.UpdateMerchandiseReturn(stored);
                    await InsertStaffNote(stored, $"Refunded {Format(refund.Amount)} " +
                                                  $"{(refund.Offline ? "offline" : "through the payment provider")} " +
                                                  $"by {by}");
                    return null;
                }

                stored.Refund.OutcomeUnknown = true;
                await merchandiseReturnService.UpdateMerchandiseReturn(stored);
                await InsertStaffNote(stored, $"The refund of {Format(refund.Amount)} by {by} gave errors: " +
                                              $"{string.Join("; ", errors)}. Whether the money went back is to be " +
                                              "checked in the payment");
                return null;
            });
            if (error == null)
                return true;

            logger.LogError("The refund of merchandise return {MerchandiseReturnId} (went back: {WentBack}) was " +
                            "not saved: {Error}", merchandiseReturn.Id, wentBack, error);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The refund of merchandise return {MerchandiseReturnId} (went back: {WentBack}) " +
                                "was not saved", merchandiseReturn.Id, wentBack);
            return false;
        }
    }

    /// <summary>
    ///     A refunded return is done: a note on the order, and the return completed, which tells the customer
    /// </summary>
    private async Task Complete(MerchandiseReturn merchandiseReturn, MerchandiseReturnRefund refund,
        string changedBy)
    {
        try
        {
            await orderService.InsertOrderNote(new OrderNote {
                OrderId = merchandiseReturn.OrderId,
                Note = $"Merchandise return #{merchandiseReturn.ReturnNumber} refunded: {Format(refund.Amount)} " +
                       $"(items {Format(refund.ItemsAmount)}, return shipping {Format(refund.ReturnShippingCost)}), " +
                       (refund.Offline ? "offline" : "through the payment provider"),
                DisplayToCustomer = false
            });

            var error = await merchandiseReturnWorkflowService.ChangeStatus(merchandiseReturn.Id,
                MerchandiseReturnStatus.Completed, changedBy: changedBy);
            if (error != null)
                logger.LogWarning("Merchandise return {MerchandiseReturnId} was refunded, but not completed: {Error}",
                    merchandiseReturn.Id, error);
        }
        catch (Exception ex)
        {
            //the money went back and the return says so: what is left is for the staff to finish by hand
            logger.LogError(ex, "Merchandise return {MerchandiseReturnId} was refunded, but its order note or its " +
                                "completion failed", merchandiseReturn.Id);
        }
    }

    private static string Format(double amount)
    {
        return amount.ToString("0.00##", CultureInfo.InvariantCulture);
    }

    private Task InsertStaffNote(MerchandiseReturn merchandiseReturn, string note)
    {
        return merchandiseReturnService.InsertMerchandiseReturnNote(new MerchandiseReturnNote {
            MerchandiseReturnId = merchandiseReturn.Id,
            Note = note,
            DisplayToCustomer = false
        });
    }

    /// <summary>
    ///     Runs the refund once the refund lock of the order is held: one refund of the order's payment at a time, as
    ///     the partial refund commands read the transaction's refunded amount and write it back. The lease of the lock
    ///     is short; a provider slower than it may let a second refund of the order start, which still finds the first
    ///     under way and keeps its amount off what is left
    /// </summary>
    private async Task<string> UnderRefundLock(MerchandiseReturn merchandiseReturn, Func<Task<string>> refund)
    {
        try
        {
            await using (await resourceLock.Acquire(
                             [MerchandiseReturnExtensions.RefundLockKey(merchandiseReturn.OrderId)]))
            {
                return await refund();
            }
        }
        catch (ResourceLockTimeoutException ex)
        {
            logger.LogWarning(ex, "The payment of order {OrderId} stayed in use by another refund",
                merchandiseReturn.OrderId);
            return MerchandiseReturnErrors.Busy;
        }
    }

    /// <summary>
    ///     Runs the change on the return as stored once the lock of its order is held, which every writer of the
    ///     order's returns holds. Always taken inside the refund lock, never the other way round
    /// </summary>
    private async Task<string> UnderOrderLock(MerchandiseReturn merchandiseReturn,
        Func<MerchandiseReturn, Task<string>> change)
    {
        try
        {
            await using (await resourceLock.Acquire(
                             [MerchandiseReturnExtensions.QuantityLockKey(merchandiseReturn.OrderId)]))
            {
                var stored = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturn.Id);
                if (stored == null)
                    return MerchandiseReturnErrors.NotFound;
                return await change(stored);
            }
        }
        catch (ResourceLockTimeoutException ex)
        {
            logger.LogWarning(ex, "The returns of order {OrderId} stayed in use by another change",
                merchandiseReturn.OrderId);
            return MerchandiseReturnErrors.Busy;
        }
    }

    private sealed class RefundStart
    {
        public string Error { get; set; }
        public MerchandiseReturnRefund Refund { get; set; }
        public PaymentTransaction Transaction { get; set; }
    }

    /// <summary>
    ///     The rounding of the order's currency, without the cash rounding: money goes back to the cent
    /// </summary>
    private sealed record Rounding(int Decimals, MidpointRounding MidpointRounding)
    {
        public double Round(double value)
        {
            return (double)Math.Round((decimal)value, Decimals, MidpointRounding);
        }
    }
}
