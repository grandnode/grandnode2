using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Catalog.Events.Handlers;

public class BrandDeletedEventHandler : INotificationHandler<EntityDeleted<Brand>>
{
    private readonly ICacheBase _cacheBase;
    private readonly IRepository<EntityUrl> _entityUrlRepository;
    private readonly IRepository<Product> _productRepository;

    public BrandDeletedEventHandler(
        IRepository<EntityUrl> entityUrlRepository,
        IRepository<Product> productRepository,
        ICacheBase cacheBase)
    {
        _entityUrlRepository = entityUrlRepository;
        _productRepository = productRepository;
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityDeleted<Brand> notification, CancellationToken cancellationToken)
    {
        //delete url - every language's slug, so the name is free again and the old link stops resolving
        await _entityUrlRepository.DeleteManyAsync(x =>
            x.EntityId == notification.Entity.Id && x.EntityName == EntityTypes.Brand);
        await _cacheBase.RemoveByPrefix(CacheKey.URLEntity_PATTERN_KEY);

        await _productRepository.UpdateManyAsync(x => x.BrandId == notification.Entity.Id,
            UpdateBuilder<Product>.Create().Set(x => x.BrandId, ""));
    }
}
