using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Messages.Queries.Models
{
    public class GetCustomerByIdQuery : IRequest<Customer>
    {
        public string Id { get; set; }
    }
}
