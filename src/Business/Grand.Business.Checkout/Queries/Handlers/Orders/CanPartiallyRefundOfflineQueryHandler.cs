using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Payments;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
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

            double canBeRefunded = paymentTransaction.TransactionAmount - paymentTransaction.RefundedAmount;
            if (canBeRefunded <= 0)
                return Task.FromResult(false);

            if (amountToRefund > canBeRefunded)
                return Task.FromResult(false);

            if (paymentTransaction.TransactionStatus == TransactionStatus.Paid ||
                paymentTransaction.TransactionStatus == TransactionStatus.PartialPaid ||
                paymentTransaction.TransactionStatus == TransactionStatus.PartiallyRefunded)
                return Task.FromResult(true);

            return Task.FromResult(false);
        }
    }
}
