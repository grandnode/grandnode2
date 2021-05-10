using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Payments;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class CanPartiallyPaidOfflineQueryHandler : IRequestHandler<CanPartiallyPaidOfflineQuery, bool>
    {
        public Task<bool> Handle(CanPartiallyPaidOfflineQuery request, CancellationToken cancellationToken)
        {
            var paymentTransaction = request.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            var amountToPaid = request.AmountToPaid;

            if (paymentTransaction.TransactionAmount == decimal.Zero)
                return Task.FromResult(false);

            decimal canBePaid = paymentTransaction.TransactionAmount - paymentTransaction.PaidAmount;
            if (canBePaid <= decimal.Zero)
                return Task.FromResult(false);

            if (amountToPaid > canBePaid)
                return Task.FromResult(false);

            if (paymentTransaction.TransactionStatus == TransactionStatus.PartialPaid ||
                paymentTransaction.TransactionStatus == TransactionStatus.Pending ||
                paymentTransaction.TransactionStatus == TransactionStatus.PartiallyRefunded ||
                paymentTransaction.TransactionStatus == TransactionStatus.Refunded 
                )
                return Task.FromResult(true);

            return Task.FromResult(false);
        }
    }
}
