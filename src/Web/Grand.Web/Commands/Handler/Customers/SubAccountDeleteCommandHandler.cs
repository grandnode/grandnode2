using Grand.Business.Core.Interfaces.Customers;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Common.Localization;
using Grand.Mediator;

namespace Grand.Web.Commands.Handler.Customers;

public class SubAccountDeleteCommandHandler : IRequestHandler<SubAccountDeleteCommand, (bool success, string error)>
{
    private readonly ICustomerService _customerService;
    private readonly IRequestTranslationService _translationService;

    public SubAccountDeleteCommandHandler(ICustomerService customerService,
        IRequestTranslationService translationService)
    {
        _customerService = customerService;
        _translationService = translationService;
    }

    public async Task<(bool success, string error)> Handle(SubAccountDeleteCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.CurrentCustomer);

        var customer = await _customerService.GetCustomerById(request.CustomerId);
        if (customer == null || customer.Deleted || customer.OwnerId != request.CurrentCustomer.Id)
            return (false, _translationService.GetResource("Account.SubAccounts.NotOwner"));

        //delete customer
        await _customerService.DeleteCustomer(customer);

        return (true, string.Empty);
    }
}