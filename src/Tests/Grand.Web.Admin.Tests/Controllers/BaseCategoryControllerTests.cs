using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Catalog;
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

// Characterization tests for the merged Category access-check behavior (ARCH-001 Category
// consolidation). Parameterized over a mocked IAdminDataScope<Category> instead of the two
// different concrete access mechanisms Admin (none) and Store (AccessToEntityByStore) used before.
[TestClass]
public class BaseCategoryControllerTests
{
    // BaseCategoryController is abstract; this minimal subclass exists only so actions under test
    // can be invoked directly. No EditWarningCheck override here (Task 3 adds that on the real
    // Store subclass) - the base's no-op default is exercised.
    private class TestCategoryController(
        ICategoryService categoryService,
        ICategoryViewModelService categoryViewModelService,
        ILanguageService languageService,
        ITranslationService translationService,
        IPictureViewModelService pictureViewModelService,
        IProductService productService,
        IAdminDataScope<Category> scope)
        : BaseCategoryController(categoryService, categoryViewModelService, languageService,
            translationService, pictureViewModelService, productService, scope);

    private TestCategoryController _controller;
    private Mock<ICategoryService> _categoryServiceMock;
    private Mock<ICategoryViewModelService> _categoryViewModelServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IAdminDataScope<Category>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<CategoryProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _categoryServiceMock = new Mock<ICategoryService>();
        _categoryViewModelServiceMock = new Mock<ICategoryViewModelService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _scopeMock = new Mock<IAdminDataScope<Category>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        var languageServiceMock = new Mock<ILanguageService>();
        languageServiceMock.Setup(l => l.GetAllLanguages(true, It.IsAny<string>())).ReturnsAsync(new List<Grand.Domain.Localization.Language>());

        _controller = new TestCategoryController(
            _categoryServiceMock.Object,
            _categoryViewModelServiceMock.Object,
            languageServiceMock.Object,
            _translationServiceMock.Object,
            new Mock<IPictureViewModelService>().Object,
            new Mock<IProductService>().Object,
            _scopeMock.Object);

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
    public async Task ListGet_PassesScopeDefaultStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _categoryViewModelServiceMock.Setup(v => v.PrepareCategoryListModel("store-1")).ReturnsAsync(new CategoryListModel());

