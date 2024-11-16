using Grand.Module.Api.Commands.Models.Customers;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Customers;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Customers;

public class AddCustomerAddressCommandHandler : IRequestHandler<AddCustomerAddressCommand, AddressDto>
{
    private readonly ICustomerService _customerService;

    public AddCustomerAddressCommandHandler(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task<AddressDto> Handle(AddCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        var address = request.Address.ToEntity();
        address.Id = "";
        await _customerService.InsertAddress(address, request.Customer.Id);
        return address.ToModel();
    }
}