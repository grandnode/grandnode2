using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Business.Catalog.Events.Handlers;

public class CategoryDeletedEventHandler : INotificationHandler<EntityDeleted<Category>>
{
    private readonly ICacheBase _cacheBase;
    private readonly IRepository<EntityUrl> _entityUrlRepository;
    private readonly IRepository<Product> _productRepository;

    public CategoryDeletedEventHandler(
        IRepository<EntityUrl> entityUrlRepository,
        IRepository<Product> productRepository,
        ICacheBase cacheBase)
    {
        _entityUrlRepository = entityUrlRepository;
        _productRepository = productRepository;

        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityDeleted<Category> notification, CancellationToken cancellationToken)
    {
        //delete url
        await _entityUrlRepository.DeleteManyAsync(x =>
            x.EntityId == notification.Entity.Id && x.EntityName == EntityTypes.Category);

        //delete on the product
        await _productRepository.PullFilter(string.Empty, x => x.ProductCategories, z => z.CategoryId,
            notification.Entity.Id);

        //clear cache
        await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
    }
}