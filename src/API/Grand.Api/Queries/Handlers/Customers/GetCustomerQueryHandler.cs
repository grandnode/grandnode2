﻿using Grand.Api.DTOs.Customers;
using Grand.Api.Extensions;
using Grand.Api.Queries.Models.Customers;
using Grand.Business.Core.Interfaces.Customers;
using MediatR;

namespace Grand.Api.Queries.Handlers.Customers
{
    public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, CustomerDto>
    {
        private readonly ICustomerService _customerService;

        public GetCustomerQueryHandler(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<CustomerDto> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
        {
            return (await _customerService.GetCustomerByEmail(request.Email)).ToModel();
        }
    }
}
