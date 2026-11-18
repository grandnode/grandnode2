using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

/// <summary>Only a transaction left behind by an abandoned checkout may be deleted. Once an order
/// exists, its payment actions, merchandise-return refunds, gateway callbacks and the customer's
/// re-payment all find the payment through this record, and a transaction that moved money is the
/// only local trace of it.</summary>
public class CanDeletePaymentTransactionQueryHandler(IRepository<Order> orderRepository)
    : IRequestHandler<CanDeletePaymentTransactionQuery, bool>
{
    /// <summary>A checkout writes its order only after the gateway call; a transaction changed more
    /// recently than this may still be getting one.</summary>
    public static readonly TimeSpan CheckoutInProgress = TimeSpan.FromHours(1);

    public async Task<bool> Handle(CanDeletePaymentTransactionQuery request, CancellationToken cancellationToken)
    {
        var paymentTransaction = request.PaymentTransaction;
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        if (paymentTransaction.TransactionStatus != TransactionStatus.Pending
            || paymentTransaction.PaidAmount != 0 || paymentTransaction.RefundedAmount != 0
            || !string.IsNullOrEmpty(paymentTransaction.AuthorizationTransactionId)
            || !string.IsNullOrEmpty(paymentTransaction.CaptureTransactionId))
            return false;

        var lastChangedUtc = paymentTransaction.UpdatedOnUtc ?? paymentTransaction.CreatedOnUtc;
        if (paymentTransaction.Temp && lastChangedUtc > DateTime.UtcNow - CheckoutInProgress)
            return false;

        //a deleted order is gone from the panel, and the placeholders reserving order numbers are deleted ones
        var orderGuid = paymentTransaction.OrderGuid;
        return !await orderRepository.AnyAsync(
            orderRepository.Table.Where(x => x.OrderGuid == orderGuid && !x.Deleted), cancellationToken);
    }
}
