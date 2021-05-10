using Grand.Infrastructure.Caching;
using Grand.Domain.Blogs;
using Grand.Infrastructure.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Events.Cache
{
    public class BlogCategoryNotificatioHandler :
        INotificationHandler<EntityInserted<BlogCategory>>,
        INotificationHandler<EntityUpdated<BlogCategory>>,
        INotificationHandler<EntityDeleted<BlogCategory>>
    {

        private readonly ICacheBase _cacheBase;

        public BlogCategoryNotificatioHandler(ICacheBase cacheBase)
        {
            _cacheBase = cacheBase;
        }

        public async Task Handle(EntityInserted<BlogCategory> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
        }
        public async Task Handle(EntityUpdated<BlogCategory> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
        }
        public async Task Handle(EntityDeleted<BlogCategory> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
        }
    }
}