using Grand.Business.Core.Interfaces.Customers;
using Grand.Module.Api.Commands.Models.Customers;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Customers;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, bool>
{
    private readonly ICustomerService _customerService;

    public DeleteCustomerCommandHandler(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task<bool> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetCustomerByEmail(request.Email);
        if (customer != null) await _customerService.DeleteCustomer(customer);

        return true;
    }
}