using Grand.Domain.Payments;
using MediatR;
using System.Collections.Generic;

namespace Grand.Business.Checkout.Commands.Models.Orders
{
    public class RefundCommand : IRequest<IList<string>>
    {
        public PaymentTransaction PaymentTransaction { get; set; }
    }
}
