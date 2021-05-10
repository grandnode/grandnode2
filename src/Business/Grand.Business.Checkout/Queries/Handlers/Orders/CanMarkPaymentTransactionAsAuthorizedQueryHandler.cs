using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class CanMarkPaymentTransactionAsAuthorizedQueryHandler : IRequestHandler<CanMarkPaymentTransactionAsAuthorizedQuery, bool>
    {
        public Task<bool> Handle(CanMarkPaymentTransactionAsAuthorizedQuery request, CancellationToken cancellationToken)
        {
            var paymentTransaction = request.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            if (paymentTransaction.TransactionStatus == TransactionStatus.Canceled)
                return Task.FromResult(false);

            if (paymentTransaction.TransactionStatus == TransactionStatus.Pending)
                return Task.FromResult(true);

            return Task.FromResult(false);
        }
    }
}
