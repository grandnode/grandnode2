using Grand.Business.Core.Interfaces.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Module.Api.Commands.Models.Customers;
using Grand.SharedKernel;
using Grand.Mediator;

namespace Grand.Module.Api.Commands.Handlers.Customers;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, bool>
{
    private readonly ICustomerService _customerService;
    private readonly CustomerConfig _customerConfig;

    public DeleteCustomerCommandHandler(ICustomerService customerService, CustomerConfig customerConfig)
    {
        _customerService = customerService;
        _customerConfig = customerConfig;
    }

    public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetCustomerByEmail(request.Email);
        if (customer == null) return true;

        //Under per-store customer identity the e-mail is not a unique key, and the global lookup prefers
        //the store-independent (system/back-office/admin) account. Refuse to delete such an account through
        //this by-email API so an attempt to remove a store customer can never destroy the administrator.
        if (_customerConfig.RegisterCustomersPerStore && string.IsNullOrEmpty(customer.StoreId))
            throw new GrandException(
                "Refusing to delete a store-independent (system/back-office) account by e-mail while per-store customer identity is enabled");

        await _customerService.DeleteCustomer(customer);

        return true;
    }
}