using Grand.Infrastructure.Events;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Authentication.Events
{
    public class CustomerDeletedEventHandler : INotificationHandler<EntityDeleted<Customer>>
    {
        private readonly IRepository<ExternalAuthentication> _externalAuthenticationRepository;
        public CustomerDeletedEventHandler(IRepository<ExternalAuthentication> externalAuthenticationRepository)
        {
            _externalAuthenticationRepository = externalAuthenticationRepository;
        }
        public async Task Handle(EntityDeleted<Customer> notification, CancellationToken cancellationToken)
        {
            var externalrecords = _externalAuthenticationRepository.Table.Where(x => x.CustomerId == notification.Entity.Id).ToList();
            foreach (var item in externalrecords)
            {
                await _externalAuthenticationRepository.DeleteAsync(item);
            }
        }
    }
}
