using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Customers;

public class PasswordRecoverySendCommand : IRequest<bool>
{
    public required PasswordRecoveryModel Model { get; set; }
    public Customer? Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
    public required Domain.Stores.DomainHost Host { get; set; }
    public required Language Language { get; set; }
}