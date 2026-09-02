using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Blogs;
using Grand.Domain.Seo;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Blogs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

// Characterization tests for the merged Blog access-check behavior (ARCH-001 Blog consolidation).
[TestClass]
public class BaseBlogControllerTests
{
    private class TestBlogController(
        IBlogService blogService,
        IBlogViewModelService blogViewModelService,
        ILanguageService languageService,
        ITranslationService translationService,
        IStoreService storeService,
        IDateTimeService dateTimeService,
        IPictureViewModelService pictureViewModelService,
        SeoSettings seoSettings,
        IAdminDataScope<BlogPost> postScope,
        IAdminDataScope<BlogCategory> categoryScope)
        : BaseBlogController(blogService, blogViewModelService, languageService, translationService,
            storeService, dateTimeService, pictureViewModelService, seoSettings, postScope, categoryScope);

    private TestBlogController _controller;
    private Mock<IBlogService> _blogServiceMock;
    private Mock<IBlogViewModelService> _blogViewModelServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IAdminDataScope<BlogPost>> _postScopeMock;
    private Mock<IAdminDataScope<BlogCategory>> _categoryScopeMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<BlogPostProfile>();
            cfg.AddProfile<BlogCategoryProfile>();
        });
        AutoMapperConfig.Init(mapperConfig);

        _blogServiceMock = new Mock<IBlogService>();
        _blogViewModelServiceMock = new Mock<IBlogViewModelService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _postScopeMock = new Mock<IAdminDataScope<BlogPost>>();
        _postScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _categoryScopeMock = new Mock<IAdminDataScope<BlogCategory>>();
        _categoryScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>()))
            .ReturnsAsync(new List<Grand.Domain.Localization.Language>());

        _controller = new TestBlogController(
            _blogServiceMock.Object,
            _blogViewModelServiceMock.Object,
            languageServiceMock.Object,
            _translationServiceMock.Object,
            new Mock<IStoreService>().Object,
            new Mock<IDateTimeService>().Object,
            new Mock<IPictureViewModelService>().Object,
            new SeoSettings(),
            _postScopeMock.Object,
            _categoryScopeMock.Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public async Task EditGet_PostNotFound_RedirectsToList()
    {
        _blogServiceMock.Setup(b => b.GetBlogPostById("missing")).ReturnsAsync((BlogPost)null);

        var result = await _controller.Edit("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _postScopeMock.Verify(s => s.CanView(It.IsAny<BlogPost>()), Times.Never);
    }

    [TestMethod]
    public async Task EditGet_ScopeDeniesView_RedirectsToList()
    {
        var post = new BlogPost { Id = "p1" };
        _blogServiceMock.Setup(b => b.GetBlogPostById("p1")).ReturnsAsync(post);
        _postScopeMock.Setup(s => s.CanView(post)).ReturnsAsync(false);

        var result = await _controller.Edit("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task EditGet_ScopeAllowsView_ReturnsViewWithModel()
    {
        var post = new BlogPost { Id = "p1", Title = "Hello" };
        _blogServiceMock.Setup(b => b.GetBlogPostById("p1")).ReturnsAsync(post);
        _postScopeMock.Setup(s => s.CanView(post)).ReturnsAsync(true);

        var result = await _controller.Edit("p1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreEqual("Hello", ((BlogPostModel)view.Model).Title);
    }

    [TestMethod]
    public async Task EditPost_ScopeDeniesAccess_RedirectsToEdit()
    {
        var post = new BlogPost { Id = "p1" };
        _blogServiceMock.Setup(b => b.GetBlogPostById("p1")).ReturnsAsync(post);
        _postScopeMock.Setup(s => s.HasAccess(post)).ReturnsAsync(false);

        var result = await _controller.Edit(new BlogPostModel { Id = "p1" }, false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        _blogViewModelServiceMock.Verify(v => v.UpdateBlogPostModel(It.IsAny<BlogPostModel>(), It.IsAny<BlogPost>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_ScopeDeniesAccess_RedirectsToEditWithoutDeleting()
    {
        var post = new BlogPost { Id = "p1" };
        _blogServiceMock.Setup(b => b.GetBlogPostById("p1")).ReturnsAsync(post);
        _postScopeMock.Setup(s => s.HasAccess(post)).ReturnsAsync(false);

        var result = await _controller.Delete("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("p1", redirect.RouteValues["id"]);
        _blogServiceMock.Verify(b => b.DeleteBlogPost(It.IsAny<BlogPost>()), Times.Never);
    }

    [TestMethod]
    public async Task CreatePost_StoreScoped_ForcesModelStores()
    {
        _postScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var inserted = new BlogPost { Id = "new-1" };
        _blogViewModelServiceMock
            .Setup(v => v.InsertBlogPostModel(It.IsAny<BlogPostModel>()))
            .ReturnsAsync(inserted)
            .Callback<BlogPostModel>(m => Assert.AreEqual("store-1", m.Stores.Single()));

        await _controller.Create(new BlogPostModel { Title = "N" }, false);

        _blogViewModelServiceMock.Verify(v => v.InsertBlogPostModel(It.IsAny<BlogPostModel>()), Times.Once);
    }

    [TestMethod]
    public async Task CreatePost_GlobalScoped_LeavesModelStoresUntouched()
    {
        _postScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var inserted = new BlogPost { Id = "new-1" };
        var submitted = new BlogPostModel { Title = "N", Stores = ["explicit-store"] };
        _blogViewModelServiceMock
            .Setup(v => v.InsertBlogPostModel(It.IsAny<BlogPostModel>()))
            .ReturnsAsync(inserted)
            .Callback<BlogPostModel>(m => Assert.AreEqual("explicit-store", m.Stores.Single()));

        await _controller.Create(submitted, false);

        _blogViewModelServiceMock.Verify(v => v.InsertBlogPostModel(It.IsAny<BlogPostModel>()), Times.Once);
    }
}
