using Grand.Domain.Customers;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Customers;

public class SubAccountEditCommand : IRequest<bool>
{
    public required Customer CurrentCustomer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required SubAccountEditModel EditModel { get; set; }
}