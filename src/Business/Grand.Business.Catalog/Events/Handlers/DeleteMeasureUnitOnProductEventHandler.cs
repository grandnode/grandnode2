using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Directory;
using Grand.Infrastructure.Events;
using MediatR;
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
            await _repositoryProduct.UpdateManyAsync(x => x.UnitId == notification.Entity.Id,
                UpdateBuilder<Product>.Create().Set(x => x.UnitId, ""));
        }
    }
}
