using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Stores;
using Grand.Web.Models.Orders;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Orders;

public class GetCustomerLoyaltyPoints : IRequest<CustomerLoyaltyPointsModel>
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public Currency Currency { get; set; }
}