using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Payments;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class CanMarkPaymentTransactionAsPaidQueryHandler : IRequestHandler<CanMarkPaymentTransactionAsPaidQuery, bool>
    {
        public async Task<bool> Handle(CanMarkPaymentTransactionAsPaidQuery request, CancellationToken cancellationToken)
        {
            var paymentTransaction = request.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            if (paymentTransaction.TransactionStatus == TransactionStatus.Canceled)
                return false;

            if (paymentTransaction.TransactionStatus == TransactionStatus.Paid ||                
                paymentTransaction.TransactionStatus == TransactionStatus.Refunded ||
                paymentTransaction.TransactionStatus == TransactionStatus.PartiallyRefunded ||
                paymentTransaction.TransactionStatus == TransactionStatus.Voided)
                return false;

            return await Task.FromResult(true);
        }
    }
}
