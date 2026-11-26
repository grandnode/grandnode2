using Grand.Domain.Customers;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Customers;

public class GetSubAccount : IRequest<SubAccountEditModel?>
{
    public required Customer CurrentCustomer { get; set; }
    public required string CustomerId { get; set; }
}