using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Commands.Models.Customers;

public class SubAccountAddCommand : IRequest
{
    public Customer Customer { get; set; }
    public Store Store { get; set; }
    public SubAccountCreateModel Model { get; set; }
}