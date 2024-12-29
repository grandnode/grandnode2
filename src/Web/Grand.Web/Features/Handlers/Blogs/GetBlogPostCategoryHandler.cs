using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Models.Blogs;
using MediatR;

namespace Grand.Web.Features.Handlers.Blogs;

public class GetBlogPostCategoryHandler : IRequestHandler<GetBlogPostCategory, IList<BlogPostCategoryModel>>
{
    private readonly IBlogService _blogService;
    private readonly ICacheBase _cacheBase;
    private readonly IWorkContextAccessor _workContextAccessor;

    public GetBlogPostCategoryHandler(IBlogService blogService, ICacheBase cacheBase,
        IWorkContextAccessor workContextAccessor)
    {
        _blogService = blogService;
        _cacheBase = cacheBase;
        _workContextAccessor = workContextAccessor;
    }

    public async Task<IList<BlogPostCategoryModel>> Handle(GetBlogPostCategory request,
        CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(CacheKeyConst.BLOG_CATEGORY_MODEL_KEY, _workContextAccessor.WorkContext.WorkingLanguage.Id,
            _workContextAccessor.WorkContext.CurrentStore.Id);
        var cachedModel = await _cacheBase.GetAsync(cacheKey, async () =>
        {
            var model = new List<BlogPostCategoryModel>();
            var categories = await _blogService.GetAllBlogCategories(_workContextAccessor.WorkContext.CurrentStore.Id);
            foreach (var item in categories)
                model.Add(new BlogPostCategoryModel {
                    Id = item.Id,
                    Name = item.GetTranslation(x => x.Name, _workContextAccessor.WorkContext.WorkingLanguage.Id),
                    SeName = item.SeName,
                    BlogPostCount = item.BlogPosts.Count
                });
            return model;
        });
        return cachedModel;
    }
}