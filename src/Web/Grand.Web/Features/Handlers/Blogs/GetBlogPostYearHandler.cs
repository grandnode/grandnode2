﻿using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain.Blogs;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Models.Blogs;
using MediatR;

namespace Grand.Web.Features.Handlers.Blogs
{
    public class GetBlogPostYearHandler : IRequestHandler<GetBlogPostYear, IList<BlogPostYearModel>>
    {
        private readonly IBlogService _blogService;
        private readonly ICacheBase _cacheBase;
        private readonly IWorkContext _workContext;

        public GetBlogPostYearHandler(IBlogService blogService, ICacheBase cacheBase,
            IWorkContext workContext)
        {
            _blogService = blogService;
            _cacheBase = cacheBase;
            _workContext = workContext;
        }

        public async Task<IList<BlogPostYearModel>> Handle(GetBlogPostYear request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKeyConst.BLOG_MONTHS_MODEL_KEY, _workContext.WorkingLanguage.Id, _workContext.CurrentStore.Id);
            var cachedModel = await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var model = new List<BlogPostYearModel>();

                var blogPosts = await _blogService.GetAllBlogPosts(_workContext.CurrentStore.Id);
                if (!blogPosts.Any()) return model;
                var months = new SortedDictionary<DateTime, int>();

                var blogPost = blogPosts[^1];
                var first = blogPost.StartDateUtc ?? blogPost.CreatedOnUtc;
                while (DateTime.SpecifyKind(first, DateTimeKind.Utc) <= DateTime.UtcNow.AddMonths(1))
                {
                    var list = blogPosts.GetPostsByDate(new DateTime(first.Year, first.Month, 1), new DateTime(first.Year, first.Month, 1).AddMonths(1).AddSeconds(-1));
                    if (list.Any())
                    {
                        var date = new DateTime(first.Year, first.Month, 1);
                        months.Add(date, list.Count);
                    }

                    first = first.AddMonths(1);
                }


                var current = 0;
                foreach (var (date, blogPostCount) in months)
                {
                    if (current == 0)
                        current = date.Year;

                    if (date.Year > current || !model.Any())
                    {
                        var yearModel = new BlogPostYearModel {
                            Year = date.Year
                        };
                        model.Insert(0, yearModel);
                    }

                    model.First().Months.Insert(0, new BlogPostMonthModel {
                        Month = date.Month,
                        BlogPostCount = blogPostCount
                    });

                    current = date.Year;
                }
                return model;
            });
            return cachedModel;
        }
    }
}
