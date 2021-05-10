using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Events;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
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
            var builder = Builders<Customer>.Update;
            var update = builder.Pull(p => p.Groups, notification.Entity.Id);
            await _customerRepository.Collection.UpdateManyAsync(new BsonDocument(), update);
        }
    }
}
