using Grand.Infrastructure.Caching;
using Grand.Domain.Blogs;
using Grand.Infrastructure.Events;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Events.Cache
{
    public class BlogPostNotificatioHandler :
        INotificationHandler<EntityInserted<BlogPost>>,
        INotificationHandler<EntityUpdated<BlogPost>>,
        INotificationHandler<EntityDeleted<BlogPost>>
    {

        private readonly ICacheBase _cacheBase;

        public BlogPostNotificatioHandler(ICacheBase cacheBase)
        {
            _cacheBase = cacheBase;
        }

        public async Task Handle(EntityInserted<BlogPost> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
        }
        public async Task Handle(EntityUpdated<BlogPost> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
        }
        public async Task Handle(EntityDeleted<BlogPost> eventMessage, CancellationToken cancellationToken)
        {
            await _cacheBase.RemoveByPrefix(CacheKeyConst.BLOG_PATTERN_KEY);
        }
    }
}