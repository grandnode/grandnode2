using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Events.Handlers
{
    public class DeliveryDateDeletedEventHandler : INotificationHandler<EntityDeleted<DeliveryDate>>
    {
        private readonly IRepository<Product> _productRepository;

        public DeliveryDateDeletedEventHandler(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task Handle(EntityDeleted<DeliveryDate> notification, CancellationToken cancellationToken)
        {
            await _productRepository.UpdateManyAsync(x => x.DeliveryDateId == notification.Entity.Id,
                        UpdateBuilder<Product>.Create().Set(x => x.DeliveryDateId, ""));
        }
    }
}
