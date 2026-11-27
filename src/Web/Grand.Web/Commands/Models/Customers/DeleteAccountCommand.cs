using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Customers;

public class DeleteAccountCommand : IRequest<bool>
{
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Domain.Stores.DomainHost Host { get; set; }
    public string? IpAddress { get; set; }
}