using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Customers;

public class SubAccountDeleteCommand : IRequest<(bool success, string error)>
{
    public required Customer CurrentCustomer { get; set; }
    public required string CustomerId { get; set; }
}