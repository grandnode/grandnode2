using Grand.Module.Api.DTOs.Customers;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Customers;

public class AddCustomerCommand : IRequest<CustomerDto>
{
    public CustomerDto Model { get; set; }
}