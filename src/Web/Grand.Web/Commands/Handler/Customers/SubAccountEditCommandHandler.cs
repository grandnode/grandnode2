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
    public class SubAccountEditCommandHandler : IRequestHandler<SubAccountEditCommand, (bool success, string error)>
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
        public async Task<(bool success, string error)> Handle(SubAccountEditCommand request, CancellationToken cancellationToken)
        {
            if (request.CurrentCustomer == null)
            {
                throw new ArgumentNullException(nameof(request.CurrentCustomer));
            }

            var customer = await _customerService.GetCustomerById(request.Model.Id);
            if (customer == null || customer.OwnerId != request.CurrentCustomer.Id)
            {
                return (false, "You are not owner of this account");
            }

            //update email
            if (customer.Email != request.Model.Email.ToLower() && _customerSettings.AllowUsersToChangeEmail)
            {
                try
                {
                    await _customerManagerService.SetEmail(customer, request.Model.Email);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            }

            //update password
            if (!string.IsNullOrEmpty(request.Model.Password))
            {
                var result = await _customerManagerService.ChangePassword(
                    new ChangePasswordRequest(request.Model.Email, false, _customerSettings.DefaultPasswordFormat, request.Model.Password));
                if (!result.Success)
                    return (false, string.Join(", ", result.Errors));
            }

            //update active
            customer.Active = request.Model.Active;
            await _customerService.UpdateActive(customer);

            //update attributes
            await _userFieldService.SaveField(customer, SystemCustomerFieldNames.FirstName, request.Model.FirstName);
            await _userFieldService.SaveField(customer, SystemCustomerFieldNames.LastName, request.Model.LastName);

            return (true, string.Empty);
        }



    }
}
