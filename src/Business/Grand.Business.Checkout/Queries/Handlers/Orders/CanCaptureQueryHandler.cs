using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain.Payments;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class CanCaptureQueryHandler : IRequestHandler<CanCaptureQuery, bool>
    {
        private readonly IPaymentService _paymentService;

        public CanCaptureQueryHandler(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task<bool> Handle(CanCaptureQuery request, CancellationToken cancellationToken)
        {
            var paymentTransaction = request.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            if (paymentTransaction.TransactionStatus == TransactionStatus.Canceled ||
                paymentTransaction.TransactionStatus == TransactionStatus.Pending)
                return false;

            if (paymentTransaction.TransactionStatus == TransactionStatus.Authorized &&
                await _paymentService.SupportCapture(paymentTransaction.PaymentMethodSystemName))
                return true;

            return false;
        }
    }
}
