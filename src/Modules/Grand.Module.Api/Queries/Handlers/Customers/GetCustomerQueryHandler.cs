using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Extensions;
using Grand.Module.Api.Queries.Models.Customers;
using Grand.Business.Core.Interfaces.Customers;
using MediatR;

namespace Grand.Module.Api.Queries.Handlers.Customers;

public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, CustomerDto>
{
    private readonly ICustomerService _customerService;

    public GetCustomerQueryHandler(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task<CustomerDto> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        return (await _customerService.GetCustomerByEmail(request.Email)).ToModel();
    }
}