        var result = await _controller.List();

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        _categoryViewModelServiceMock.Verify(v => v.PrepareCategoryListModel("store-1"), Times.Once);
    }

    [TestMethod]
    public async Task ListPost_ForcesScopeDefaultStoreIdOntoSearchModel()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _categoryViewModelServiceMock
            .Setup(v => v.PrepareCategoryListModel(It.IsAny<CategoryListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<CategoryModel>(), 0));

        var model = new CategoryListModel { SearchStoreId = "attacker-supplied-store" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    // --- Edit (GET) --------------------------------------------------------------------------------

    [TestMethod]
    public async Task EditGet_CategoryNotFound_RedirectsToList()
    {
        _categoryServiceMock.Setup(c => c.GetCategoryById("missing")).ReturnsAsync((Category)null);

        var result = await _controller.Edit("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _scopeMock.Verify(s => s.CanView(It.IsAny<Category>()), Times.Never);
    }

    [TestMethod]
    public async Task EditGet_ScopeDeniesView_RedirectsToList()
    {
        var category = new Category { Id = "c1" };
        _categoryServiceMock.Setup(c => c.GetCategoryById("c1")).ReturnsAsync(category);
        _scopeMock.Setup(s => s.CanView(category)).ReturnsAsync(false);

        var result = await _controller.Edit("c1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task EditGet_ScopeAllowsView_ReturnsViewWithModel()
    {
        var category = new Category { Id = "c1", Name = "Widgets" };
        _categoryServiceMock.Setup(c => c.GetCategoryById("c1")).ReturnsAsync(category);
        _scopeMock.Setup(s => s.CanView(category)).ReturnsAsync(true);
        var languageServiceMock = new Mock<ILanguageService>();
        _categoryViewModelServiceMock
            .Setup(v => v.PrepareCategoryModel(It.IsAny<CategoryModel>(), category, null))
            .ReturnsAsync((CategoryModel m, Category c, string s) => m);

        var result = await _controller.Edit("c1");

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreEqual("Widgets", ((CategoryModel)view.Model).Name);
    }

    // --- Edit (POST) -------------------------------------------------------------------------------

    [TestMethod]
    public async Task EditPost_ScopeDeniesAccess_RedirectsToEdit()
    {
        var category = new Category { Id = "c1" };
        _categoryServiceMock.Setup(c => c.GetCategoryById("c1")).ReturnsAsync(category);
        _scopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(false);

        var result = await _controller.Edit(new CategoryModel { Id = "c1" }, false);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        _categoryViewModelServiceMock.Verify(v => v.UpdateCategoryModel(It.IsAny<Category>(), It.IsAny<CategoryModel>()), Times.Never);
    }

    // --- Delete --------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Delete_ScopeDeniesAccess_RedirectsToEditWithoutDeleting()
    {
        var category = new Category { Id = "c1" };
        _categoryServiceMock.Setup(c => c.GetCategoryById("c1")).ReturnsAsync(category);
        _scopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(false);

        var result = await _controller.Delete("c1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("c1", redirect.RouteValues["id"]);
        _categoryViewModelServiceMock.Verify(v => v.DeleteCategory(It.IsAny<Category>()), Times.Never);
    }

    // --- Create (POST) ------------------------------------------------------------------------------

    [TestMethod]
    public async Task CreatePost_StoreScoped_ForcesModelStores()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var inserted = new Category { Id = "new-1" };
        _categoryViewModelServiceMock
            .Setup(v => v.InsertCategoryModel(It.IsAny<CategoryModel>()))
            .ReturnsAsync(inserted)
            .Callback<CategoryModel>(m => Assert.AreSequenceEqual(new[] { "store-1" }, m.Stores));

        await _controller.Create(new CategoryModel { Name = "N" }, false);

        _categoryViewModelServiceMock.Verify(v => v.InsertCategoryModel(It.IsAny<CategoryModel>()), Times.Once);
    }

    [TestMethod]
    public async Task CreatePost_GlobalScoped_LeavesModelStoresUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var inserted = new Category { Id = "new-1" };
        var submitted = new CategoryModel { Name = "N", Stores = ["explicit-store"] };
        _categoryViewModelServiceMock
            .Setup(v => v.InsertCategoryModel(It.IsAny<CategoryModel>()))
            .ReturnsAsync(inserted)
            .Callback<CategoryModel>(m => Assert.AreSequenceEqual(new[] { "explicit-store" }, m.Stores));

        await _controller.Create(submitted, false);

        _categoryViewModelServiceMock.Verify(v => v.InsertCategoryModel(It.IsAny<CategoryModel>()), Times.Once);
    }

    // --- PicturePopup --------------------------------------------------------------------------------

    [TestMethod]
    public async Task PicturePopupGet_ScopeDeniesAccess_ReturnsDeniedContent()
    {
        var category = new Category { Id = "c1", PictureId = "pic-1" };
        _categoryServiceMock.Setup(c => c.GetCategoryById("c1")).ReturnsAsync(category);
        _scopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(false);

        var result = await _controller.PicturePopup("c1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("This is not your category", content.Content);
    }

    [TestMethod]
    public async Task PicturePopupGet_CategoryHasNoPicture_ReturnsNotExistContent()
    {
        var category = new Category { Id = "c1", PictureId = null };
        _categoryServiceMock.Setup(c => c.GetCategoryById("c1")).ReturnsAsync(category);
        _scopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(true);

        var result = await _controller.PicturePopup("c1");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Picture not exist", content.Content);
    }

    [TestMethod]
    public async Task PicturePopupGet_CategoryNotFound_ReturnsNotExistContent()
    {
        _categoryServiceMock.Setup(c => c.GetCategoryById("missing")).ReturnsAsync((Category)null);

        var result = await _controller.PicturePopup("missing");

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("Category not exist", content.Content);
        _scopeMock.Verify(s => s.HasAccess(It.IsAny<Category>()), Times.Never);
    }

    [TestMethod]
    public async Task PicturePopupPost_ScopeDeniesAccess_ReturnsDeniedContent()
    {
        var category = new Category { Id = "c1", PictureId = "pic-1" };
        _categoryServiceMock.Setup(c => c.GetCategoryById("c1")).ReturnsAsync(category);
        _scopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(false);

        var model = new PictureModel { ObjectId = "c1", Id = "pic-1" };
        var result = await _controller.PicturePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("This is not your category", content.Content);
    }

    [TestMethod]
    public async Task PicturePopupPost_CategoryNotFound_ThrowsArgumentException()
    {
        _categoryServiceMock.Setup(c => c.GetCategoryById("missing")).ReturnsAsync((Category)null);

        var model = new PictureModel { ObjectId = "missing", Id = "pic-1" };

        var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _controller.PicturePopup(model));

        Assert.AreEqual("No category found with the specified id", exception.Message);
    }

    [TestMethod]
    public async Task PicturePopupPost_PictureIdMismatch_ThrowsArgumentException()
    {
        var category = new Category { Id = "c1", PictureId = "pic-1" };
        _categoryServiceMock.Setup(c => c.GetCategoryById("c1")).ReturnsAsync(category);
        _scopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(true);

        var model = new PictureModel { ObjectId = "c1", Id = "pic-2" };

        var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _controller.PicturePopup(model));

        Assert.AreEqual("Picture ident doesn't fit with category", exception.Message);
    }

    [TestMethod]
    public async Task PicturePopupPost_ValidRequest_CallsUpdatePicture()
    {
        var pictureViewModelServiceMock = new Mock<IPictureViewModelService>();
        var category = new Category { Id = "c1", PictureId = "pic-1" };
        _categoryServiceMock.Setup(c => c.GetCategoryById("c1")).ReturnsAsync(category);
        _scopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(true);
        pictureViewModelServiceMock.Setup(p => p.UpdatePicture(It.IsAny<PictureModel>())).Returns(Task.CompletedTask);

        var controller = new TestCategoryController(
            _categoryServiceMock.Object,
            _categoryViewModelServiceMock.Object,
            new Mock<ILanguageService>().Object,
            _translationServiceMock.Object,
            pictureViewModelServiceMock.Object,
            new Mock<IProductService>().Object,
            _scopeMock.Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);

        var model = new PictureModel { ObjectId = "c1", Id = "pic-1" };

        var result = await controller.PicturePopup(model);

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
        pictureViewModelServiceMock.Verify(p => p.UpdatePicture(model), Times.Once);
    }

    // --- Products tab ---------------------------------------------------------------------------------

    [TestMethod]
    public async Task ProductList_ScopeDeniesAccess_ReturnsKendoError()
    {
        var category = new Category { Id = "c1" };
        _categoryServiceMock.Setup(c => c.GetCategoryById("c1")).ReturnsAsync(category);
        _scopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(false);

        var result = await _controller.ProductList(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsNotNull(gridModel.Errors);
    }

    [TestMethod]
    public async Task ProductUpdate_ProductNotOwnedByScopeStore_ReturnsKendoError()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var product = new Product { Id = "p1", LimitedToStores = true, Stores = ["other-store"] };
        new Mock<IProductService>().Setup(p => p.GetProductById("p1")).ReturnsAsync(product);
        var productServiceMock = new Mock<IProductService>();
        productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(product);

        var controller = new TestCategoryController(
            _categoryServiceMock.Object, _categoryViewModelServiceMock.Object,
            new Mock<ILanguageService>().Object, _translationServiceMock.Object,
            new Mock<IPictureViewModelService>().Object, productServiceMock.Object, _scopeMock.Object);
        controller.ControllerContext = _controller.ControllerContext;
        controller.TempData = _controller.TempData;

        var result = await controller.ProductUpdate(new CategoryModel.CategoryProductModel { Id = "pc1", ProductId = "p1" });

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsNotNull(gridModel.Errors);
        _categoryViewModelServiceMock.Verify(v => v.UpdateProductCategoryModel(It.IsAny<CategoryModel.CategoryProductModel>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAddPopupInsert_GlobalScope_SkipsPerProductFiltering()
    {
        var category = new Category { Id = "c1" };
        _categoryServiceMock.Setup(c => c.GetCategoryById("c1")).ReturnsAsync(category);
        _scopeMock.Setup(s => s.HasAccess(category)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        var model = new CategoryModel.AddCategoryProductModel { CategoryId = "c1", SelectedProductIds = ["p1", "p2"] };

        await _controller.ProductAddPopup(model);

        _categoryViewModelServiceMock.Verify(v => v.InsertCategoryProductModel(
            It.Is<CategoryModel.AddCategoryProductModel>(m => m.SelectedProductIds.Length == 2)), Times.Once);
    }
}
