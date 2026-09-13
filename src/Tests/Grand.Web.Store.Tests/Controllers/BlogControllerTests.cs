using System.Reflection;
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
using Grand.Web.Common.Filters;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class BlogControllerTests
{
    private BlogController _controller;
    private Mock<IBlogService> _blogServiceMock;
    private Mock<IAdminDataScope<BlogPost>> _postScopeMock;

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
        _postScopeMock = new Mock<IAdminDataScope<BlogPost>>();

        _controller = new BlogController(
            _blogServiceMock.Object,
            new Mock<IBlogViewModelService>().Object,
            new Mock<ILanguageService>().Object,
            new Mock<ITranslationService>().Object,
            new Mock<IStoreService>().Object,
            new Mock<IDateTimeService>().Object,
            new Mock<IPictureViewModelService>().Object,
            new SeoSettings(),
            _postScopeMock.Object,
            new Mock<IAdminDataScope<BlogCategory>>().Object);

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
    public void BlogController_IsThinSubclassOfBaseBlogController()
    {
        Assert.IsTrue(typeof(BlogController).IsSubclassOf(typeof(BaseBlogController)));
    }

    [TestMethod]
    public void BlogController_HasRequiredHostAttributes()
    {
        var type = typeof(BlogController);
        Assert.IsTrue(type.IsDefined(typeof(AreaAttribute), inherit: false), "[Area] missing");
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeStoreAttribute), inherit: false), "[AuthorizeStore] missing");
        Assert.IsTrue(type.IsDefined(typeof(AuthorizeMenuAttribute), inherit: false), "[AuthorizeMenu] missing");
        Assert.IsTrue(type.IsDefined(typeof(AutoValidateAntiforgeryTokenAttribute), inherit: false), "[AutoValidateAntiforgeryToken] missing");

        var area = type.GetCustomAttribute<AreaAttribute>()!;
        Assert.AreEqual("Store", area.RouteValue);
    }

    [TestMethod]
    public async Task Preview_PostNotFound_RedirectsToList()
    {
        _blogServiceMock.Setup(b => b.GetBlogPostById("missing")).ReturnsAsync((BlogPost)null);

        var result = await _controller.Preview("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task Preview_ScopeDeniesAccess_RedirectsToList()
    {
        var post = new BlogPost { Id = "p1" };
        _blogServiceMock.Setup(b => b.GetBlogPostById("p1")).ReturnsAsync(post);
        _postScopeMock.Setup(s => s.HasAccess(post)).ReturnsAsync(false);

        var result = await _controller.Preview("p1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task Preview_ScopeGrantsAccess_ReturnsViewWithModel()
    {
        var post = new BlogPost { Id = "p1", Title = "Hello" };
        _blogServiceMock.Setup(b => b.GetBlogPostById("p1")).ReturnsAsync(post);
        _postScopeMock.Setup(s => s.HasAccess(post)).ReturnsAsync(true);

        var result = await _controller.Preview("p1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
    }

    // --- EditWarningCheck ----------------------------------------------------------------------
    //
    // Characterization tests for BlogController.EditWarningCheck, the one hand-ported piece of
    // business logic in the Blog consolidation (ARCH-001 Phase 14). Exercised indirectly through
    // the public Edit(GET) action, since EditWarningCheck itself is protected. Identical condition
    // shape to BrandController's EditWarningCheck (Phase 13).

    [TestClass]
    public class EditWarningCheckTests
    {
        private const string PermissionsResourceKey = "Admin.Content.Blog.BlogPosts.Permissions";

        private BlogController _controller;
        private Mock<IBlogService> _blogServiceMock;
        private Mock<ITranslationService> _translationServiceMock;
        private Mock<IAdminDataScope<BlogPost>> _postScopeMock;

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
            _translationServiceMock = new Mock<ITranslationService>();
            _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

            _postScopeMock = new Mock<IAdminDataScope<BlogPost>>();
            _postScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
            _postScopeMock.Setup(s => s.CanView(It.IsAny<BlogPost>())).ReturnsAsync(true);

            var languageServiceMock = new Mock<ILanguageService>();
            languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>()))
                .ReturnsAsync(new List<Grand.Domain.Localization.Language>());

            _controller = new BlogController(
                _blogServiceMock.Object,
                new Mock<IBlogViewModelService>().Object,
                languageServiceMock.Object,
                _translationServiceMock.Object,
                new Mock<IStoreService>().Object,
                new Mock<IDateTimeService>().Object,
                new Mock<IPictureViewModelService>().Object,
                new SeoSettings(),
                _postScopeMock.Object,
                new Mock<IAdminDataScope<BlogCategory>>().Object);

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

        private bool WarningWasRaised()
        {
            return _controller.TempData["grand.notifications.Warning"] is List<string> warnings
                   && warnings.Contains("resource");
        }

        [TestMethod]
        public async Task EditGet_NotLimitedToStores_RaisesPermissionsWarning()
        {
            var blogPost = new BlogPost { Id = "b1", LimitedToStores = false, Stores = [] };
            _blogServiceMock.Setup(b => b.GetBlogPostById("b1")).ReturnsAsync(blogPost);

            await _controller.Edit("b1");

            Assert.IsTrue(WarningWasRaised());
            _translationServiceMock.Verify(t => t.GetResource(PermissionsResourceKey), Times.Once);
        }

        [TestMethod]
        public async Task EditGet_LimitedToStores_ContainsDefaultStore_MultipleStores_RaisesPermissionsWarning()
        {
            var blogPost = new BlogPost { Id = "b1", LimitedToStores = true, Stores = ["store-1", "store-2"] };
            _blogServiceMock.Setup(b => b.GetBlogPostById("b1")).ReturnsAsync(blogPost);

            await _controller.Edit("b1");

            Assert.IsTrue(WarningWasRaised());
            _translationServiceMock.Verify(t => t.GetResource(PermissionsResourceKey), Times.Once);
        }

        [TestMethod]
        public async Task EditGet_LimitedToStores_ContainsDefaultStore_SingleStore_DoesNotRaiseWarning()
        {
            var blogPost = new BlogPost { Id = "b1", LimitedToStores = true, Stores = ["store-1"] };
            _blogServiceMock.Setup(b => b.GetBlogPostById("b1")).ReturnsAsync(blogPost);

            await _controller.Edit("b1");

            Assert.IsFalse(WarningWasRaised());
            _translationServiceMock.Verify(t => t.GetResource(PermissionsResourceKey), Times.Never);
        }

        [TestMethod]
        public async Task EditGet_LimitedToStores_DoesNotContainDefaultStore_DoesNotRaiseWarning()
        {
            var blogPost = new BlogPost { Id = "b1", LimitedToStores = true, Stores = ["store-2", "store-3"] };
            _blogServiceMock.Setup(b => b.GetBlogPostById("b1")).ReturnsAsync(blogPost);

            await _controller.Edit("b1");

            Assert.IsFalse(WarningWasRaised());
            _translationServiceMock.Verify(t => t.GetResource(PermissionsResourceKey), Times.Never);
        }
    }
}
