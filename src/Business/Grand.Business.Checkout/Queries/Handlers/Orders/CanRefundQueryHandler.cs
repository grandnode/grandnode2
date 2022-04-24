using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class CanRefundQueryHandler : IRequestHandler<CanRefundQuery, bool>
    {
        private readonly IPaymentService _paymentService;

        public CanRefundQueryHandler(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task<bool> Handle(CanRefundQuery request, CancellationToken cancellationToken)
        {
            var paymentTransaction = request.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            if (paymentTransaction.TransactionAmount == 0)
                return false;

            if (paymentTransaction.RefundedAmount > 0)
                return false;

            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            if ((paymentTransaction.TransactionStatus == TransactionStatus.Paid ||
                paymentTransaction.TransactionStatus == TransactionStatus.PartialPaid ||
                paymentTransaction.TransactionStatus == TransactionStatus.PartiallyRefunded) &&
                await _paymentService.SupportRefund(paymentTransaction.PaymentMethodSystemName))
                return true;

            return false;
        }
    }
}
