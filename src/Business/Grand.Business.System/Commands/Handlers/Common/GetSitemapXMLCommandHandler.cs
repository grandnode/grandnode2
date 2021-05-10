using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Marketing.Interfaces.Knowledgebase;
using Grand.Business.Storage.Interfaces;
using Grand.Business.System.Commands.Models.Common;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.News;
using Grand.Domain.Stores;
using Grand.Infrastructure.Configuration;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Grand.Business.System.Commands.Handlers.Common
{
    public class GetSitemapXMLCommandHandler : IRequestHandler<GetSitemapXmlCommand, string>
    {
        private const string DateFormat = @"yyyy-MM-dd";

        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly IPageService _pageService;
        private readonly IBlogService _blogService;
        private readonly IKnowledgebaseService _knowledgebaseService;
        private readonly IPictureService _pictureService;
        private readonly CommonSettings _commonSettings;
        private readonly KnowledgebaseSettings _knowledgebaseSettings;
        private readonly NewsSettings _newsSettings;
        private readonly BlogSettings _blogSettings;
        private readonly LinkGenerator _linkGenerator;
        private readonly AppConfig _appConfig;

        private GetSitemapXmlCommand _request;

        public GetSitemapXMLCommandHandler(
            ICategoryService categoryService,
            IProductService productService,
            IBrandService brandService,
            IPageService pageService,
            IBlogService blogService,
            IPictureService pictureService,
            IKnowledgebaseService knowledgebaseService,
            CommonSettings commonSettings,
            BlogSettings blogSettings,
            KnowledgebaseSettings knowledgebaseSettings,
            NewsSettings newsSettings,
            LinkGenerator linkGenerator,
            AppConfig appConfig)
        {
            _categoryService = categoryService;
            _productService = productService;
            _brandService = brandService;
            _pageService = pageService;
            _blogService = blogService;
            _pictureService = pictureService;
            _commonSettings = commonSettings;
            _knowledgebaseService = knowledgebaseService;
            _knowledgebaseSettings = knowledgebaseSettings;
            _newsSettings = newsSettings;
            _blogSettings = blogSettings;
            _linkGenerator = linkGenerator;
            _appConfig = appConfig;
        }

        public async Task<string> Handle(GetSitemapXmlCommand request, CancellationToken cancellationToken)
        {
            _request = request;
            return await Generate(request.Language, request.Store);

        }
        private string RemoveBom(string p)
        {
            string BOMMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (p.StartsWith(BOMMarkUtf8))
                p = p.Remove(0, BOMMarkUtf8.Length);
            return p.Replace("\0", "");
        }
        private async Task<string> Generate(Language language, Store store)
        {
            using (var stream = new MemoryStream())
            {
                await Generate(stream, language, store);
                return RemoveBom(Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        private async Task Generate(Stream stream, Language language, Store store)
        {
            //generate all URLs for the sitemap
            var sitemapUrls = await GenerateUrls(language, store);

            await WriteSitemap(stream, sitemapUrls);

        }

        /// <summary>
        /// Represents sitemap URL 
        /// </summary>
        class SitemapUrl
        {
            public SitemapUrl(string location, string image, UpdateFrequency frequency, DateTime updatedOn)
            {
                Location = location;
                Image = image;
                UpdateFrequency = frequency;
                UpdatedOn = updatedOn;
            }

            /// <summary>
            /// Gets or sets URL of the page
            /// </summary>
            public string Location { get; set; }

            /// <summary>
            /// Gets or sets URL of the image
            /// </summary>
            public string Image { get; set; }

            /// <summary>
            /// Gets or sets a value indicating how frequently the page is likely to change
            /// </summary>
            public UpdateFrequency UpdateFrequency { get; set; }

            /// <summary>
            /// Gets or sets the date of last modification of the file
            /// </summary>
            public DateTime UpdatedOn { get; set; }
        }

        /// <summary>
        /// Get HTTP protocol
        /// </summary>
        /// <returns>Protocol name as string</returns>
        private string GetHttpProtocol()
        {
            return _request.Store.SslEnabled ? "https" : "http";
        }
        /// <summary>
        /// Get HTTP protocol
        /// </summary>
        /// <returns>Protocol name as string</returns>
        private HostString GetHost()
        {
            return _request.Store.SslEnabled ?
                new HostString(_request.Store.SecureUrl.Replace("https://", "").Trim('/')) :
                new HostString(_request.Store.Url.Replace("http://", "").Trim('/'));
        }
        /// <summary>
        /// Get store location
        /// </summary>
        /// <returns>Store url</returns>
        private string GetStoreLocation()
        {
            return _request.Store.SslEnabled ? _request.Store.SecureUrl : _request.Store.Url;
        }

        private async Task<IList<SitemapUrl>> GenerateUrls(Language language, Store store)
        {
            var sitemapUrls = new List<SitemapUrl>();
            var routeValues = !_appConfig.SeoFriendlyUrlsForLanguagesEnabled ? null : new { language = language.UniqueSeoCode };

            //home page
            var homePageUrl = _linkGenerator.GetUriByRouteValues("HomePage", routeValues, GetHttpProtocol(), GetHost());
            sitemapUrls.Add(new SitemapUrl(homePageUrl, string.Empty, UpdateFrequency.Weekly, DateTime.UtcNow));

            //search products
            var productSearchUrl = _linkGenerator.GetUriByRouteValues("ProductSearch", routeValues, GetHttpProtocol(), GetHost());
            sitemapUrls.Add(new SitemapUrl(productSearchUrl, string.Empty, UpdateFrequency.Weekly, DateTime.UtcNow));

            //contact us
            var contactUsUrl = _linkGenerator.GetUriByRouteValues("ContactUs", routeValues, GetHttpProtocol(), GetHost());
            sitemapUrls.Add(new SitemapUrl(contactUsUrl, string.Empty, UpdateFrequency.Weekly, DateTime.UtcNow));

            //news
            if (_newsSettings.Enabled)
            {
                var url = _linkGenerator.GetUriByRouteValues("NewsArchive", routeValues, GetHttpProtocol(), GetHost());
                sitemapUrls.Add(new SitemapUrl(url, string.Empty, UpdateFrequency.Weekly, DateTime.UtcNow));
            }

            //blog
            if (_blogSettings.Enabled)
            {
                var url = _linkGenerator.GetUriByRouteValues("Blog", routeValues, GetHttpProtocol(), GetHost());
                sitemapUrls.Add(new SitemapUrl(url, string.Empty, UpdateFrequency.Weekly, DateTime.UtcNow));
            }

            //knowledgebase
            if (_knowledgebaseSettings.Enabled)
            {
                var url = _linkGenerator.GetUriByRouteValues("Knowledgebase", routeValues, GetHttpProtocol(), GetHost());
                sitemapUrls.Add(new SitemapUrl(url, string.Empty, UpdateFrequency.Weekly, DateTime.UtcNow));
            }

            //categories
            if (_commonSettings.SitemapIncludeCategories)
                sitemapUrls.AddRange(await GetCategoryUrls("", language, store));

            //brands
            if (_commonSettings.SitemapIncludeBrands)
                sitemapUrls.AddRange(await GetBrandUrls(language, store));

            //products
            if (_commonSettings.SitemapIncludeProducts)
                sitemapUrls.AddRange(await GetProductUrls(language, store));

            //topics
            sitemapUrls.AddRange(await GetPagesUrls(language, store));

            //blog posts
            sitemapUrls.AddRange(await GetBlogPostsUrls(language, store));

            //knowledgebase articles
            sitemapUrls.AddRange(await GetKnowledgebaseUrls(language));

            //custom URLs
            sitemapUrls.AddRange(GetCustomUrls());

            return sitemapUrls;
        }

        private async Task<IEnumerable<SitemapUrl>> GetCategoryUrls(string parentCategoryId, Language language, Store store)
        {
            var allCategoriesByParentCategoryId = await _categoryService.GetAllCategoriesByParentCategoryId(parentCategoryId: parentCategoryId);
            var categories = new List<SitemapUrl>();
            var storeLocation = GetStoreLocation();
            foreach (var category in allCategoriesByParentCategoryId)
            {
               
                var url =
                    _appConfig.SeoFriendlyUrlsForLanguagesEnabled ?
                    _linkGenerator.GetUriByRouteValues("Category", new { SeName = category.GetSeName(language.Id), language = language.UniqueSeoCode }, GetHttpProtocol(), GetHost())
                    :
                    _linkGenerator.GetUriByRouteValues("Category", new { SeName = category.GetSeName(language.Id) }, GetHttpProtocol(), GetHost());

                var imageurl = string.Empty;
                if (_commonSettings.SitemapIncludeImage)
                {
                    if (!string.IsNullOrEmpty(category.PictureId))
                    {
                        imageurl = await _pictureService.GetPictureUrl(category.PictureId, showDefaultPicture: false, storeLocation: storeLocation);
                    }
                }
                categories.Add(new SitemapUrl(url, imageurl, UpdateFrequency.Weekly, category.UpdatedOnUtc));
                categories.AddRange(await GetCategoryUrls(category.Id, language, store));
            }
            return categories;
        }

        private async Task<IEnumerable<SitemapUrl>> GetBrandUrls(Language language, Store store)
        {
            var brands = await _brandService.GetAllBrands(storeId: store.Id);
            var _brands = new List<SitemapUrl>();
            var storeLocation = GetStoreLocation();
            foreach (var brand in brands)
            {
                var url =
                    _appConfig.SeoFriendlyUrlsForLanguagesEnabled ?
                    _linkGenerator.GetUriByRouteValues("Brand", new { SeName = brand.GetSeName(language.Id), language = language.UniqueSeoCode }, GetHttpProtocol(), GetHost())
                    :
                    _linkGenerator.GetUriByRouteValues("Brand", new { SeName = brand.GetSeName(language.Id) }, GetHttpProtocol(), GetHost());

                var imageurl = string.Empty;
                if (_commonSettings.SitemapIncludeImage)
                {
                    if (!string.IsNullOrEmpty(brand.PictureId))
                    {
                        imageurl = await _pictureService.GetPictureUrl(brand.PictureId, showDefaultPicture: false, storeLocation: storeLocation);
                    }
                }
                _brands.Add(new SitemapUrl(url, imageurl, UpdateFrequency.Weekly, brand.UpdatedOnUtc));
            }
            return _brands;
        }

        private async Task<IEnumerable<SitemapUrl>> GetProductUrls(Language language, Store store)
        {
            var search = await _productService.SearchProducts(storeId: store.Id,
                visibleIndividuallyOnly: true, orderBy: ProductSortingEnum.CreatedOn);
            var storeLocation = GetStoreLocation();
            var products = new List<SitemapUrl>();
            foreach (var product in search.products)
            {
                var url =
                    _appConfig.SeoFriendlyUrlsForLanguagesEnabled ?
                    _linkGenerator.GetUriByRouteValues("Product", new { SeName = product.GetSeName(language.Id), language = language.UniqueSeoCode }, GetHttpProtocol(), GetHost())
                    :
                    _linkGenerator.GetUriByRouteValues("Product", new { SeName = product.GetSeName(language.Id) }, GetHttpProtocol(), GetHost());

                var imageurl = string.Empty;
                if (_commonSettings.SitemapIncludeImage)
                {
                    if (!string.IsNullOrEmpty(product.ProductPictures.FirstOrDefault()?.PictureId))
                    {
                        imageurl = await _pictureService.GetPictureUrl(product.ProductPictures.FirstOrDefault().PictureId, showDefaultPicture: false, storeLocation: storeLocation);
                    }
                }
                products.Add(new SitemapUrl(url, imageurl, UpdateFrequency.Weekly, product.UpdatedOnUtc));
            }
            return products;

        }

        private async Task<IEnumerable<SitemapUrl>> GetPagesUrls(Language language, Store store)
        {
            var now = DateTime.UtcNow;
            return (await _pageService.GetAllPages(storeId: store.Id))
                .Where(t => t.IncludeInSitemap && (!t.StartDateUtc.HasValue || t.StartDateUtc < now) && (!t.EndDateUtc.HasValue || t.EndDateUtc > now))
                .Select(topic =>
                {
                    var url =
                        _appConfig.SeoFriendlyUrlsForLanguagesEnabled ?
                        _linkGenerator.GetUriByRouteValues("Topic", new { SeName = topic.GetSeName(language.Id), language = language.UniqueSeoCode }, GetHttpProtocol(), GetHost())
                        :
                        _linkGenerator.GetUriByRouteValues("Topic", new { SeName = topic.GetSeName(language.Id) }, GetHttpProtocol(), GetHost());

                    return new SitemapUrl(url, string.Empty, UpdateFrequency.Weekly, DateTime.UtcNow);
                });
        }

        private async Task<IEnumerable<SitemapUrl>> GetBlogPostsUrls(Language language, Store store)
        {
            var blogposts = await _blogService.GetAllBlogPosts(storeId: store.Id);
            var blog = new List<SitemapUrl>();
            var storeLocation = GetStoreLocation();
            foreach (var blogpost in blogposts)
            {
                var url =
                    _appConfig.SeoFriendlyUrlsForLanguagesEnabled ?
                    _linkGenerator.GetUriByRouteValues("BlogPost", new { SeName = blogpost.GetSeName(language.Id), language = language.UniqueSeoCode }, GetHttpProtocol(), GetHost())
                    :
                    _linkGenerator.GetUriByRouteValues("BlogPost", new { SeName = blogpost.GetSeName(language.Id) }, GetHttpProtocol(), GetHost());

                var imageurl = string.Empty;
                if (_commonSettings.SitemapIncludeImage)
                {
                    if (!string.IsNullOrEmpty(blogpost.PictureId))
                    {
                        imageurl = await _pictureService.GetPictureUrl(blogpost.PictureId, showDefaultPicture: false, storeLocation: storeLocation);
                    }
                }
                blog.Add(new SitemapUrl(url, imageurl, UpdateFrequency.Weekly, DateTime.UtcNow));
            }

            return blog;
        }

        private async Task<IEnumerable<SitemapUrl>> GetKnowledgebaseUrls(Language language)
        {
            var knowledgebasearticles = await _knowledgebaseService.GetPublicKnowledgebaseArticles();
            var knowledgebase = new List<SitemapUrl>();
            foreach (var knowledgebasearticle in knowledgebasearticles)
            {
                var url =
                    _appConfig.SeoFriendlyUrlsForLanguagesEnabled ?
                    _linkGenerator.GetUriByRouteValues("KnowledgebaseArticle", new { SeName = knowledgebasearticle.GetSeName(language.Id), language = language.UniqueSeoCode }, GetHttpProtocol(), GetHost())
                    :
                    _linkGenerator.GetUriByRouteValues("KnowledgebaseArticle", new { SeName = knowledgebasearticle.GetSeName(language.Id) }, GetHttpProtocol(), GetHost());

                knowledgebase.Add(new SitemapUrl(url, string.Empty, UpdateFrequency.Weekly, DateTime.UtcNow));
            }

            return knowledgebase;
        }

        private IEnumerable<SitemapUrl> GetCustomUrls()
        {
            var storeLocation = GetStoreLocation();

            return _commonSettings.SitemapCustomUrls.Select(customUrl =>
                new SitemapUrl(string.Concat(storeLocation, customUrl), string.Empty, UpdateFrequency.Weekly, DateTime.UtcNow));
        }

        private string XmlEncode(string str)
        {
            if (str == null)
                return null;
            str = Regex.Replace(str, @"[^\u0009\u000A\u000D\u0020-\uD7FF\uE000-\uFFFD]", "", RegexOptions.Compiled);
            if (str == null)
                return null;

            var xwSettings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Auto
            };
            using (var sw = new StringWriter())
            using (var xwr = XmlWriter.Create(sw, xwSettings))
            {
                xwr.WriteString(str);
                xwr.Flush();
                return sw.ToString();
            }
        }


        private async Task WriteSitemap(Stream stream, IList<SitemapUrl> sitemapUrls)
        {
            var xwSettings = new XmlWriterSettings
            {
                ConformanceLevel = ConformanceLevel.Auto,
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\r\n",
                Encoding = Encoding.UTF8,
                Async = true
            };

            using (var writer = XmlWriter.Create(stream, xwSettings))
            {
                await writer.WriteStartDocumentAsync();
                writer.WriteStartElement("urlset");
                await writer.WriteAttributeStringAsync("urlset", "xmlns", null, "http://www.sitemaps.org/schemas/sitemap/0.9");

                if (_commonSettings.SitemapIncludeImage)
                    await writer.WriteAttributeStringAsync("xmlns", "image", null, "http://www.google.com/schemas/sitemap-image/1.1");

                await writer.WriteAttributeStringAsync("xsi", "schemaLocation", null, "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd");

                foreach (var url in sitemapUrls)
                {
                    writer.WriteStartElement("url");
                    var location = XmlEncode(url.Location);
                    writer.WriteElementString("loc", location);

                    if (_commonSettings.SitemapIncludeImage && !string.IsNullOrEmpty(url.Image))
                    {
                        writer.WriteStartElement("image", "image", null);
                        writer.WriteElementString("image", "loc", null, url.Image);
                        writer.WriteEndElement();
                    }

                    writer.WriteElementString("changefreq", url.UpdateFrequency.ToString().ToLowerInvariant());
                    writer.WriteElementString("lastmod", url.UpdatedOn.ToString(DateFormat));
                    writer.WriteEndElement();
                }

                await writer.WriteEndElementAsync();
                await writer.FlushAsync();
            }
        }

    }

    /// <summary>
    /// Represents a sitemap update frequency
    /// </summary>
    public enum UpdateFrequency
    {
        /// <summary>
        /// Always
        /// </summary>
        Always,
        /// <summary>
        /// Hourly
        /// </summary>
        Hourly,
        /// <summary>
        /// Daily
        /// </summary>
        Daily,
        /// <summary>
        /// Weekly
        /// </summary>
        Weekly,
        /// <summary>
        /// Monthly
        /// </summary>
        Monthly,
        /// <summary>
        /// Yearly
        /// </summary>
        Yearly,
        /// <summary>
        /// Never
        /// </summary>
        Never
    }
}
