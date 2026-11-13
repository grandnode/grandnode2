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
    /// <summary>
    ///     How long a refund under way is waited for before it is taken as one that never went through: far longer
    ///     than any payment provider call
    /// </summary>
    protected virtual TimeSpan RefundAbandonedAfter => TimeSpan.FromMinutes(15);

    public async Task<MerchandiseReturnRefundQuote> GetQuote(string merchandiseReturnId)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null)
            return null;

        var order = await orderService.GetOrderById(merchandiseReturn.OrderId);
        return order == null ? null : (await Quote(merchandiseReturn, order)).Quote;
    }

    public async Task<string> Refund(string merchandiseReturnId, double amount, bool offline,
        string refundedBy = null)
    {
        var merchandiseReturn = await merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (merchandiseReturn == null)
            return MerchandiseReturnErrors.NotFound;

        try
        {
            //one refund of the order's payment at a time: the payment transaction counts what is left to refund, and
            //two refunds of it at once would both count the same amount
            await using (await resourceLock.Acquire(
                             [MerchandiseReturnExtensions.RefundLockKey(merchandiseReturn.OrderId)]))
            {
                var start = await Start(merchandiseReturn, amount, offline, refundedBy);
                if (start.FinishedBefore != null)
                {
                    //a refund that went through but whose result was not saved: it is completed now, not paid again
                    await Complete(merchandiseReturn, start.FinishedBefore);
                    return MerchandiseReturnErrors.AlreadyRefunded;
                }

                if (start.Error != null)
                    return start.Error;

                var errors = await PayBack(start.Transaction, start.Refund);
                //the payment transaction says whether the money went back, whatever the command answered: it reports
                //an error also when only the order or the messages failed after the provider refunded
                var transaction = await paymentTransactionService.GetById(start.Transaction.Id);
                var wentThrough = WentThrough(transaction, start.Refund);
                var saved = await Finish(merchandiseReturn, start.Refund, wentThrough, errors);

                if (!wentThrough)
                    return MerchandiseReturnErrors.RefundFailed;
                //the money went back but the return does not say so yet: the next refund of it completes it
                if (!saved)
                    return MerchandiseReturnErrors.RefundInProgress;

                await Complete(merchandiseReturn, start.Refund);
                return null;
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
    ///     Checks the refund and saves it on the return as under way before the payment is asked: a refund that waited
    ///     for the lock, or one asked again after a crash, finds it there and pays nothing twice
    /// </summary>
    private async Task<RefundStart> Start(MerchandiseReturn merchandiseReturn, double amount, bool offline,
        string refundedBy)
    {
        var start = new RefundStart();
        start.Error = await UnderOrderLock(merchandiseReturn, async stored =>
        {
            if (stored.Refund != null)
            {
                if (stored.Refund.RefundedOnUtc != null)
                    return MerchandiseReturnErrors.AlreadyRefunded;

                var underWay = await ResolveUnderWay(stored);
                if (underWay == UnderWay.WentThrough)
                {
                    start.FinishedBefore = stored.Refund;
                    return null;
                }

                if (underWay == UnderWay.Running)
                    return MerchandiseReturnErrors.RefundInProgress;
            }

            var order = await orderService.GetOrderById(stored.OrderId);
            if (order == null)
                return MerchandiseReturnErrors.NotFound;

            var (quote, transaction, rounding) = await Quote(stored, order);
            if (quote.Error != null)
                return quote.Error;

            amount = rounding.Round(amount);
            if (amount <= 0 || amount > quote.MaxAmount)
                return MerchandiseReturnErrors.RefundAmountNotAllowed;
            if (offline ? !quote.CanRefundOffline : !quote.CanRefundOnline)
                return MerchandiseReturnErrors.RefundNotAllowed;

            stored.Refund = new MerchandiseReturnRefund {
                ItemsAmount = quote.ItemsAmount,
                ReturnShippingCost = quote.ReturnShippingCost,
                Amount = amount,
                Offline = offline,
                PaymentTransactionId = transaction.Id,
                TransactionRefundedAmount = transaction.RefundedAmount,
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
    ///     A refund of the return found under way, by a call that crashed or still runs: it went through when its
    ///     transaction counts it, and is saved as refunded; it is given up once it is older than any payment call, and
    ///     cleared so the return can be refunded again
    /// </summary>
    private async Task<UnderWay> ResolveUnderWay(MerchandiseReturn stored)
    {
        var refund = stored.Refund;
        var transaction = await paymentTransactionService.GetById(refund.PaymentTransactionId);
        if (WentThrough(transaction, refund))
        {
            refund.RefundedOnUtc = DateTime.UtcNow;
            await merchandiseReturnService.UpdateMerchandiseReturn(stored);
            await InsertStaffNote(stored, $"The refund of {Format(refund.Amount)} started on " +
                                          $"{refund.CreatedOnUtc:u} went through; its result is saved now");
            return UnderWay.WentThrough;
        }

        if (refund.CreatedOnUtc > DateTime.UtcNow - RefundAbandonedAfter)
            return UnderWay.Running;

        stored.Refund = null;
        await merchandiseReturnService.UpdateMerchandiseReturn(stored);
        await InsertStaffNote(stored, $"The refund of {Format(refund.Amount)} started on {refund.CreatedOnUtc:u} " +
                                      "did not go through");
        return UnderWay.Abandoned;
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

        //the refunds of the order's other returns under way are not counted by the transaction yet
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
    ///     Saves the refund as done, or clears it when the money did not go back so the return can be refunded again
    /// </summary>
    /// <returns>false when it could not be saved</returns>
    private async Task<bool> Finish(MerchandiseReturn merchandiseReturn, MerchandiseReturnRefund refund,
        bool wentThrough, IList<string> errors)
    {
        if (errors.Count > 0)
            logger.LogWarning("The refund of merchandise return {MerchandiseReturnId} gave errors (went through: " +
                              "{WentThrough}): {Errors}", merchandiseReturn.Id, wentThrough,
                string.Join("; ", errors));

        var by = refund.RefundedBy ?? "system";
        try
        {
            var error = await UnderOrderLock(merchandiseReturn, async stored =>
            {
                //this call's refund: no other call finishes or clears one before it is given up
                if (stored.Refund is not { RefundedOnUtc: null })
                    return MerchandiseReturnErrors.RefundInProgress;

                if (wentThrough)
                {
                    stored.Refund.RefundedOnUtc = DateTime.UtcNow;
                    await merchandiseReturnService.UpdateMerchandiseReturn(stored);
                    await InsertStaffNote(stored, $"Refunded {Format(refund.Amount)} " +
                                                  $"{(refund.Offline ? "offline" : "through the payment provider")} " +
                                                  $"by {by}");
                    return null;
                }

                stored.Refund = null;
                await merchandiseReturnService.UpdateMerchandiseReturn(stored);
                await InsertStaffNote(stored, $"The refund of {Format(refund.Amount)} by {by} did not go through: " +
                                              (errors.Count > 0 ? string.Join("; ", errors) : "no reason given"));
                return null;
            });
            if (error == null)
                return true;

            logger.LogError("The refund of merchandise return {MerchandiseReturnId} (went through: {WentThrough}) " +
                            "was not saved: {Error}", merchandiseReturn.Id, wentThrough, error);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The refund of merchandise return {MerchandiseReturnId} (went through: " +
                                "{WentThrough}) was not saved", merchandiseReturn.Id, wentThrough);
            return false;
        }
    }

    /// <summary>
    ///     A refunded return is done: a note on the order, and the return completed, which tells the customer
    /// </summary>
    private async Task Complete(MerchandiseReturn merchandiseReturn, MerchandiseReturnRefund refund)
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
                MerchandiseReturnStatus.Completed, changedBy: refund.RefundedBy);
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

    /// <summary>
    ///     Whether the transaction counts the refund: its refunded amount grew by the refund's amount since the refund
    ///     was started
    /// </summary>
    private static bool WentThrough(PaymentTransaction transaction, MerchandiseReturnRefund refund)
    {
        return transaction != null &&
               (decimal)transaction.RefundedAmount - (decimal)refund.TransactionRefundedAmount >=
               (decimal)refund.Amount - 0.000001m;
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

    private enum UnderWay
    {
        WentThrough,
        Running,
        Abandoned
    }

    private sealed class RefundStart
    {
        public string Error { get; set; }
        public MerchandiseReturnRefund Refund { get; set; }
        public PaymentTransaction Transaction { get; set; }

        /// <summary>
        ///     An earlier refund of the return, found under way, that went through
        /// </summary>
        public MerchandiseReturnRefund FinishedBefore { get; set; }
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
