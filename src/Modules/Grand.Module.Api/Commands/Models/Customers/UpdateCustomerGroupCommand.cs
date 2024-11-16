using Grand.Module.Api.DTOs.Customers;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Customers;

public class UpdateCustomerGroupCommand : IRequest<CustomerGroupDto>
{
    public CustomerGroupDto Model { get; set; }
}