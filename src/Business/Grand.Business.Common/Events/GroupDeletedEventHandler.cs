using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Common.Events
{
    public class GroupDeletedEventHandler : INotificationHandler<EntityDeleted<CustomerGroup>>
    {
        private readonly IRepository<Customer> _customerRepository;

        public GroupDeletedEventHandler(IRepository<Customer> customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task Handle(EntityDeleted<CustomerGroup> notification, CancellationToken cancellationToken)
        {
            //delete from customers
            await _customerRepository.Pull(string.Empty, x => x.Groups, notification.Entity.Id, true);
        }
    }
}
