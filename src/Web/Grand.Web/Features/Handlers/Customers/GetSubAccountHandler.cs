using Grand.Business.Customers.Interfaces;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Customers
{
    public class GetSubAccountHandler : IRequestHandler<GetSubAccount, SubAccountModel>
    {
        private readonly ICustomerService _customerService;
        public GetSubAccountHandler(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<SubAccountModel> Handle(GetSubAccount request, CancellationToken cancellationToken)
        {
            if (request.CurrentCustomer == null)
                throw new ArgumentNullException(nameof(request.CurrentCustomer));

            var model = new SubAccountModel();

            var subaccount = await _customerService.GetCustomerById(customerId: request.CustomerId);
            if (subaccount != null && subaccount.OwnerId == request.CurrentCustomer.Id)
            {
                model = new SubAccountModel()
                {
                    Id = subaccount.Id,
                    Email = subaccount.Email,
                    Active = subaccount.Active,
                    FirstName = subaccount.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName),
                    LastName = subaccount.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName)
                };
            }

            return model;
        }
    }
}
