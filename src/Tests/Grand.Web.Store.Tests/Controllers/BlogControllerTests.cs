using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Blogs;
using Grand.Domain.Seo;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Authorization;
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
    public void HasAreaAttribute_SetToStore()
    {
        var attr = typeof(BlogController).GetCustomAttributes(typeof(AreaAttribute), false)
            .Cast<AreaAttribute>().SingleOrDefault();
        Assert.IsNotNull(attr);
        Assert.AreEqual("Store", attr.RouteValue);
    }

    [TestMethod]
    public void HasAuthorizeStoreAttribute()
    {
        Assert.IsTrue(typeof(BlogController).GetCustomAttributes(typeof(Grand.Web.Common.Filters.AuthorizeStoreAttribute), true).Length > 0);
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
}
