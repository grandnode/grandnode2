using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Customers;

public class CustomerRegisteredCommand : IRequest<bool>
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Domain.Stores.DomainHost Host { get; set; }
    public required RegisterModel Model { get; set; }
    public required IList<CustomAttribute> CustomerAttributes { get; set; }
}