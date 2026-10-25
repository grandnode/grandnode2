using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Features.Handlers.Customers;

public class GetSubAccountHandler : IRequestHandler<GetSubAccount, SubAccountEditModel>
{
    private readonly ICustomerService _customerService;

    public GetSubAccountHandler(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task<SubAccountEditModel> Handle(GetSubAccount request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.CurrentCustomer);

        //null for a missing, deleted or someone else's account, so the caller can tell it from an empty form
        var subaccount = await _customerService.GetCustomerById(request.CustomerId);
        if (subaccount == null || subaccount.Deleted || subaccount.OwnerId != request.CurrentCustomer.Id)
            return null;

        return new SubAccountEditModel {
            Id = subaccount.Id,
            Email = subaccount.Email,
            Active = subaccount.Active,
            Role = subaccount.SubAccountRole,
            OrderApprovalLimit = subaccount.OrderApprovalLimit,
            FirstName = subaccount.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName),
            LastName = subaccount.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName)
        };
    }
}