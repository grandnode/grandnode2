using Grand.Infrastructure.Caching;
using Grand.Domain.News;
using Grand.Infrastructure.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Events.Cache
{
    public class NewsItemNotificatioHandler :
        INotificationHandler<EntityInserted<NewsItem>>,
        INotificationHandler<EntityUpdated<NewsItem>>,
        INotificationHandler<EntityDeleted<NewsItem>>
    {

        private readonly ICacheBase _cacheBase;

        public NewsItemNotificatioHandler(ICacheBase cacheBase)
        {
            _cacheBase = cacheBase;
        }

        public async Task Handle(EntityInserted<NewsItem> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.NEWS_PATTERN_KEY);
        }
        public async Task Handle(EntityUpdated<NewsItem> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.NEWS_PATTERN_KEY);
        }
        public async Task Handle(EntityDeleted<NewsItem> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.NEWS_PATTERN_KEY);
        }
    }
}