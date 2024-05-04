using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;

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
        if (request.CurrentCustomer == null)
            throw new ArgumentNullException(nameof(request.CurrentCustomer));

        var model = new SubAccountEditModel();

        var subaccount = await _customerService.GetCustomerById(request.CustomerId);
        if (subaccount != null && subaccount.OwnerId == request.CurrentCustomer.Id)
            model = new SubAccountEditModel {
                Id = subaccount.Id,
                Email = subaccount.Email,
                Active = subaccount.Active,
                FirstName = subaccount.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName),
                LastName = subaccount.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName)
            };

        return model;
    }
}