﻿using Grand.Api.Commands.Models.Customers;
using Grand.Api.DTOs.Customers;
using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Customers;
using MediatR;

namespace Grand.Api.Commands.Handlers.Customers
{
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
            address.CreatedOnUtc = DateTime.UtcNow;
            address.Id = "";
            await _customerService.InsertAddress(address, request.Customer.Id);
            return address.ToModel();
        }
    }
}
