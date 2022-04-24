using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class CanVoidOfflineQueryHandler : IRequestHandler<CanVoidOfflineQuery, bool>
    {
        public Task<bool> Handle(CanVoidOfflineQuery request, CancellationToken cancellationToken)
        {
            var paymentTransaction = request.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            if (paymentTransaction.TransactionAmount == 0)
                return Task.FromResult(false);

            if (paymentTransaction.TransactionStatus == TransactionStatus.Authorized)
                return Task.FromResult(true);

            return Task.FromResult(false);
        }
    }
}
