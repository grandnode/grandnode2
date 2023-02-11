﻿using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Web.Commands.Models.Customers;
using MediatR;

namespace Grand.Web.Commands.Handler.Customers
{
    public class SubAccountEditCommandHandler : IRequestHandler<SubAccountEditCommand, bool>
    {
        private readonly ICustomerService _customerService;
        private readonly ICustomerManagerService _customerManagerService;
        private readonly IUserFieldService _userFieldService;
        private readonly CustomerSettings _customerSettings;

        public SubAccountEditCommandHandler(
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

        public async Task<bool> Handle(SubAccountEditCommand request, CancellationToken cancellationToken)
        {
            if (request.CurrentCustomer == null)
            {
                throw new ArgumentNullException(nameof(request.CurrentCustomer));
            }

            var customer = await _customerService.GetCustomerById(request.EditModel.Id);

            //update email
            if (customer.Email != request.EditModel.Email.ToLower() && _customerSettings.AllowUsersToChangeEmail)
            {
                await _customerManagerService.SetEmail(customer, request.EditModel.Email);
            }

            //update password
            await _customerManagerService.ChangePassword(
                new ChangePasswordRequest(customer.Email, _customerSettings.DefaultPasswordFormat,
                    request.EditModel.Password));

            //update active
            customer.Active = request.EditModel.Active;
            await _customerService.UpdateActive(customer);

            //update attributes
            await _userFieldService.SaveField(customer, SystemCustomerFieldNames.FirstName, request.EditModel.FirstName);
            await _userFieldService.SaveField(customer, SystemCustomerFieldNames.LastName, request.EditModel.LastName);

            return true;
        }
    }
}