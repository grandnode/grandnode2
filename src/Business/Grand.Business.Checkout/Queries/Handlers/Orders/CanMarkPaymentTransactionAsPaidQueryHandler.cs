using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders;

public class CanMarkPaymentTransactionAsPaidQueryHandler : IRequestHandler<CanMarkPaymentTransactionAsPaidQuery, bool>
{
    public async Task<bool> Handle(CanMarkPaymentTransactionAsPaidQuery request, CancellationToken cancellationToken)
    {
        var paymentTransaction = request.PaymentTransaction;
        if (paymentTransaction == null)
            throw new ArgumentNullException(nameof(request.PaymentTransaction));

        if (paymentTransaction.TransactionStatus is TransactionStatus.Canceled or TransactionStatus.Paid
            or TransactionStatus.Refunded or TransactionStatus.PartiallyRefunded or TransactionStatus.Voided)
            return false;

        return await Task.FromResult(true);
    }
}