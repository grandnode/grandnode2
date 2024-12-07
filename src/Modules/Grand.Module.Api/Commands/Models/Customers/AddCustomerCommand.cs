using Grand.Module.Api.DTOs.Customers;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Customers;

public class AddCustomerCommand : IRequest<CustomerDto>
{
    public CustomerDto Model { get; set; }
}