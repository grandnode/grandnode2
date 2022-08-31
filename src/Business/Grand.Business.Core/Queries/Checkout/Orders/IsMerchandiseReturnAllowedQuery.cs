using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Queries.Checkout.Orders
{
    public class IsMerchandiseReturnAllowedQuery : IRequest<bool>
    {
        public Order Order { get; set; }
    }
}
