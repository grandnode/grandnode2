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
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.DataSource;
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

    [TestMethod]
    public async Task PicturePopupGet_ScopeDeniesAccess_ReturnsDeniedContent()
    {
        var post = new BlogPost { Id = "p1", PictureId = "pic-1" };
        _blogServiceMock.Setup(b => b.GetBlogPostById("p1")).ReturnsAsync(post);
        _postScopeMock.Setup(s => s.HasAccess(post)).ReturnsAsync(false);

        var result = await _controller.PicturePopup("p1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("You don't have access to this blog post", content.Content);
    }

    [TestMethod]
    public async Task PicturePopupGet_PostNotFound_ReturnsNotExistContent()
    {
        _blogServiceMock.Setup(b => b.GetBlogPostById("missing")).ReturnsAsync((BlogPost)null);

        var result = await _controller.PicturePopup("missing");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Blog post not exist", content.Content);
        _postScopeMock.Verify(s => s.HasAccess(It.IsAny<BlogPost>()), Times.Never);
    }

    [TestMethod]
    public async Task PicturePopupPost_ScopeDeniesAccess_ReturnsDeniedContent()
    {
        var post = new BlogPost { Id = "p1", PictureId = "pic-1" };
        _blogServiceMock.Setup(b => b.GetBlogPostById("p1")).ReturnsAsync(post);
        _postScopeMock.Setup(s => s.HasAccess(post)).ReturnsAsync(false);

        var model = new PictureModel { ObjectId = "p1", Id = "pic-1" };
        var result = await _controller.PicturePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("You don't have access to this blog post", content.Content);
    }

    [TestMethod]
    public async Task CategoryListPost_ForcesScopeDefaultStoreId()
    {
        _categoryScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _blogServiceMock.Setup(b => b.GetAllBlogCategories("store-1")).ReturnsAsync(new List<BlogCategory>());

        await _controller.CategoryList(new DataSourceRequest { Page = 1, PageSize = 10 });

        _blogServiceMock.Verify(b => b.GetAllBlogCategories("store-1"), Times.Once);
    }

    [TestMethod]
    public async Task CategoryListPost_GlobalScope_PassesEmptyString()
    {
        _categoryScopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _blogServiceMock.Setup(b => b.GetAllBlogCategories("")).ReturnsAsync(new List<BlogCategory>());

        await _controller.CategoryList(new DataSourceRequest { Page = 1, PageSize = 10 });

        _blogServiceMock.Verify(b => b.GetAllBlogCategories(""), Times.Once);
    }

    [TestMethod]
    public async Task CategoryEditGet_ScopeDeniesAccess_RedirectsToCategoryList()
    {
        var category = new BlogCategory { Id = "c1" };
        _blogServiceMock.Setup(b => b.GetBlogCategoryById("c1")).ReturnsAsync(category);
        _categoryScopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(false);

        var result = await _controller.CategoryEdit("c1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("CategoryList", redirect.ActionName);
    }

    [TestMethod]
    public async Task CategoryDelete_ScopeDeniesAccess_RedirectsToCategoryListWithoutDeleting()
    {
        var category = new BlogCategory { Id = "c1" };
        _blogServiceMock.Setup(b => b.GetBlogCategoryById("c1")).ReturnsAsync(category);
        _categoryScopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(false);

        var result = await _controller.CategoryDelete("c1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("CategoryList", redirect.ActionName);
        _blogServiceMock.Verify(b => b.DeleteBlogCategory(It.IsAny<BlogCategory>()), Times.Never);
    }

    [TestMethod]
    public async Task CategoryPostList_ScopeDeniesAccess_ReturnsKendoError()
    {
        var category = new BlogCategory { Id = "c1" };
        _blogServiceMock.Setup(b => b.GetBlogCategoryById("c1")).ReturnsAsync(category);
        _categoryScopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(false);

        var result = await _controller.CategoryPostList("c1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsNotNull(gridModel.Errors);
    }

    [TestMethod]
    public async Task BlogPostAddPopupInsert_SkipsInaccessiblePostsButLinksAccessibleOnes()
    {
        // Store's original AddSelectedPostsToBlogCategory/AddPostToBlogCategoryIfValid skip-not-fail
        // semantics: an accessible category linking one accessible and one inaccessible post must
        // link only the accessible one, not fail the whole request.
        var category = new BlogCategory { Id = "c1", BlogPosts = new List<Grand.Domain.Blogs.BlogCategoryPost>() };
        _blogServiceMock.Setup(b => b.GetBlogCategoryById("c1")).ReturnsAsync(category);
        _categoryScopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(true);

        var accessiblePost = new BlogPost { Id = "accessible-1" };
        var inaccessiblePost = new BlogPost { Id = "inaccessible-1" };
        _blogServiceMock.Setup(b => b.GetBlogPostById("accessible-1")).ReturnsAsync(accessiblePost);
        _blogServiceMock.Setup(b => b.GetBlogPostById("inaccessible-1")).ReturnsAsync(inaccessiblePost);
        _postScopeMock.Setup(s => s.HasAccess(accessiblePost)).ReturnsAsync(true);
        _postScopeMock.Setup(s => s.HasAccess(inaccessiblePost)).ReturnsAsync(false);

        var model = new AddBlogPostCategoryModel { CategoryId = "c1", SelectedBlogPostIds = ["accessible-1", "inaccessible-1"] };
        await _controller.BlogPostAddPopup(model);

        Assert.AreEqual(1, category.BlogPosts.Count);
        Assert.AreEqual("accessible-1", category.BlogPosts[0].BlogPostId);
        _blogServiceMock.Verify(b => b.UpdateBlogCategory(category), Times.Once);
    }

    [TestMethod]
    public async Task BlogPostAddPopupInsert_ScopeDeniesCategoryAccess_LinksNothing()
    {
        var category = new BlogCategory { Id = "c1", BlogPosts = new List<Grand.Domain.Blogs.BlogCategoryPost>() };
        _blogServiceMock.Setup(b => b.GetBlogCategoryById("c1")).ReturnsAsync(category);
        _categoryScopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(false);

        var model = new AddBlogPostCategoryModel { CategoryId = "c1", SelectedBlogPostIds = ["post-1"] };
        await _controller.BlogPostAddPopup(model);

        Assert.AreEqual(0, category.BlogPosts.Count);
        _blogServiceMock.Verify(b => b.UpdateBlogCategory(It.IsAny<BlogCategory>()), Times.Never);
    }

    [TestMethod]
    public async Task BlogPostAddPopupList_ForcesScopeDefaultStoreId()
    {
        _categoryScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _blogServiceMock
            .Setup(b => b.GetAllBlogPosts("store-1", null, null, 0, 10, false, null, null, ""))
            .ReturnsAsync(new Grand.Domain.PagedList<BlogPost>(new List<BlogPost>(), 0, 10));

        var model = new AddBlogPostCategoryModel { SearchStoreId = "attacker-supplied" };
        await _controller.BlogPostAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task CommentDelete_ScopeDeniesAccess_ReturnsKendoErrorWithoutDeleting()
    {
        var comment = new Grand.Domain.Blogs.BlogComment { Id = "cm1", BlogPostId = "p1" };
        var post = new BlogPost { Id = "p1" };
        _blogServiceMock.Setup(b => b.GetBlogCommentById("cm1")).ReturnsAsync(comment);
        _blogServiceMock.Setup(b => b.GetBlogPostById("p1")).ReturnsAsync(post);
        _postScopeMock.Setup(s => s.HasAccess(post)).ReturnsAsync(false);

        var result = await _controller.CommentDelete("cm1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsNotNull(gridModel.Errors);
        _blogServiceMock.Verify(b => b.DeleteBlogComment(It.IsAny<Grand.Domain.Blogs.BlogComment>()), Times.Never);
    }

    [TestMethod]
    public async Task CommentDelete_ScopeGrantsAccess_DeletesAndUpdatesCommentCount()
    {
        var comment = new Grand.Domain.Blogs.BlogComment { Id = "cm1", BlogPostId = "p1" };
        var post = new BlogPost { Id = "p1", CommentCount = 3 };
        _blogServiceMock.Setup(b => b.GetBlogCommentById("cm1")).ReturnsAsync(comment);
        _blogServiceMock.Setup(b => b.GetBlogPostById("p1")).ReturnsAsync(post);
        _postScopeMock.Setup(s => s.HasAccess(post)).ReturnsAsync(true);
        _blogServiceMock.Setup(b => b.GetBlogCommentsByBlogPostId("p1"))
            .ReturnsAsync(new List<Grand.Domain.Blogs.BlogComment> { new(), new() });

        await _controller.CommentDelete("cm1");

        _blogServiceMock.Verify(b => b.DeleteBlogComment(comment), Times.Once);
        _blogServiceMock.Verify(b => b.UpdateBlogPost(It.Is<BlogPost>(p => p.CommentCount == 2)), Times.Once);
    }
}
