using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class CanRefundOfflineQueryHandler : IRequestHandler<CanRefundOfflineQuery, bool>
{
    public Task<bool> Handle(CanRefundOfflineQuery request, CancellationToken cancellationToken)
    {
        var paymentTransaction = request.PaymentTransaction;
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        if (paymentTransaction.TransactionAmount == 0)
            return Task.FromResult(false);

        //refund cannot be made if previously a partial refund has been already done. only other partial refund can be made in this case
        return paymentTransaction.RefundedAmount > 0
            ? Task.FromResult(false)
            : Task.FromResult(paymentTransaction.TransactionStatus == TransactionStatus.Paid);
    }
}