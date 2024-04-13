using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Commands.Models.Customers;

public class SubAccountEditCommand : IRequest<bool>
{
    public Customer CurrentCustomer { get; set; }
    public Store Store { get; set; }
    public SubAccountEditModel EditModel { get; set; }
}