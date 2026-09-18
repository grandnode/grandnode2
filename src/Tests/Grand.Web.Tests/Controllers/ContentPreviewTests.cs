using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Blogs;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.News;
using Grand.Domain.Pages;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Controllers;
using Grand.Web.Features.Handlers.Pages;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Features.Models.News;
using Grand.Web.Features.Models.Pages;
using Grand.Web.Models.Blogs;
using Grand.Web.Models.News;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

/// <summary>
///     The admin "Preview" button of a news item, blog post or page must open the storefront page for the manager
///     even when the entity is unpublished or outside its date range; a customer is still sent away.
/// </summary>
[TestClass]
public class ContentPreviewTests
{
    private Mock<IAclService> _aclServiceMock;
    private Mock<IContextAccessor> _contextAccessorMock;
    private Customer _customer;
    private Mock<IMediator> _mediatorMock;
    private Mock<IPermissionService> _permissionServiceMock;

    [TestInitialize]
    public void Init()
    {
        _customer = new Customer { Id = "c1" };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(x => x.CurrentCustomer).Returns(_customer);
        workContextMock.Setup(x => x.WorkingLanguage).Returns(new Language { Id = "l1" });
        var storeContextMock = new Mock<IStoreContext>();
        storeContextMock.Setup(x => x.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "s1" });
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(x => x.WorkContext).Returns(workContextMock.Object);
        _contextAccessorMock.Setup(x => x.StoreContext).Returns(storeContextMock.Object);

        _aclServiceMock = new Mock<IAclService>();
        _aclServiceMock.Setup(x => x.Authorize(It.IsAny<NewsItem>(), It.IsAny<string>())).Returns(true);
        _aclServiceMock.Setup(x => x.Authorize(It.IsAny<BlogPost>(), It.IsAny<string>())).Returns(true);
        _aclServiceMock.Setup(x => x.Authorize(It.IsAny<Page>(), It.IsAny<Customer>())).Returns(true);

        _permissionServiceMock = new Mock<IPermissionService>();
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetNewsItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewsItemModel());
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetBlogPost>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogPostModel());
    }

    private static ControllerContext GetContext()
    {
        return new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Method = "GET" } } };
    }

    private NewsController NewsController(NewsItem newsItem)
    {
        var newsServiceMock = new Mock<INewsService>();
        newsServiceMock.Setup(x => x.GetNewsById(newsItem.Id)).ReturnsAsync(newsItem);
        return new NewsController(newsServiceMock.Object, _contextAccessorMock.Object,
            new Mock<ITranslationService>().Object, _aclServiceMock.Object, _permissionServiceMock.Object,
            _mediatorMock.Object, new NewsSettings { Enabled = true }) { ControllerContext = GetContext() };
    }

    private BlogController BlogController(BlogPost blogPost)
    {
        var blogServiceMock = new Mock<IBlogService>();
        blogServiceMock.Setup(x => x.GetBlogPostById(blogPost.Id)).ReturnsAsync(blogPost);
        return new BlogController(_mediatorMock.Object, blogServiceMock.Object,
            new Mock<ITranslationService>().Object, _contextAccessorMock.Object,
            new BlogSettings { Enabled = true }) { ControllerContext = GetContext() };
    }

    private void GivenManager(string permission, bool granted)
    {
        _permissionServiceMock.Setup(x => x.Authorize(It.Is<Permission>(p => p.SystemName == permission)))
            .ReturnsAsync(granted);
    }

    [TestMethod]
    [DataRow(false, null)]
    [DataRow(true, 30)]
    public async Task NewsItem_Hidden_CustomerIsRedirectedHome(bool published, int? startInDays)
    {
        var controller = NewsController(new NewsItem {
            Id = "n1", Published = published,
            StartDateUtc = startInDays.HasValue ? DateTime.UtcNow.AddDays(startInDays.Value) : null
        });
        GivenManager(StandardPermission.ManageNews.SystemName, false);

        var result = await controller.NewsItem("n1");

        Assert.IsInstanceOfType<RedirectToRouteResult>(result.Result);
    }

    [TestMethod]
    [DataRow(false, null)]
    [DataRow(true, 30)]
    public async Task NewsItem_Hidden_ManagerCanPreview(bool published, int? startInDays)
    {
        var controller = NewsController(new NewsItem {
            Id = "n1", Published = published,
            StartDateUtc = startInDays.HasValue ? DateTime.UtcNow.AddDays(startInDays.Value) : null
        });
        GivenManager(StandardPermission.ManageNews.SystemName, true);

        var result = await controller.NewsItem("n1");

        Assert.IsInstanceOfType<ViewResult>(result.Result);
    }

    [TestMethod]
    public async Task BlogPost_NotStarted_CustomerIsRedirectedHome()
    {
        var controller = BlogController(new BlogPost { Id = "b1", StartDateUtc = DateTime.UtcNow.AddDays(30) });
        GivenManager(StandardPermission.ManageBlog.SystemName, false);

        var result = await controller.BlogPost("b1", _aclServiceMock.Object, _permissionServiceMock.Object);

        Assert.IsInstanceOfType<RedirectToRouteResult>(result.Result);
    }

    [TestMethod]
    public async Task BlogPost_NotStarted_ManagerCanPreview()
    {
        var controller = BlogController(new BlogPost { Id = "b1", StartDateUtc = DateTime.UtcNow.AddDays(30) });
        GivenManager(StandardPermission.ManageBlog.SystemName, true);

        var result = await controller.BlogPost("b1", _aclServiceMock.Object, _permissionServiceMock.Object);

        Assert.IsInstanceOfType<ViewResult>(result.Result);
    }

    [TestMethod]
    [DataRow(false, false, null)]
    [DataRow(false, true, 30)]
    [DataRow(true, false, null)]
    [DataRow(true, true, 30)]
    public async Task GetPageBlock_HiddenPage_ReturnedOnlyForPreview(bool showHidden, bool published, int? startInDays)
    {
        var page = new Page {
            Id = "p1", Published = published,
            StartDateUtc = startInDays.HasValue ? DateTime.UtcNow.AddDays(startInDays.Value) : null
        };
        var pageServiceMock = new Mock<IPageService>();
        pageServiceMock.Setup(x => x.GetPageById("p1")).ReturnsAsync(page);
        var handler = new GetPageBlockHandler(pageServiceMock.Object, _contextAccessorMock.Object,
            _aclServiceMock.Object, new Mock<IDateTimeService>().Object);

        var model = await handler.Handle(new GetPageBlock { PageId = "p1", ShowHidden = showHidden }, default);

        Assert.AreEqual(showHidden, model != null);
    }
}
