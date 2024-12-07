using Grand.Module.Api.DTOs.Customers;
using MediatR;

namespace Grand.Module.Api.Commands.Models.Customers;

public class UpdateCustomerCommand : IRequest<CustomerDto>
{
    public CustomerDto Model { get; set; }
}