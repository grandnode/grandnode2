using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Checkout;

public class GetMinOrderPlaceIntervalValid : IRequest<bool>
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
}