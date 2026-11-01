using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Web.Commands.Models.Customers;
using Grand.Mediator;

namespace Grand.Web.Commands.Handler.Customers;

public class SubAccountEditCommandHandler : IRequestHandler<SubAccountEditCommand, bool>
{
    private readonly ICustomerManagerService _customerManagerService;
    private readonly ICustomerService _customerService;
    private readonly CustomerSettings _customerSettings;

    public SubAccountEditCommandHandler(
        ICustomerService customerService,
        ICustomerManagerService customerManagerService,
        CustomerSettings customerSettings)
    {
        _customerService = customerService;
        _customerManagerService = customerManagerService;
        _customerSettings = customerSettings;
    }

    public async Task<bool> Handle(SubAccountEditCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.CurrentCustomer);
        var customer = await _customerService.GetCustomerById(request.EditModel.Id);
        if (customer == null || customer.Deleted || customer.OwnerId != request.CurrentCustomer.Id)
            return false;

        //update email - stored lowercased like every other write, or the lowercased lookups miss it. An address
        //differing only in case is the same address, so it is repaired even when changing it is not allowed
        var email = request.EditModel.Email.Trim().ToLowerInvariant();
        var sameAddress = string.Equals(customer.Email, email, StringComparison.OrdinalIgnoreCase);
        if (customer.Email != email && (sameAddress || _customerSettings.AllowUsersToChangeEmail))
        {
            customer.Email = email;
            await _customerService.UpdateCustomerField(customer, x => x.Email, email);
            //without usernames the username is the email (see SubAccountAddCommandHandler)
            if (!_customerSettings.UsernamesEnabled)
            {
                customer.Username = email;
                await _customerService.UpdateCustomerField(customer, x => x.Username, email);
            }
        }

        //update password (scope the lookup to the sub-account's store - safe with or without per-store identity)
        if (!string.IsNullOrEmpty(request.EditModel.Password))
        {
            //the password change finds its account by email, which has to lead back to this account
            var emailHolder = await _customerService.GetCustomerByEmail(customer.Email, customer.StoreId);
            if (emailHolder?.Id != customer.Id)
                return false;

            await _customerManagerService.ChangePassword(
                new ChangePasswordRequest(customer.Email, _customerSettings.DefaultPasswordFormat,
                    request.EditModel.Password), customer.StoreId);
        }

        //update active
        customer.Active = request.EditModel.Active;
        await _customerService.UpdateActive(customer);

        //update attributes
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.FirstName, request.EditModel.FirstName);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.LastName, request.EditModel.LastName);

        return true;
    }
}