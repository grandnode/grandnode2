using Grand.Module.Api.DTOs.Customers;
using MediatR;

namespace Grand.Module.Api.Queries.Models.Customers;

public class GetCustomerQuery : IRequest<CustomerDto>
{
    public string Email { get; set; }
}