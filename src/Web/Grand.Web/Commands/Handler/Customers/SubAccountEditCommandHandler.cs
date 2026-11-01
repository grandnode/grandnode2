using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Models.Customer;
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

        //update email - stored lowercased like every other write, or the lowercased login lookup misses it. An
        //address differing only in case is the same address, so it is repaired even when changing it is not
        //allowed: older edits stored it as typed, which locked the account out and broke its password change
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

        //update password - of this account, the one the ownership check above is about
        if (!string.IsNullOrEmpty(request.EditModel.Password))
            await _customerManagerService.ChangePassword(customer, _customerSettings.DefaultPasswordFormat,
                request.EditModel.Password);

        //update active
        customer.Active = request.EditModel.Active;
        await _customerService.UpdateActive(customer);

        //update attributes
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.FirstName, request.EditModel.FirstName);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.LastName, request.EditModel.LastName);

        //update role and order limit - only after the ownership check above. An approver orders without a limit,
        //so whatever limit came with the request is dropped
        var role = request.EditModel.Role;
        //the text was checked by the validator; unparseable text from a caller that skipped it means no limit
        OrderApprovalLimitText.TryParse(request.EditModel.OrderApprovalLimit, out var typedLimit);
        var limit = role == SubAccountRole.Approver ? null : typedLimit;
        customer.SubAccountRole = role;
        customer.OrderApprovalLimit = limit;
        await _customerService.UpdateCustomerField(customer, x => x.SubAccountRole, role);
        await _customerService.UpdateCustomerField(customer, x => x.OrderApprovalLimit, limit);

        return true;
    }
}