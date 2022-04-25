using Grand.Business.Core.Queries.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;

namespace Grand.Business.Messages.Queries.Handlers
{
    public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, Customer>
    {
        private readonly IRepository<Customer> _customerRepository;

        public GetCustomerByIdQueryHandler(IRepository<Customer> customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<Customer> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
                return null;

            return await _customerRepository.GetByIdAsync(request.Id);
        }
    }
}
