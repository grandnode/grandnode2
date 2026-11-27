using Grand.Domain.Customers;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Customers;

public class SubAccountAddCommand : IRequest
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required SubAccountCreateModel Model { get; set; }
}