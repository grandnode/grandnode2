using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Blogs;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Blogs
{
    public class GetBlogPostCategoryHandler : IRequestHandler<GetBlogPostCategory, IList<BlogPostCategoryModel>>
    {
        private readonly IBlogService _blogService;
        private readonly ICacheBase _cacheBase;
        private readonly IWorkContext _workContext;

        public GetBlogPostCategoryHandler(IBlogService blogService, ICacheBase cacheBase,
            IWorkContext workContext)
        {
            _blogService = blogService;
            _cacheBase = cacheBase;
            _workContext = workContext;
        }

        public async Task<IList<BlogPostCategoryModel>> Handle(GetBlogPostCategory request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKeyConst.BLOG_CATEGORY_MODEL_KEY, _workContext.WorkingLanguage.Id, _workContext.CurrentStore.Id);
            var cachedModel = await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var model = new List<BlogPostCategoryModel>();
                var categories = await _blogService.GetAllBlogCategories(_workContext.CurrentStore.Id);
                foreach (var item in categories)
                {
                    model.Add(new BlogPostCategoryModel()
                    {
                        Id = item.Id,
                        Name = item.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                        SeName = item.SeName,
                        BlogPostCount = item.BlogPosts.Count
                    });
                }
                return model;
            });
            return cachedModel;
        }
    }
}
