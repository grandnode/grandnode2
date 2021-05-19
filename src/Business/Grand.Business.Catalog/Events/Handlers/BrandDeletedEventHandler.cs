using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Infrastructure.Events;
using MediatR;
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
            await _productRepository.UpdateManyAsync(x => x.BrandId == notification.Entity.Id,
                UpdateBuilder<Product>.Create().Set(x => x.BrandId, ""));
        }
    }
}
