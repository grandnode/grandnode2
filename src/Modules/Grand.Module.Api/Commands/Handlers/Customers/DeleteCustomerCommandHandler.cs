using Grand.Module.Api.Commands.Models.Customers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Customers;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, bool>
{
    private readonly ICustomerService _customerService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public DeleteCustomerCommandHandler(
        ICustomerService customerService,
        ITranslationService translationService,
        IWorkContextAccessor workContextAccessor)
    {
        _customerService = customerService;
        _translationService = translationService;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetCustomerByEmail(request.Email);
        if (customer != null) await _customerService.DeleteCustomer(customer);

        return true;
    }
}