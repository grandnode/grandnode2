using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Business.Common.Events
{
    public class GroupDeletedEventHandler : INotificationHandler<EntityDeleted<CustomerGroup>>
    {
        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly ICacheBase _cacheBase;
        
        public GroupDeletedEventHandler(
            IRepository<Customer> customerRepository,
            IRepository<Product> productRepository,
            ICacheBase cacheBase)
        {
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _cacheBase = cacheBase;
        }

        public async Task Handle(EntityDeleted<CustomerGroup> notification, CancellationToken cancellationToken)
        {
            //delete from customers
            await _customerRepository.Pull(string.Empty, x => x.Groups, notification.Entity.Id);
            
            //delete tier prices on the product
            await _productRepository.PullFilter(string.Empty, x => x.TierPrices, z => z.CustomerGroupId, notification.Entity.Id);

            //clear cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
        }
    }
}
