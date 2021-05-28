using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Payments;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class CanPartiallyRefundQueryHandler : IRequestHandler<CanPartiallyRefundQuery, bool>
    {
        private readonly IPaymentService _paymentService;

        public CanPartiallyRefundQueryHandler(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task<bool> Handle(CanPartiallyRefundQuery request, CancellationToken cancellationToken)
        {
            var paymentTransaction = request.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            var amountToRefund = request.AmountToRefund;

            if (paymentTransaction.TransactionAmount == 0)
                return false;

            //uncomment the lines below in order to allow this operation for cancelled orders
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            double canBeRefunded = paymentTransaction.TransactionAmount - paymentTransaction.RefundedAmount;
            if (canBeRefunded <= 0)
                return false;

            if (amountToRefund > canBeRefunded)
                return false;

            if ((paymentTransaction.TransactionStatus == TransactionStatus.Paid ||
                paymentTransaction.TransactionStatus == TransactionStatus.PartialPaid ||
                paymentTransaction.TransactionStatus == TransactionStatus.PartiallyRefunded) &&
                await _paymentService.SupportPartiallyRefund(paymentTransaction.PaymentMethodSystemName))
                return true;

            return false;
        }
    }
}
