using Grand.Data;
using Grand.Domain.Knowledgebase;
using Grand.Domain.News;
using Grand.Domain.Pages;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Cms.Events;

/// <summary>
///     Removes the slugs of a deleted page, news item or knowledgebase article/category - every language's,
///     so the old link stops resolving and the name can be reused - as the catalog handlers do for theirs.
/// </summary>
public class ContentDeletedSlugEventHandler :
    INotificationHandler<EntityDeleted<Page>>,
    INotificationHandler<EntityDeleted<NewsItem>>,
    INotificationHandler<EntityDeleted<KnowledgebaseArticle>>,
    INotificationHandler<EntityDeleted<KnowledgebaseCategory>>
{
    private readonly ICacheBase _cacheBase;
    private readonly IRepository<EntityUrl> _entityUrlRepository;

    public ContentDeletedSlugEventHandler(IRepository<EntityUrl> entityUrlRepository, ICacheBase cacheBase)
    {
        _entityUrlRepository = entityUrlRepository;
        _cacheBase = cacheBase;
    }

    public Task Handle(EntityDeleted<Page> notification, CancellationToken cancellationToken)
        => DeleteUrls(notification.Entity.Id, EntityTypes.Page);

    public Task Handle(EntityDeleted<NewsItem> notification, CancellationToken cancellationToken)
        => DeleteUrls(notification.Entity.Id, EntityTypes.NewsItem);

    public Task Handle(EntityDeleted<KnowledgebaseArticle> notification, CancellationToken cancellationToken)
        => DeleteUrls(notification.Entity.Id, EntityTypes.KnowledgeBaseArticle);

    public Task Handle(EntityDeleted<KnowledgebaseCategory> notification, CancellationToken cancellationToken)
        => DeleteUrls(notification.Entity.Id, EntityTypes.KnowledgeBaseCategory);

    private async Task DeleteUrls(string entityId, string entityName)
    {
        await _entityUrlRepository.DeleteManyAsync(x => x.EntityId == entityId && x.EntityName == entityName);
        await _cacheBase.RemoveByPrefix(CacheKey.URLEntity_PATTERN_KEY);
    }
}
