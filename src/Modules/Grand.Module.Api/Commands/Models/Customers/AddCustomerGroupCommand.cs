using Grand.Module.Api.DTOs.Customers;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Customers;

public class AddCustomerGroupCommand : IRequest<CustomerGroupDto>
{
    public CustomerGroupDto Model { get; set; }
}