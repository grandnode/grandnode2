using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Checkout.Queries.Models.Orders
{
    public class IsMerchandiseReturnAllowedQuery : IRequest<bool>
    {
        public Order Order { get; set; }
    }
}
