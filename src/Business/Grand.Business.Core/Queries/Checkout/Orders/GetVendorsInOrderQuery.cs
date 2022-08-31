using Grand.Domain.Orders;
using Grand.Domain.Vendors;
using MediatR;

namespace Grand.Business.Core.Queries.Checkout.Orders
{
    public class GetVendorsInOrderQuery : IRequest<IList<Vendor>>
    {
        public Order Order { get; set; }
    }
}
