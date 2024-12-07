using Grand.Module.Api.Commands.Models.Customers;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Customers;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Customers;

public class UpdateCustomerAddressCommandHandler : IRequestHandler<UpdateCustomerAddressCommand, AddressDto>
{
    private readonly ICustomerService _customerService;

    public UpdateCustomerAddressCommandHandler(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task<AddressDto> Handle(UpdateCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetCustomerById(request.Customer.Id);
        if (customer != null)
        {
            var address = customer.Addresses.FirstOrDefault(x => x.Id == request.Address.Id);
            if (address != null)
            {
                address = request.Address.ToEntity(address);
                await _customerService.UpdateAddress(address, request.Customer.Id);
                return address.ToModel();
            }
        }

        return null;
    }
}