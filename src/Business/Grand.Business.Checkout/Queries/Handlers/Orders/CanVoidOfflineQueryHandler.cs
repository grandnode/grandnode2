using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class CanVoidOfflineQueryHandler : IRequestHandler<CanVoidOfflineQuery, bool>
{
    public Task<bool> Handle(CanVoidOfflineQuery request, CancellationToken cancellationToken)
    {
        var paymentTransaction = request.PaymentTransaction;
        ArgumentNullException.ThrowIfNull(paymentTransaction);

        return paymentTransaction.TransactionAmount == 0
            ? Task.FromResult(false)
            : Task.FromResult(paymentTransaction.TransactionStatus == TransactionStatus.Authorized);
    }
}