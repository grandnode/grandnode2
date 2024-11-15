using Grand.Module.Api.DTOs.Customers;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Customers;

public class DeleteCustomerGroupCommand : IRequest<bool>
{
    public CustomerGroupDto Model { get; set; }
}