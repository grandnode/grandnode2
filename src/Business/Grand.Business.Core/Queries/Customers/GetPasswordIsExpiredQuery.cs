using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Customers;

public class GetPasswordIsExpiredQuery : IRequest<bool>
{
    public Customer Customer { get; set; }
}