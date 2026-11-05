using Grand.Domain.Knowledgebase;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Cms.Events.Cache;

public class KnowledgebaseCacheEventHandler :
    INotificationHandler<EntityInserted<KnowledgebaseCategory>>,
    INotificationHandler<EntityUpdated<KnowledgebaseCategory>>,
    INotificationHandler<EntityDeleted<KnowledgebaseCategory>>,
    INotificationHandler<EntityInserted<KnowledgebaseArticle>>,
    INotificationHandler<EntityUpdated<KnowledgebaseArticle>>,
    INotificationHandler<EntityDeleted<KnowledgebaseArticle>>
{
    private readonly ICacheBase _cacheBase;

    public KnowledgebaseCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<KnowledgebaseCategory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<KnowledgebaseCategory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<KnowledgebaseCategory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
    }

    public async Task Handle(EntityInserted<KnowledgebaseArticle> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<KnowledgebaseArticle> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<KnowledgebaseArticle> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.ARTICLES_PATTERN_KEY);
        await _cacheBase.RemoveByPrefix(CacheKey.KNOWLEDGEBASE_CATEGORIES_PATTERN_KEY);
    }
}
