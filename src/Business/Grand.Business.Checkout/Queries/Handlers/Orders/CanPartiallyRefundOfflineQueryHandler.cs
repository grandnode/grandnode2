using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class CanPartiallyRefundOfflineQueryHandler : IRequestHandler<CanPartiallyRefundOfflineQuery, bool>
{
    public Task<bool> Handle(CanPartiallyRefundOfflineQuery request, CancellationToken cancellationToken)
    {
        var paymentTransaction = request.PaymentTransaction;
        if (paymentTransaction == null)
            throw new ArgumentNullException(nameof(request.PaymentTransaction));

        var amountToRefund = request.AmountToRefund;

        if (paymentTransaction.TransactionAmount == 0)
            return Task.FromResult(false);

        var canBeRefunded = paymentTransaction.TransactionAmount - paymentTransaction.RefundedAmount;
        if (canBeRefunded <= 0)
            return Task.FromResult(false);

        return amountToRefund > canBeRefunded
            ? Task.FromResult(false)
            : Task.FromResult(paymentTransaction.TransactionStatus is TransactionStatus.Paid
                or TransactionStatus.PartialPaid or TransactionStatus.PartiallyRefunded);
    }
}