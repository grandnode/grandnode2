using Grand.Business.Core.Extensions;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class CanPartiallyPaidOfflineQueryHandler : IRequestHandler<CanPartiallyPaidOfflineQuery, bool>
{
    public Task<bool> Handle(CanPartiallyPaidOfflineQuery request, CancellationToken cancellationToken)
    {
        var paymentTransaction = request.PaymentTransaction;
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        var amountToPaid = request.AmountToPaid;

        if (paymentTransaction.TransactionAmount == 0)
            return Task.FromResult(false);

        var canBePaid = paymentTransaction.AmountLeftToPay();
        if (canBePaid <= 0)
            return Task.FromResult(false);

        return amountToPaid > canBePaid
            ? Task.FromResult(false)
            : Task.FromResult(paymentTransaction.TransactionStatus is TransactionStatus.PartialPaid
                or TransactionStatus.Pending or TransactionStatus.PartiallyRefunded or TransactionStatus.Refunded);
    }
}