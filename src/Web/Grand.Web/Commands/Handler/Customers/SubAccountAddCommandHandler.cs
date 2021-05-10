using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Customers.Utilities;
using Grand.Domain.Customers;
using Grand.Web.Commands.Models.Customers;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.Customers
{
    public class SubAccountAddCommandHandler : IRequestHandler<SubAccountAddCommand, RegistrationResult>
    {
        private readonly ICustomerService _customerService;
        private readonly ICustomerManagerService _customerManagerService;
        private readonly IUserFieldService _userFieldService;
        private readonly CustomerSettings _customerSettings;

        public SubAccountAddCommandHandler(
            ICustomerService customerService,
            ICustomerManagerService customerManagerService,
            IUserFieldService userFieldService,
            CustomerSettings customerSettings)
        {
            _customerService = customerService;
            _customerManagerService = customerManagerService;
            _userFieldService = userFieldService;
            _customerSettings = customerSettings;
        }
        public async Task<RegistrationResult> Handle(SubAccountAddCommand request, CancellationToken cancellationToken)
        {
            var customer = await PrepareCustomer(request);

            var registrationRequest = new RegistrationRequest(customer, request.Model.Email,
                    request.Model.Email, request.Model.Password,
                    _customerSettings.DefaultPasswordFormat, request.Store.Id, request.Model.Active);

            var customerRegistrationResult = await _customerManagerService.RegisterCustomer(registrationRequest);

            if (!customerRegistrationResult.Success)
            {
                await _customerService.DeleteCustomer(customer, true);
            }
            else
            {
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.FirstName, request.Model.FirstName);
                await _userFieldService.SaveField(customer, SystemCustomerFieldNames.LastName, request.Model.LastName);
            }
            return customerRegistrationResult;
        }

        protected async Task<Customer> PrepareCustomer(SubAccountAddCommand request)
        {
            var customer = new Customer();
            customer.OwnerId = request.Customer.Id;
            customer.CustomerGuid = Guid.NewGuid();
            customer.StoreId = request.Store.Id;
            customer.CreatedOnUtc = DateTime.UtcNow;
            customer.LastActivityDateUtc = DateTime.UtcNow;

            await _customerService.InsertCustomer(customer);
            return customer;
        }

    }
}
