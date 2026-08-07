using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Customers;

public class CustomerRegisteredCommand : IRequest<bool>
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public RegisterModel Model { get; set; }
    public IList<CustomAttribute> CustomerAttributes { get; set; }
}