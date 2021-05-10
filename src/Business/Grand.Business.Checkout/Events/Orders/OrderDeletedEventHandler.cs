using Grand.Domain.Catalog;
using Grand.Domain.Data;
using MediatR;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Events.Orders
{
    public class OrderDeletedEventHandler : INotificationHandler<OrderDeletedEvent>
    {
        private readonly IRepository<ProductAlsoPurchased> _productAlsoPurchasedRepository;

        public OrderDeletedEventHandler(IRepository<ProductAlsoPurchased> productAlsoPurchasedRepository)
        {
            _productAlsoPurchasedRepository = productAlsoPurchasedRepository;
        }

        public Task Handle(OrderDeletedEvent notification, CancellationToken cancellationToken)
        {
            //delete product also purchased
            var filters = Builders<ProductAlsoPurchased>.Filter;
            var filter = filters.Where(x => x.OrderId == notification.Order.Id);
            return _productAlsoPurchasedRepository.Collection.DeleteManyAsync(filter);
        }
    }
}
