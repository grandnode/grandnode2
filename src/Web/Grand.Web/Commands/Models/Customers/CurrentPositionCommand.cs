using Grand.Domain.Customers;
using Grand.Web.Models.Common;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Customers;

public class CurrentPositionCommand : IRequest<bool>
{
    public Customer Customer { get; set; }
    public LocationModel Model { get; set; }
}