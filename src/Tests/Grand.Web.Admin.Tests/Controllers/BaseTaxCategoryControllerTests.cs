using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Tax;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseTaxCategoryControllerTests
{
    // Concrete subclass exists only to instantiate the abstract base for direct-call unit tests -
    // same pattern as BaseEmailAccountControllerTests/BaseCategoryControllerTests.
    private class TestTaxCategoryController(
        ITaxCategoryService taxCategoryService,
        IStoreService storeService,
        ITranslationService translationService,
        IAdminDataScope<TaxCategory> scope)
        : BaseTaxCategoryController(taxCategoryService, storeService, translationService, scope);

    private Mock<ITaxCategoryService> _taxCategoryServiceMock = null!;
    private Mock<IStoreService> _storeServiceMock = null!;
    private Mock<ITranslationService> _translationServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<TaxCategoryProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _taxCategoryServiceMock = new Mock<ITaxCategoryService>();
        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string s) => s);
    }

    private TestTaxCategoryController CreateController(IAdminDataScope<TaxCategory> scope)
    {
        var controller = new TestTaxCategoryController(
            _taxCategoryServiceMock.Object,
            _storeServiceMock.Object,
            _translationServiceMock.Object,
            scope);

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
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    private static Mock<IAdminDataScope<TaxCategory>> AdminScope() => AdminScope(true);

    private static Mock<IAdminDataScope<TaxCategory>> AdminScope(bool hasAccess)
    {
        var scope = new Mock<IAdminDataScope<TaxCategory>>();
        scope.Setup(s => s.DefaultStoreId).Returns((string?)null);
        scope.Setup(s => s.HasAccess(It.IsAny<TaxCategory>())).ReturnsAsync(hasAccess);
        scope.Setup(s => s.ShowStoreSelector).Returns(true);
        return scope;
    }

    private static Mock<IAdminDataScope<TaxCategory>> StoreScope(string storeId, bool hasAccess)
    {
        var scope = new Mock<IAdminDataScope<TaxCategory>>();
        scope.Setup(s => s.DefaultStoreId).Returns(storeId);
        scope.Setup(s => s.HasAccess(It.IsAny<TaxCategory>())).ReturnsAsync(hasAccess);
        scope.Setup(s => s.ShowStoreSelector).Returns(false);
        return scope;
    }

    // --- Categories (grid data) ---

    [TestMethod]
    public async Task Categories_AdminScope_RequestsAllStores()
    {
        _taxCategoryServiceMock.Setup(s => s.GetAllTaxCategories(""))
            .ReturnsAsync(new List<TaxCategory> { new() { Id = "tc-1", Name = "Books", StoreId = "" } });
        var controller = CreateController(AdminScope().Object);

        var result = await controller.Categories(new DataSourceRequest()) as JsonResult;

        var payload = (DataSourceResult)result!.Value!;
        Assert.AreEqual(1, payload.Total);
        _taxCategoryServiceMock.Verify(s => s.GetAllTaxCategories(""), Times.Once);
    }

    [TestMethod]
    public async Task Categories_StoreScope_RequestsOwnStore()
    {
        _taxCategoryServiceMock.Setup(s => s.GetAllTaxCategories("store-1"))
            .ReturnsAsync(new List<TaxCategory>());
        var controller = CreateController(StoreScope("store-1", true).Object);

        await controller.Categories(new DataSourceRequest());

        _taxCategoryServiceMock.Verify(s => s.GetAllTaxCategories("store-1"), Times.Once);
        _storeServiceMock.Verify(s => s.GetAllStores(), Times.Never); // ShowStoreSelector=false: no store lookup needed
    }

    [TestMethod]
    public async Task Categories_AdminScope_ResolvesStoreNameForOwnedCategory()
    {
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Store> { new() { Id = "store-1", Shortcut = "MyStore" } });
        _taxCategoryServiceMock.Setup(s => s.GetAllTaxCategories(""))
            .ReturnsAsync(new List<TaxCategory> { new() { Id = "tc-1", Name = "Books", StoreId = "store-1" } });
        var controller = CreateController(AdminScope().Object);

        var result = await controller.Categories(new DataSourceRequest()) as JsonResult;

        var payload = (DataSourceResult)result!.Value!;
        var items = (List<TaxCategoryModel>)payload.Data;
        Assert.AreEqual("MyStore", items.Single().StoreName);
    }

    [TestMethod]
    public async Task Categories_AdminScope_ResolvesStoreNameAsAllForGlobalCategory()
    {
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Store> { new() { Id = "store-1", Shortcut = "MyStore" } });
        _taxCategoryServiceMock.Setup(s => s.GetAllTaxCategories(""))
            .ReturnsAsync(new List<TaxCategory> { new() { Id = "tc-1", Name = "Books", StoreId = "" } });
        var controller = CreateController(AdminScope().Object);

        var result = await controller.Categories(new DataSourceRequest()) as JsonResult;

        var payload = (DataSourceResult)result!.Value!;
        var items = (List<TaxCategoryModel>)payload.Data;
        Assert.AreEqual("Admin.Common.All", items.Single().StoreName);
    }

    [TestMethod]
    public async Task Categories_StoreScope_LeavesStoreNameEmpty()
    {
        _taxCategoryServiceMock.Setup(s => s.GetAllTaxCategories("store-1"))
            .ReturnsAsync(new List<TaxCategory> { new() { Id = "tc-1", Name = "Books", StoreId = "store-1" } });
        var controller = CreateController(StoreScope("store-1", true).Object);

        var result = await controller.Categories(new DataSourceRequest()) as JsonResult;

        var payload = (DataSourceResult)result!.Value!;
        var items = (List<TaxCategoryModel>)payload.Data;
        Assert.AreEqual(string.Empty, items.Single().StoreName);
    }

    // --- CategoryAdd ---

    [TestMethod]
    public async Task CategoryAdd_AdminScope_DoesNotForceStoreId()
    {
        TaxCategory? inserted = null;
        _taxCategoryServiceMock.Setup(s => s.InsertTaxCategory(It.IsAny<TaxCategory>()))
            .Callback<TaxCategory>(tc => inserted = tc)
            .Returns(Task.CompletedTask);
        var controller = CreateController(AdminScope().Object);

        await controller.CategoryAdd(new TaxCategoryModel { Name = "Books", StoreId = "store-9" });

        Assert.AreEqual("store-9", inserted!.StoreId); // whatever the admin's grid dropdown picked
    }

    [TestMethod]
    public async Task CategoryAdd_StoreScope_ForcesStoreId()
    {
        TaxCategory? inserted = null;
        _taxCategoryServiceMock.Setup(s => s.InsertTaxCategory(It.IsAny<TaxCategory>()))
            .Callback<TaxCategory>(tc => inserted = tc)
            .Returns(Task.CompletedTask);
        var controller = CreateController(StoreScope("store-1", true).Object);

        await controller.CategoryAdd(new TaxCategoryModel { Name = "Books" });

        Assert.AreEqual("store-1", inserted!.StoreId);
    }

    // --- CategoryUpdate ---

    [TestMethod]
    public async Task CategoryUpdate_AdminScope_UpdatesRegardlessOfOwnership()
    {
        var existing = new TaxCategory { Id = "tc-1", Name = "Old", StoreId = "" };
        _taxCategoryServiceMock.Setup(s => s.GetTaxCategoryById("tc-1")).ReturnsAsync(existing);
        var controller = CreateController(AdminScope().Object);

        var result = await controller.CategoryUpdate(new TaxCategoryModel { Id = "tc-1", Name = "New" });

        _taxCategoryServiceMock.Verify(s => s.UpdateTaxCategory(It.IsAny<TaxCategory>()), Times.Once);
        Assert.IsInstanceOfType(result, typeof(JsonResult));
    }

    [TestMethod]
    public async Task CategoryUpdate_StoreScope_DeniedOwnership_DoesNotUpdate()
    {
        var existing = new TaxCategory { Id = "tc-1", Name = "Old", StoreId = "store-2" };
        _taxCategoryServiceMock.Setup(s => s.GetTaxCategoryById("tc-1")).ReturnsAsync(existing);
        var controller = CreateController(StoreScope("store-1", false).Object);

        await controller.CategoryUpdate(new TaxCategoryModel { Id = "tc-1", Name = "New" });

        _taxCategoryServiceMock.Verify(s => s.UpdateTaxCategory(It.IsAny<TaxCategory>()), Times.Never);
    }

    [TestMethod]
    public async Task CategoryUpdate_NotFound_DoesNotUpdate()
    {
        _taxCategoryServiceMock.Setup(s => s.GetTaxCategoryById("missing")).ReturnsAsync((TaxCategory)null!);
        var controller = CreateController(AdminScope().Object);

        await controller.CategoryUpdate(new TaxCategoryModel { Id = "missing", Name = "New" });

        _taxCategoryServiceMock.Verify(s => s.UpdateTaxCategory(It.IsAny<TaxCategory>()), Times.Never);
    }

    [TestMethod]
    public async Task CategoryUpdate_StoreScope_ForcesStoreIdOnMappedEntity()
    {
        var existing = new TaxCategory { Id = "tc-1", Name = "Old", StoreId = "store-1" };
        _taxCategoryServiceMock.Setup(s => s.GetTaxCategoryById("tc-1")).ReturnsAsync(existing);
        TaxCategory? updated = null;
        _taxCategoryServiceMock.Setup(s => s.UpdateTaxCategory(It.IsAny<TaxCategory>()))
            .Callback<TaxCategory>(tc => updated = tc)
            .Returns(Task.CompletedTask);
        var controller = CreateController(StoreScope("store-1", true).Object);

        await controller.CategoryUpdate(new TaxCategoryModel { Id = "tc-1", Name = "New" });

        Assert.AreEqual("store-1", updated!.StoreId);
    }

    // --- CategoryDelete ---

    [TestMethod]
    public async Task CategoryDelete_AdminScope_Deletes()
    {
        var existing = new TaxCategory { Id = "tc-1", StoreId = "" };
        _taxCategoryServiceMock.Setup(s => s.GetTaxCategoryById("tc-1")).ReturnsAsync(existing);
        var controller = CreateController(AdminScope().Object);

        await controller.CategoryDelete("tc-1");

        _taxCategoryServiceMock.Verify(s => s.DeleteTaxCategory(existing), Times.Once);
    }

    [TestMethod]
    public async Task CategoryDelete_StoreScope_DeniedOwnership_DoesNotDelete()
    {
        var existing = new TaxCategory { Id = "tc-1", StoreId = "store-2" };
        _taxCategoryServiceMock.Setup(s => s.GetTaxCategoryById("tc-1")).ReturnsAsync(existing);
        var controller = CreateController(StoreScope("store-1", false).Object);

        await controller.CategoryDelete("tc-1");

        _taxCategoryServiceMock.Verify(s => s.DeleteTaxCategory(It.IsAny<TaxCategory>()), Times.Never);
    }

    [TestMethod]
    public async Task CategoryDelete_NotFound_ReturnsEmptyJsonWithoutThrowing()
    {
        // Deliberate unification onto Store's original silent-empty-JSON behavior for not-found
        // (Admin originally threw ArgumentException here) — see design spec.
        _taxCategoryServiceMock.Setup(s => s.GetTaxCategoryById("missing")).ReturnsAsync((TaxCategory)null!);
        var controller = CreateController(AdminScope().Object);

        var result = await controller.CategoryDelete("missing");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        _taxCategoryServiceMock.Verify(s => s.DeleteTaxCategory(It.IsAny<TaxCategory>()), Times.Never);
    }
}
