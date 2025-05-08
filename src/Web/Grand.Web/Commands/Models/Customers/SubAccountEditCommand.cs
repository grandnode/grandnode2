using Grand.Domain.Customers;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Commands.Models.Customers;

public class SubAccountEditCommand : IRequest<bool>
{
    public Customer CurrentCustomer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public SubAccountEditModel EditModel { get; set; }
}