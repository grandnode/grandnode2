using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class
    CanMarkPaymentTransactionAsAuthorizedQueryHandler : IRequestHandler<CanMarkPaymentTransactionAsAuthorizedQuery,
    bool>
{
    public Task<bool> Handle(CanMarkPaymentTransactionAsAuthorizedQuery request, CancellationToken cancellationToken)
    {
        var paymentTransaction = request.PaymentTransaction;
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        return paymentTransaction.TransactionStatus == TransactionStatus.Canceled
            ? Task.FromResult(false)
            : Task.FromResult(paymentTransaction.TransactionStatus == TransactionStatus.Pending);
    }
}