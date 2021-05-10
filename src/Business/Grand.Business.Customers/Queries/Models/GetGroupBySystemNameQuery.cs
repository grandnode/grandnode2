using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Customers.Queries.Models
{
    public class GetGroupBySystemNameQuery : IRequest<CustomerGroup>
    {
        public string SystemName { get; set; } = "";
    }
}
