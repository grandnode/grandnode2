using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Domain.Blogs;
using Grand.Business.Cms.Interfaces;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Blogs;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Blogs
{
    public class GetBlogPostTagListHandler : IRequestHandler<GetBlogPostTagList, BlogPostTagListModel>
    {
        private readonly IBlogService _blogService;
        private readonly ICacheBase _cacheBase;
        private readonly IWorkContext _workContext;

        private readonly BlogSettings _blogSettings;

        public GetBlogPostTagListHandler(IBlogService blogService, ICacheBase cacheBase, IWorkContext workContext, BlogSettings blogSettings)
        {
            _blogService = blogService;
            _cacheBase = cacheBase;
            _workContext = workContext;
            _blogSettings = blogSettings;
        }

        public async Task<BlogPostTagListModel> Handle(GetBlogPostTagList request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKeyConst.BLOG_TAGS_MODEL_KEY, _workContext.WorkingLanguage.Id, _workContext.CurrentStore.Id);
            var cachedModel = await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var model = new BlogPostTagListModel();

                //get tags
                var tags = await _blogService.GetAllBlogPostTags(_workContext.CurrentStore.Id);
                tags = tags.OrderByDescending(x => x.BlogPostCount)
                    .Take(_blogSettings.NumberOfTags)
                    .ToList();
                //sorting
                tags = tags.OrderBy(x => x.Name).ToList();

                foreach (var tag in tags)
                    model.Tags.Add(new BlogPostTagModel {
                        Name = tag.Name,
                        BlogPostCount = tag.BlogPostCount
                    });
                return model;
            });
            return cachedModel;
        }
    }
}
