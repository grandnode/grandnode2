using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Web.Commands.Models.Customers;
using MediatR;

namespace Grand.Web.Commands.Handler.Customers;

public class SubAccountAddCommandHandler : IRequestHandler<SubAccountAddCommand>
{
    private readonly ICustomerManagerService _customerManagerService;
    private readonly ICustomerService _customerService;
    private readonly CustomerSettings _customerSettings;

    public SubAccountAddCommandHandler(
        ICustomerService customerService,
        ICustomerManagerService customerManagerService,
        CustomerSettings customerSettings)
    {
        _customerService = customerService;
        _customerManagerService = customerManagerService;
        _customerSettings = customerSettings;
    }

    public async Task Handle(SubAccountAddCommand request, CancellationToken cancellationToken)
    {
        var customer = await PrepareCustomer(request);

        var registrationRequest = new RegistrationRequest(customer, request.Model.Email,
            request.Model.Email, request.Model.Password,
            _customerSettings.DefaultPasswordFormat, request.Store.Id, request.Model.Active);

        await _customerManagerService.RegisterCustomer(registrationRequest);

        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.FirstName, request.Model.FirstName);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.LastName, request.Model.LastName);
    }

    protected async Task<Customer> PrepareCustomer(SubAccountAddCommand request)
    {
        var customer = new Customer {
            OwnerId = request.Customer.Id,
            CustomerGuid = Guid.NewGuid(),
            StoreId = request.Store.Id,
            LastActivityDateUtc = DateTime.UtcNow
        };

        await _customerService.InsertCustomer(customer);
        return customer;
    }
}