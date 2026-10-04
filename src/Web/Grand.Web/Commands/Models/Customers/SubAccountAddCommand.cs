using Grand.Domain.Customers;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Customers;

public class SubAccountAddCommand : IRequest
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public SubAccountCreateModel Model { get; set; }
}