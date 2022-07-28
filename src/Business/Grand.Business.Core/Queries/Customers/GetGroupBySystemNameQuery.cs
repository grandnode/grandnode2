using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Core.Queries.Customers
{
    public class GetGroupBySystemNameQuery : IRequest<CustomerGroup>
    {
        public string SystemName { get; set; } = "";
    }
}
