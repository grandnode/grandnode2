using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Marketing.Interfaces.Knowledgebase;
using Grand.Infrastructure.Caching;
using Grand.Domain.Blogs;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.News;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Common;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Blogs;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Common;
using Grand.Web.Models.Knowledgebase;
using Grand.Web.Models.Pages;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grand.Business.Catalog.Interfaces.Brands;

namespace Grand.Web.Features.Handlers.Common
{
    public class GetSitemapHandler : IRequestHandler<GetSitemap, SitemapModel>
    {
        private readonly ICacheBase _cacheBase;
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;
        private readonly IProductService _productService;
        private readonly IPageService _pageService;
        private readonly IBlogService _blogService;
        private readonly IKnowledgebaseService _knowledgebaseService;

        private readonly CommonSettings _commonSettings;
        private readonly BlogSettings _blogSettings;
        private readonly NewsSettings _newsSettings;
        private readonly KnowledgebaseSettings _knowledgebaseSettings;

        public GetSitemapHandler(ICacheBase cacheBase,
            ICategoryService categoryService,
            IBrandService brandService,
            IProductService productService,
            IPageService pageService,
            IBlogService blogService,
            IKnowledgebaseService knowledgebaseService,
            CommonSettings commonSettings,
            BlogSettings blogSettings,
            NewsSettings newsSettings,
            KnowledgebaseSettings knowledgebaseSettings)
        {
            _cacheBase = cacheBase;
            _categoryService = categoryService;
            _brandService = brandService;
            _productService = productService;
            _pageService = pageService;
            _blogService = blogService;
            _knowledgebaseService = knowledgebaseService;

            _commonSettings = commonSettings;
            _blogSettings = blogSettings;
            _newsSettings = newsSettings;
            _knowledgebaseSettings = knowledgebaseSettings;
        }

        public async Task<SitemapModel> Handle(GetSitemap request, CancellationToken cancellationToken)
        {
            string cacheKey = string.Format(CacheKeyConst.SITEMAP_PAGE_MODEL_KEY,
                request.Language.Id,
                string.Join(",", request.Customer.GetCustomerGroupIds()),
                request.Store.Id);
            var cachedModel = await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var model = new SitemapModel
                {
                    BlogEnabled = _blogSettings.Enabled,
                    NewsEnabled = _newsSettings.Enabled,
                    KnowledgebaseEnabled = _knowledgebaseSettings.Enabled
                };
                //categories
                if (_commonSettings.SitemapIncludeCategories)
                {
                    var categories = await _categoryService.GetAllCategories(storeId: request.Store.Id);
                    model.Categories = categories.Select(x => x.ToModel(request.Language)).ToList();
                }
                //collections
                if (_commonSettings.SitemapIncludeBrands)
                {
                    var brands = await _brandService.GetAllBrands(storeId: request.Store.Id);
                    model.Brands = brands.Select(x => x.ToModel(request.Language)).ToList();
                }
                //products
                if (_commonSettings.SitemapIncludeProducts)
                {
                    //limit product to 200 until paging is supported on this page
                    var products = (await _productService.SearchProducts(
                        storeId: request.Store.Id,
                        visibleIndividuallyOnly: true,
                        pageSize: 200)).products;
                    model.Products = products.Select(product => new ProductOverviewModel
                    {
                        Id = product.Id,
                        Name = product.GetTranslation(x => x.Name, request.Language.Id),
                        ShortDescription = product.GetTranslation(x => x.ShortDescription, request.Language.Id),
                        FullDescription = product.GetTranslation(x => x.FullDescription, request.Language.Id),
                        SeName = product.GetSeName(request.Language.Id),
                    }).ToList();
                }

                //pages
                var now = DateTime.UtcNow;
                var pages = (await _pageService.GetAllPages(request.Store.Id))
                    .Where(t => t.IncludeInSitemap && (!t.StartDateUtc.HasValue || t.StartDateUtc < now) && (!t.EndDateUtc.HasValue || t.EndDateUtc > now))
                    .ToList();
                model.Pages = pages.Select(page => new PageModel
                {
                    Id = page.Id,
                    SystemName = page.GetTranslation(x => x.SystemName, request.Language.Id),
                    IncludeInSitemap = page.IncludeInSitemap,
                    IsPasswordProtected = page.IsPasswordProtected,
                    Title = page.GetTranslation(x => x.Title, request.Language.Id),
                }).ToList();

                //blog posts
                var blogposts = (await _blogService.GetAllBlogPosts(request.Store.Id))
                    .ToList();
                model.BlogPosts = blogposts.Select(blogpost => new BlogPostModel
                {
                    Id = blogpost.Id,
                    SeName = blogpost.GetSeName(request.Language.Id),
                    Title = blogpost.GetTranslation(x => x.Title, request.Language.Id),
                }).ToList();

                //knowledgebase
                var knowledgebasearticles = (await _knowledgebaseService.GetPublicKnowledgebaseArticles()).ToList();
                model.KnowledgebaseArticles = knowledgebasearticles.Select(knowledgebasearticle => new KnowledgebaseItemModel
                {
                    Id = knowledgebasearticle.Id,
                    SeName = knowledgebasearticle.GetSeName(request.Language.Id),
                    Name = knowledgebasearticle.GetTranslation(x => x.Name, request.Language.Id)
                }).ToList();

                return model;
            });
            return cachedModel;
        }
    }
}
