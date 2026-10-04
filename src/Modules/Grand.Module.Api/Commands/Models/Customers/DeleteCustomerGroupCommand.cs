using Grand.Module.Api.DTOs.Customers;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Customers;

public class DeleteCustomerGroupCommand : IRequest<bool>
{
    public CustomerGroupDto Model { get; set; }
}