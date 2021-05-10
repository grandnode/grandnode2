using Grand.Infrastructure.Events;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Directory;
using MediatR;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Events.Handlers
{
    public class DeleteMeasureUnitOnProductEventHandler : INotificationHandler<EntityDeleted<MeasureUnit>>
    {
        private readonly IRepository<Product> _repositoryProduct;

        public DeleteMeasureUnitOnProductEventHandler(IRepository<Product> repositoryProduct)
        {
            _repositoryProduct = repositoryProduct;
        }

        public async Task Handle(EntityDeleted<MeasureUnit> notification, CancellationToken cancellationToken)
        {
            var builder = Builders<Product>.Filter;
            var filter = builder.Eq(x => x.UnitId, notification.Entity.Id);
            var update = Builders<Product>.Update
                .Set(x => x.UnitId, "");

            await _repositoryProduct.Collection.UpdateManyAsync(filter, update);
        }
    }
}
