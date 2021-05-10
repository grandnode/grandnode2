using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Infrastructure.Events;
using MediatR;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Events.Handlers
{
    public class BrandDeletedEventHandler : INotificationHandler<EntityDeleted<Brand>>
    {
        private readonly IRepository<Product> _productRepository;

        public BrandDeletedEventHandler(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task Handle(EntityDeleted<Brand> notification, CancellationToken cancellationToken)
        {
            var builder = Builders<Product>.Filter;
            var filter = builder.Eq(x => x.BrandId, notification.Entity.Id);
            var update = Builders<Product>.Update
                .Set(x => x.BrandId, "");

            await _productRepository.Collection.UpdateManyAsync(filter, update);
        }
    }
}
