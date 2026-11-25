using Grand.Module.Api.DTOs.Customers;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Customers;

public class UpdateCustomerCommand : IRequest<CustomerDto>
{
    public required CustomerDto Model { get; set; }
}