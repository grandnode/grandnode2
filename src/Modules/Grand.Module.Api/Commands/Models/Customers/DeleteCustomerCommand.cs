using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Customers;

public class DeleteCustomerCommand : IRequest<bool>
{
    public string Email { get; set; }
}