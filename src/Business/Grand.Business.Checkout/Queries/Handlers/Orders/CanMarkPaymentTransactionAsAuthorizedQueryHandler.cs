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
        if (paymentTransaction == null)
            throw new ArgumentNullException(nameof(request.PaymentTransaction));

        return paymentTransaction.TransactionStatus == TransactionStatus.Canceled
            ? Task.FromResult(false)
            : Task.FromResult(paymentTransaction.TransactionStatus == TransactionStatus.Pending);
    }
}