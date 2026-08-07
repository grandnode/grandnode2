using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Customers;

public class GetGroupBySystemNameQuery : IRequest<CustomerGroup>
{
    public string SystemName { get; set; } = "";
}