﻿using Grand.Business.Core.Interfaces.Customers;
using Grand.Web.Commands.Models.Customers;
using MediatR;

namespace Grand.Web.Commands.Handler.Customers
{
    public class SubAccountDeleteCommandHandler : IRequestHandler<SubAccountDeleteCommand, (bool success, string error)>
    {
        private readonly ICustomerService _customerService;

        public SubAccountDeleteCommandHandler(ICustomerService customerService)
        {
            _customerService = customerService;
        }
        public async Task<(bool success, string error)> Handle(SubAccountDeleteCommand request, CancellationToken cancellationToken)
        {
            if (request.CurrentCustomer == null)
            {
                throw new ArgumentNullException(nameof(request.CurrentCustomer));
            }

            var customer = await _customerService.GetCustomerById(request.CustomerId);
            if (customer == null || customer.OwnerId != request.CurrentCustomer.Id)
            {
                return (false, "You are not owner of this account");
            }

            //delete customer
            await _customerService.DeleteCustomer(customer);

            return (true, string.Empty);
        }



    }
}
