using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Payments;
using MediatR;

namespace Grand.Business.Checkout.Queries.Handlers.Orders
{
    public class CanVoidQueryHandler : IRequestHandler<CanVoidQuery, bool>
    {
        private readonly IPaymentService _paymentService;

        public CanVoidQueryHandler(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task<bool> Handle(CanVoidQuery request, CancellationToken cancellationToken)
        {
            var paymentTransaction = request.PaymentTransaction;
            if (paymentTransaction == null)
                throw new ArgumentNullException(nameof(request.PaymentTransaction));

            if (paymentTransaction.TransactionAmount == 0)
                return false;

            if (paymentTransaction.TransactionStatus == TransactionStatus.Authorized &&
                await _paymentService.SupportVoid(paymentTransaction.PaymentMethodSystemName))
                return true;

            return false;
        }
    }
}
