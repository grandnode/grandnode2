using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Core.Queries.Messages
{
    public class GetCustomerByIdQuery : IRequest<Customer>
    {
        public string Id { get; set; }
    }
}
