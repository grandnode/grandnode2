using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Events;
using Grand.Mediator;

namespace Grand.Business.Messages.Events.Cache;

public class MessageTemplateCacheEventHandler :
    INotificationHandler<EntityInserted<MessageTemplate>>,
    INotificationHandler<EntityUpdated<MessageTemplate>>,
    INotificationHandler<EntityDeleted<MessageTemplate>>
{
    private readonly ICacheBase _cacheBase;

    public MessageTemplateCacheEventHandler(ICacheBase cacheBase)
    {
        _cacheBase = cacheBase;
    }

    public async Task Handle(EntityInserted<MessageTemplate> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);
    }

    public async Task Handle(EntityUpdated<MessageTemplate> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);
    }

    public async Task Handle(EntityDeleted<MessageTemplate> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.MESSAGETEMPLATES_PATTERN_KEY);
    }
}
