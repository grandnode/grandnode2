using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain;
using Grand.Domain.Blogs;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.Localization;
using Grand.Domain.News;
using Grand.Domain.Pages;
using Grand.Infrastructure.Caching;
using Grand.Web.Features.Handlers.Common;
using Grand.Web.Features.Models.Common;
using Grand.Web.Models.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Common;

[TestClass]
public class GetSitemapHandlerTests
{
    private Mock<IPageService> _pageServiceMock;
    private GetSitemapHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _pageServiceMock = new Mock<IPageService>();

        var cacheMock = new Mock<ICacheBase>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<SitemapModel>>>()))
            .Returns((string _, Func<Task<SitemapModel>> acquire) => acquire());

        var blogServiceMock = new Mock<IBlogService>();
        blogServiceMock.Setup(s => s.GetAllBlogPosts(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new PagedList<BlogPost>());

        var knowledgebaseMock = new Mock<IKnowledgebaseService>();
        knowledgebaseMock.Setup(s => s.GetPublicKnowledgebaseArticles()).ReturnsAsync(new List<KnowledgebaseArticle>());

        _handler = new GetSitemapHandler(cacheMock.Object, new Mock<ICategoryService>().Object,
            new Mock<IBrandService>().Object, new Mock<IProductService>().Object, _pageServiceMock.Object,
            blogServiceMock.Object, knowledgebaseMock.Object, new CommonSettings(), new BlogSettings(),
            new NewsSettings(), new KnowledgebaseSettings());
    }

    private static Page CreatePage(string id, bool published, bool includeInSitemap)
    {
        return new Page {
            Id = id,
            SystemName = id,
            Title = id,
            Published = published,
            IncludeInSitemap = includeInSitemap
        };
    }

    [TestMethod]
    public async Task Handle_ListsOnlyPublishedPagesIncludedInSitemap()
    {
        _pageServiceMock.Setup(s => s.GetAllPages("store-1", It.IsAny<bool>())).ReturnsAsync(new List<Page> {
            CreatePage("published", true, true),
            CreatePage("unpublished", false, true),
            CreatePage("hidden", true, false)
        });

        var model = await _handler.Handle(new GetSitemap {
            Customer = new Customer(),
            Store = new Grand.Domain.Stores.Store { Id = "store-1" },
            Language = new Language { Id = "lang-1" }
        }, CancellationToken.None);

        Assert.AreEqual(1, model.Pages.Count);
        Assert.AreEqual("published", model.Pages[0].Id);
    }
}
