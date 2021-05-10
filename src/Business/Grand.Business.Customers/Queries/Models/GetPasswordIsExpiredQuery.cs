using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Customers.Queries.Models
{
    public class GetPasswordIsExpiredQuery : IRequest<bool>
    {
        public Customer Customer { get; set; }
    }
}
