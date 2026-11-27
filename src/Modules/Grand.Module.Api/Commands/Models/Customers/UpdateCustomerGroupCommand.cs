using Grand.Module.Api.DTOs.Customers;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Customers;

public class UpdateCustomerGroupCommand : IRequest<CustomerGroupDto>
{
    public required CustomerGroupDto Model { get; set; }
}