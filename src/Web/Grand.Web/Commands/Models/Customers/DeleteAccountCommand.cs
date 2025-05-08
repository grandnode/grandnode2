using Grand.Domain.Customers;
using MediatR;

namespace Grand.Web.Commands.Models.Customers;

public class DeleteAccountCommand : IRequest<bool>
{
    public Customer Customer { get; set; }
    public Domain.Stores.Store Store { get; set; }
    public string IpAddress { get; set; }
}