﻿using Grand.Api.Commands.Models.Customers;
using Grand.Business.Core.Interfaces.Customers;
using MediatR;

namespace Grand.Api.Commands.Handlers.Customers
{
    public class DeleteCustomerAddressCommandHandler : IRequestHandler<DeleteCustomerAddressCommand, bool>
    {
        private readonly ICustomerService _customerService;

        public DeleteCustomerAddressCommandHandler(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<bool> Handle(DeleteCustomerAddressCommand request, CancellationToken cancellationToken)
        {
            var customer = await _customerService.GetCustomerById(request.Customer.Id);
            if (customer != null)
            {
                var address = customer.Addresses.FirstOrDefault(x => x.Id == request.Address.Id);
                if (address != null)
                {
                    await _customerService.DeleteAddress(address, request.Customer.Id);
                }
            }
            return true;
        }
    }
}
