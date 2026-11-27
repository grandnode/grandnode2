using Grand.Module.Api.DTOs.Customers;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Models.Customers;

public class DeleteCustomerAddressCommand : IRequest<bool>
{
    public required CustomerDto Customer { get; set; }
    public required AddressDto Address { get; set; }
}