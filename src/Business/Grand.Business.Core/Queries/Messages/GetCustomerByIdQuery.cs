using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Messages;

public class GetCustomerByIdQuery : IRequest<Customer>
{
    public string Id { get; set; }
}