using Grand.Business.Common.Interfaces.Seo;
using Grand.Infrastructure.Events;
using Grand.Domain.Blogs;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Cms.Interfaces.Events
{
    public class BlogPostDeletedEventHandler : INotificationHandler<EntityDeleted<BlogPost>>
    {
        private readonly ISlugService _slugService;

        public BlogPostDeletedEventHandler(ISlugService slugService)
        {
            _slugService = slugService;
        }
        public async Task Handle(EntityDeleted<BlogPost> notification, CancellationToken cancellationToken)
        {
            var urlToDelete = await _slugService.GetBySlug(notification.Entity.SeName);
            await _slugService.DeleteEntityUrl(urlToDelete);
        }
    }
}
