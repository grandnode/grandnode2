using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Queries.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Mediator;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Discounts;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseDiscountControllerTests
{
    // BaseDiscountController is abstract; minimal subclass so actions can be invoked directly.
    private class TestDiscountController(
        IDiscountViewModelService discountViewModelService,
        IDiscountService discountService,
        ITranslationService translationService,
        IDateTimeService dateTimeService,
        IMediator mediator,
        IDiscountProviderLoader discountProviderLoader,
        IAdminDataScope<Discount> scope)
        : BaseDiscountController(discountViewModelService, discountService, translationService, dateTimeService,
            mediator, discountProviderLoader, scope);

    private Mock<IDiscountViewModelService> _vmService = null!;
    private Mock<IDiscountService> _service = null!;
    private Mock<IAdminDataScope<Discount>> _scope = null!;
    private Mock<IMediator> _mediator = null!;
    private Mock<IDiscountProviderLoader> _loader = null!;
    private TestDiscountController _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<DiscountProfile>());
        AutoMapperConfig.Init(mapperConfig);

        _vmService = new Mock<IDiscountViewModelService>();
        _service = new Mock<IDiscountService>();
        _scope = new Mock<IAdminDataScope<Discount>>();
        _mediator = new Mock<IMediator>();
        _loader = new Mock<IDiscountProviderLoader>();

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _sut = new TestDiscountController(_vmService.Object, _service.Object,
            translationServiceMock.Object, Mock.Of<IDateTimeService>(), _mediator.Object, _loader.Object,
            _scope.Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _sut.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public async Task Edit_Get_ScopeDeniesView_RedirectsToList()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.CanView(discount)).ReturnsAsync(false);

        var result = await _sut.Edit("1") as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
    }

    [TestMethod]
    public async Task Edit_Get_ScopeAllowsView_ReturnsView()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.CanView(discount)).ReturnsAsync(true);

        var result = await _sut.Edit("1") as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result!.Model, typeof(DiscountModel));
        _vmService.Verify(x => x.PrepareDiscountModel(It.IsAny<DiscountModel>(), discount), Times.Once);
    }

    [TestMethod]
    public async Task Edit_Post_ScopeDeniesAccess_RedirectsToEditSelf()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        var model = new DiscountModel { Id = "1" };

        var result = await _sut.Edit(model, false) as RedirectToActionResult;

        Assert.AreEqual("Edit", result!.ActionName);
    }

    [TestMethod]
    public async Task Delete_ScopeDeniesAccess_RedirectsToEditSelf()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        _mediator.Setup(x => x.Send(It.IsAny<GetDiscountUsageHistoryQuery>(), default))
            .ReturnsAsync(new PagedList<DiscountUsageHistory>());

        var result = await _sut.Delete("1") as RedirectToActionResult;

        Assert.AreEqual("Edit", result!.ActionName);
    }

    [TestMethod]
    public async Task Delete_HasUsageHistory_BlocksDeletionEvenWithAccess()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(true);
        _mediator.Setup(x => x.Send(It.IsAny<GetDiscountUsageHistoryQuery>(), default))
            .ReturnsAsync(new PagedList<DiscountUsageHistory>(new List<DiscountUsageHistory> { new() }, 0, int.MaxValue));

        var result = await _sut.Delete("1") as RedirectToActionResult;

        Assert.AreEqual("Edit", result!.ActionName);
        _vmService.Verify(x => x.DeleteDiscount(It.IsAny<Discount>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_HasAccessAndNoUsageHistory_DeletesAndRedirectsToList()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(true);
        _mediator.Setup(x => x.Send(It.IsAny<GetDiscountUsageHistoryQuery>(), default))
            .ReturnsAsync(new PagedList<DiscountUsageHistory>());

        var result = await _sut.Delete("1") as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
        _vmService.Verify(x => x.DeleteDiscount(discount), Times.Once);
    }

    [TestMethod]
    public void Index_RedirectsToList()
    {
        var result = _sut.Index() as RedirectToActionResult;
        Assert.AreEqual("List", result!.ActionName);
    }

    [TestMethod]
    public async Task Create_Post_DefaultStoreIdSet_ForcesModelStores()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-A");
        var model = new DiscountModel { Name = "Test" };
        _vmService.Setup(x => x.InsertDiscountModel(model)).ReturnsAsync(new Discount { Id = "1" });

        await _sut.Create(model, false);

        CollectionAssert.AreEqual(new[] { "store-A" }, model.Stores);
    }

    [TestMethod]
    public async Task CouponCodeDelete_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);

        var result = await _sut.CouponCodeDelete("1", "coupon-1") as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task CouponCodeInsert_ScopeAllowsAccess_InsertsCoupon()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(true);
        _service.Setup(x => x.GetDiscountByCouponCode("SAVE10")).ReturnsAsync((Discount?)null);

        await _sut.CouponCodeInsert("1", "save10");

        _vmService.Verify(x => x.InsertCouponCode("1", "SAVE10"), Times.Once);
    }

    [TestMethod]
    public async Task GetDiscountRequirementConfigurationUrl_ScopeDeniesAccess_ReturnsGracefulJsonError()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        var provider = Mock.Of<IDiscountProvider>();
        _loader.Setup(x => x.LoadDiscountProviderByRuleSystemName("rule1")).Returns(provider);

        var result = await _sut.GetDiscountRequirementConfigurationUrl("rule1", "1", "req1") as JsonResult;

        Assert.IsFalse((bool)result!.Value!.GetType().GetProperty("Result")!.GetValue(result.Value)!);
    }

    [TestMethod]
    public async Task GetDiscountRequirementMetaInfo_ScopeDeniesAccess_ReturnsGracefulJsonError()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);

        var result = await _sut.GetDiscountRequirementMetaInfo("req1", "1") as JsonResult;

        Assert.IsFalse((bool)result!.Value!.GetType().GetProperty("Result")!.GetValue(result.Value)!);
    }

    [TestMethod]
    public async Task DeleteDiscountRequirement_ScopeDeniesAccess_ReturnsGracefulJsonError()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);

        var result = await _sut.DeleteDiscountRequirement("req1", "1") as JsonResult;

        Assert.IsFalse((bool)result!.Value!.GetType().GetProperty("Result")!.GetValue(result.Value)!);
    }

    [TestMethod]
    public async Task ProductList_ScopeDeniesView_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.CanView(discount)).ReturnsAsync(false);
        var productService = new Mock<IProductService>();

        var result = await _sut.ProductList(new DataSourceRequest(), "1", productService.Object) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task ProductList_ScopeAllowsView_ReturnsProducts()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.CanView(discount)).ReturnsAsync(true);
        var productService = new Mock<IProductService>();
        var products = new PagedList<Product>(new List<Product> { new() { Id = "p1", Name = "Product 1" } }, 0, int.MaxValue);
        productService.Setup(x => x.GetProductsByDiscount("1", 0, 10)).ReturnsAsync(products);

        var result = await _sut.ProductList(new DataSourceRequest { Page = 1, PageSize = 10 }, "1", productService.Object) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual(1, data.Total);
    }

    [TestMethod]
    public async Task ProductDelete_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        var productService = new Mock<IProductService>();

        var result = await _sut.ProductDelete("1", "p1", productService.Object) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task ProductDelete_ScopeAllowsAccess_DeletesProduct()
    {
        var discount = new Discount { Id = "1" };
        var product = new Product { Id = "p1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(true);
        var productService = new Mock<IProductService>();
        productService.Setup(x => x.GetProductById("p1", false)).ReturnsAsync(product);

        await _sut.ProductDelete("1", "p1", productService.Object);

        _vmService.Verify(x => x.DeleteProduct(discount, product), Times.Once);
    }

    [TestMethod]
    public async Task ProductAddPopup_Get_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);

        var result = await _sut.ProductAddPopup("1") as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task ProductAddPopup_Get_ScopeAllowsAccess_ReturnsView()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(true);
        var model = new DiscountModel.AddProductToDiscountModel();
        _vmService.Setup(x => x.PrepareProductToDiscountModel()).ReturnsAsync(model);

        var result = await _sut.ProductAddPopup("1") as ViewResult;

        Assert.AreSame(model, result!.Model);
    }

    [TestMethod]
    public async Task ProductAddPopupList_GlobalScope_LeavesSearchStoreIdUntouched()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns((string?)null);
        var model = new DiscountModel.AddProductToDiscountModel { SearchStoreId = "client-supplied" };
        _vmService.Setup(x => x.PrepareProductModel(model, 1, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        await _sut.ProductAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("client-supplied", model.SearchStoreId);
    }

    [TestMethod]
    public async Task ProductAddPopupList_StoreScope_ForcesSearchStoreId()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-a");
        var model = new DiscountModel.AddProductToDiscountModel { SearchStoreId = "client-supplied" };
        _vmService.Setup(x => x.PrepareProductModel(model, 1, 10))
            .ReturnsAsync((new List<ProductModel>(), 0));

        await _sut.ProductAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-a", model.SearchStoreId);
    }

    [TestMethod]
    public async Task ProductAddPopup_Post_ScopeDeniesAccess_ReturnsAccessDeniedContent()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        var model = new DiscountModel.AddProductToDiscountModel { DiscountId = "1" };

        var result = await _sut.ProductAddPopup(model) as ContentResult;

        Assert.AreEqual("Access denied", result!.Content);
    }

    [TestMethod]
    public async Task ProductAddPopup_Post_ScopeAllowsAccess_InsertsProduct()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(true);
        var model = new DiscountModel.AddProductToDiscountModel { DiscountId = "1", SelectedProductIds = ["p1"] };

        await _sut.ProductAddPopup(model);

        _vmService.Verify(x => x.InsertProductToDiscountModel(model), Times.Once);
    }

    [TestMethod]
    public async Task CategoryList_ScopeDeniesView_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.CanView(discount)).ReturnsAsync(false);
        var categoryService = new Mock<ICategoryService>();

        var result = await _sut.CategoryList(new DataSourceRequest(), "1", categoryService.Object) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task CategoryDelete_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        var categoryService = new Mock<ICategoryService>();

        var result = await _sut.CategoryDelete("1", "c1", categoryService.Object) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task CategoryAddPopup_Get_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);

        var result = await _sut.CategoryAddPopup("1") as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task CategoryAddPopup_Post_ScopeDeniesAccess_ReturnsAccessDeniedContent()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        var model = new DiscountModel.AddCategoryToDiscountModel { DiscountId = "1" };

        var result = await _sut.CategoryAddPopup(model) as ContentResult;

        Assert.AreEqual("Access denied", result!.Content);
    }

    [TestMethod]
    public async Task CategoryAddPopupList_GlobalScope_PassesEmptyStoreId()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns((string?)null);
        var categoryService = new Mock<ICategoryService>();
        categoryService.Setup(x => x.GetAllCategories(null, null, "", 0, 10, true))
            .ReturnsAsync(new PagedList<Category>(new List<Category>(), 0, 10, 0));

        await _sut.CategoryAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 },
            new DiscountModel.AddCategoryToDiscountModel(), categoryService.Object);

        categoryService.Verify(x => x.GetAllCategories(null, null, "", 0, 10, true), Times.Once);
    }

    [TestMethod]
    public async Task CategoryAddPopupList_StoreScope_PassesScopedStoreId()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-a");
        var categoryService = new Mock<ICategoryService>();
        categoryService.Setup(x => x.GetAllCategories(null, null, "store-a", 0, 10, true))
            .ReturnsAsync(new PagedList<Category>(new List<Category>(), 0, 10, 0));

        await _sut.CategoryAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 },
            new DiscountModel.AddCategoryToDiscountModel(), categoryService.Object);

        categoryService.Verify(x => x.GetAllCategories(null, null, "store-a", 0, 10, true), Times.Once);
    }

    [TestMethod]
    public async Task BrandList_ScopeDeniesView_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.CanView(discount)).ReturnsAsync(false);
        var brandService = new Mock<IBrandService>();

        var result = await _sut.BrandList(new DataSourceRequest(), "1", brandService.Object) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task BrandDelete_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        var brandService = new Mock<IBrandService>();

        var result = await _sut.BrandDelete("1", "b1", brandService.Object) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task BrandAddPopup_Get_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);

        var result = await _sut.BrandAddPopup("1") as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task BrandAddPopup_Post_ScopeDeniesAccess_ReturnsAccessDeniedContent()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        var model = new DiscountModel.AddBrandToDiscountModel { DiscountId = "1" };

        var result = await _sut.BrandAddPopup(model) as ContentResult;

        Assert.AreEqual("Access denied", result!.Content);
    }

    [TestMethod]
    public async Task BrandAddPopupList_GlobalScope_PassesEmptyStoreId()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns((string?)null);
        var brandService = new Mock<IBrandService>();
        brandService.Setup(x => x.GetAllBrands(null, "", 0, 10, true))
            .ReturnsAsync(new PagedList<Brand>(new List<Brand>(), 0, 10, 0));

        await _sut.BrandAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 },
            new DiscountModel.AddBrandToDiscountModel(), brandService.Object);

        brandService.Verify(x => x.GetAllBrands(null, "", 0, 10, true), Times.Once);
    }

    [TestMethod]
    public async Task BrandAddPopupList_StoreScope_PassesScopedStoreId()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-a");
        var brandService = new Mock<IBrandService>();
        brandService.Setup(x => x.GetAllBrands(null, "store-a", 0, 10, true))
            .ReturnsAsync(new PagedList<Brand>(new List<Brand>(), 0, 10, 0));

        await _sut.BrandAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 },
            new DiscountModel.AddBrandToDiscountModel(), brandService.Object);

        brandService.Verify(x => x.GetAllBrands(null, "store-a", 0, 10, true), Times.Once);
    }

    [TestMethod]
    public async Task CollectionList_ScopeDeniesView_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.CanView(discount)).ReturnsAsync(false);
        var collectionService = new Mock<ICollectionService>();

        var result = await _sut.CollectionList(new DataSourceRequest(), "1", collectionService.Object) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task CollectionDelete_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        var collectionService = new Mock<ICollectionService>();

        var result = await _sut.CollectionDelete("1", "c1", collectionService.Object) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task CollectionAddPopup_Get_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);

        var result = await _sut.CollectionAddPopup("1") as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task CollectionAddPopup_Post_ScopeDeniesAccess_ReturnsAccessDeniedContent()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);
        var model = new DiscountModel.AddCollectionToDiscountModel { DiscountId = "1" };

        var result = await _sut.CollectionAddPopup(model) as ContentResult;

        Assert.AreEqual("Access denied", result!.Content);
    }

    [TestMethod]
    public async Task CollectionAddPopupList_GlobalScope_PassesEmptyStoreId()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns((string?)null);
        var collectionService = new Mock<ICollectionService>();
        collectionService.Setup(x => x.GetAllCollections(null, "", 0, 10, true))
            .ReturnsAsync(new PagedList<Collection>(new List<Collection>(), 0, 10, 0));

        await _sut.CollectionAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 },
            new DiscountModel.AddCollectionToDiscountModel(), collectionService.Object);

        collectionService.Verify(x => x.GetAllCollections(null, "", 0, 10, true), Times.Once);
    }

    [TestMethod]
    public async Task CollectionAddPopupList_StoreScope_PassesScopedStoreId()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("store-a");
        var collectionService = new Mock<ICollectionService>();
        collectionService.Setup(x => x.GetAllCollections(null, "store-a", 0, 10, true))
            .ReturnsAsync(new PagedList<Collection>(new List<Collection>(), 0, 10, 0));

        await _sut.CollectionAddPopupList(new DataSourceRequest { Page = 1, PageSize = 10 },
            new DiscountModel.AddCollectionToDiscountModel(), collectionService.Object);

        collectionService.Verify(x => x.GetAllCollections(null, "store-a", 0, 10, true), Times.Once);
    }

    [TestMethod]
    public async Task UsageHistoryList_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);

        var result = await _sut.UsageHistoryList("1", new DataSourceRequest()) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task UsageHistoryList_ScopeAllowsAccess_ReturnsUsageHistory()
    {
        var discount = new Discount { Id = "1" };
        var usageHistoryModel = new DiscountModel.DiscountUsageHistoryModel { Id = "h1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(true);
        _vmService.Setup(x => x.PrepareDiscountUsageHistoryModel(discount, 1, 10))
            .ReturnsAsync((new[] { usageHistoryModel }, 1));

        var result = await _sut.UsageHistoryList("1", new DataSourceRequest { Page = 1, PageSize = 10 }) as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual(1, data.Total);
        Assert.AreEqual(1, ((List<DiscountModel.DiscountUsageHistoryModel>)data.Data).Count);
    }

    [TestMethod]
    public async Task UsageHistoryDelete_ScopeDeniesAccess_ReturnsAccessDeniedJson()
    {
        var discount = new Discount { Id = "1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(false);

        var result = await _sut.UsageHistoryDelete("1", "h1") as JsonResult;

        var data = (DataSourceResult)result!.Value!;
        Assert.AreEqual("Access denied", data.Errors);
    }

    [TestMethod]
    public async Task UsageHistoryDelete_ScopeAllowsAccess_DeletesHistory()
    {
        var discount = new Discount { Id = "1" };
        var history = new DiscountUsageHistory { Id = "h1" };
        _service.Setup(x => x.GetDiscountById("1")).ReturnsAsync(discount);
        _scope.Setup(x => x.HasAccess(discount)).ReturnsAsync(true);
        _service.Setup(x => x.GetDiscountUsageHistoryById("h1")).ReturnsAsync(history);

        await _sut.UsageHistoryDelete("1", "h1");

        _service.Verify(x => x.DeleteDiscountUsageHistory(history), Times.Once);
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on Discount applied-to-collections region
/// methods. Ensures CollectionList, CollectionDelete, both CollectionAddPopup overloads, and
/// CollectionAddPopupList carry the required [PermissionAuthorizeAction] attributes.
/// </summary>
[TestClass]
public class BaseDiscountControllerCollectionsAttributeTests
{
    [TestMethod]
    public void CollectionList_HasPermissionAuthorizeActionPreview()
    {
        var method = typeof(BaseDiscountController).GetMethod("CollectionList");
        Assert.IsNotNull(method, "CollectionList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CollectionList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Preview, attr!.PermissionAction, "CollectionList should require Preview permission");
    }

    [TestMethod]
    public void CollectionDelete_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("CollectionDelete");
        Assert.IsNotNull(method, "CollectionDelete method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CollectionDelete missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "CollectionDelete should require Edit permission");
    }

    [TestMethod]
    public void CollectionAddPopup_Get_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("CollectionAddPopup", [typeof(string)]);
        Assert.IsNotNull(method, "CollectionAddPopup(string) method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CollectionAddPopup(string) missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "CollectionAddPopup(string) should require Edit permission");
    }

    [TestMethod]
    public void CollectionAddPopupList_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("CollectionAddPopupList");
        Assert.IsNotNull(method, "CollectionAddPopupList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CollectionAddPopupList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "CollectionAddPopupList should require Edit permission");
    }

    [TestMethod]
    public void CollectionAddPopup_Post_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("CollectionAddPopup", [typeof(DiscountModel.AddCollectionToDiscountModel)]);
        Assert.IsNotNull(method, "CollectionAddPopup(AddCollectionToDiscountModel) method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CollectionAddPopup(AddCollectionToDiscountModel) missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "CollectionAddPopup(AddCollectionToDiscountModel) should require Edit permission");
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on Discount applied-to-products region methods.
/// Ensures that ProductList, ProductDelete, and both ProductAddPopup overloads carry the required
/// [PermissionAuthorizeAction] attributes.
/// </summary>
[TestClass]
public class BaseDiscountControllerProductsAttributeTests
{
    [TestMethod]
    public void ProductList_HasPermissionAuthorizeActionPreview()
    {
        var method = typeof(BaseDiscountController).GetMethod("ProductList");
        Assert.IsNotNull(method, "ProductList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "ProductList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Preview, attr!.PermissionAction, "ProductList should require Preview permission");
    }

    [TestMethod]
    public void ProductDelete_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("ProductDelete");
        Assert.IsNotNull(method, "ProductDelete method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "ProductDelete missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "ProductDelete should require Edit permission");
    }

    [TestMethod]
    public void ProductAddPopup_Get_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("ProductAddPopup", [typeof(string)]);
        Assert.IsNotNull(method, "ProductAddPopup(string) method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "ProductAddPopup(string) missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "ProductAddPopup(string) should require Edit permission");
    }

    [TestMethod]
    public void ProductAddPopupList_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("ProductAddPopupList");
        Assert.IsNotNull(method, "ProductAddPopupList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "ProductAddPopupList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "ProductAddPopupList should require Edit permission");
    }

    [TestMethod]
    public void ProductAddPopup_Post_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("ProductAddPopup", [typeof(DiscountModel.AddProductToDiscountModel)]);
        Assert.IsNotNull(method, "ProductAddPopup(AddProductToDiscountModel) method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "ProductAddPopup(AddProductToDiscountModel) missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "ProductAddPopup(AddProductToDiscountModel) should require Edit permission");
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on Discount requirements region methods.
/// Ensures that GetDiscountRequirementConfigurationUrl, GetDiscountRequirementMetaInfo, and
/// DeleteDiscountRequirement carry the required [PermissionAuthorizeAction] attributes.
/// </summary>
[TestClass]
public class BaseDiscountControllerRequirementsAttributeTests
{
    [TestMethod]
    public void GetDiscountRequirementConfigurationUrl_HasPermissionAuthorizeActionPreview()
    {
        var method = typeof(BaseDiscountController).GetMethod("GetDiscountRequirementConfigurationUrl");
        Assert.IsNotNull(method, "GetDiscountRequirementConfigurationUrl method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "GetDiscountRequirementConfigurationUrl missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Preview, attr!.PermissionAction,
            "GetDiscountRequirementConfigurationUrl should require Preview permission");
    }

    [TestMethod]
    public void GetDiscountRequirementMetaInfo_HasPermissionAuthorizeActionPreview()
    {
        var method = typeof(BaseDiscountController).GetMethod("GetDiscountRequirementMetaInfo");
        Assert.IsNotNull(method, "GetDiscountRequirementMetaInfo method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "GetDiscountRequirementMetaInfo missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Preview, attr!.PermissionAction,
            "GetDiscountRequirementMetaInfo should require Preview permission");
    }

    [TestMethod]
    public void DeleteDiscountRequirement_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("DeleteDiscountRequirement");
        Assert.IsNotNull(method, "DeleteDiscountRequirement method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "DeleteDiscountRequirement missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction,
            "DeleteDiscountRequirement should require Edit permission");
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on Discount coupon-code region methods.
/// Ensures that CouponCodeList, CouponCodeDelete, and CouponCodeInsert carry the required
/// [PermissionAuthorizeAction] attributes to prevent authorization bypass (users without Edit
/// permission should not be able to modify or delete coupon codes).
/// </summary>
[TestClass]
public class BaseDiscountControllerCouponCodeAttributeTests
{
    [TestMethod]
    public void CouponCodeList_HasPermissionAuthorizeActionPreview()
    {
        var method = typeof(BaseDiscountController).GetMethod("CouponCodeList");
        Assert.IsNotNull(method, "CouponCodeList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CouponCodeList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Preview, attr!.PermissionAction, "CouponCodeList should require Preview permission");
    }

    [TestMethod]
    public void CouponCodeDelete_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("CouponCodeDelete");
        Assert.IsNotNull(method, "CouponCodeDelete method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CouponCodeDelete missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "CouponCodeDelete should require Edit permission");
    }

    [TestMethod]
    public void CouponCodeInsert_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("CouponCodeInsert");
        Assert.IsNotNull(method, "CouponCodeInsert method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CouponCodeInsert missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "CouponCodeInsert should require Edit permission");
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on Discount usage-history region methods.
/// Ensures that UsageHistoryList and UsageHistoryDelete carry the required [PermissionAuthorizeAction]
/// attributes to prevent authorization bypass.
/// </summary>
[TestClass]
public class BaseDiscountControllerUsageHistoryAttributeTests
{
    [TestMethod]
    public void UsageHistoryList_HasPermissionAuthorizeActionPreview()
    {
        var method = typeof(BaseDiscountController).GetMethod("UsageHistoryList");
        Assert.IsNotNull(method, "UsageHistoryList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "UsageHistoryList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Preview, attr!.PermissionAction, "UsageHistoryList should require Preview permission");
    }

    [TestMethod]
    public void UsageHistoryDelete_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("UsageHistoryDelete");
        Assert.IsNotNull(method, "UsageHistoryDelete method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "UsageHistoryDelete missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "UsageHistoryDelete should require Edit permission");
    }
}

/// <summary>
/// Regression test for ARCH-001 authorization attributes on Discount applied-to-categories and
/// applied-to-brands region methods. Ensures CategoryList, CategoryDelete, both CategoryAddPopup
/// overloads, CategoryAddPopupList, BrandList, BrandDelete, both BrandAddPopup overloads, and
/// BrandAddPopupList carry the required [PermissionAuthorizeAction] attributes.
/// </summary>
[TestClass]
public class BaseDiscountControllerCategoriesAndBrandsAttributeTests
{
    [TestMethod]
    public void CategoryList_HasPermissionAuthorizeActionPreview()
    {
        var method = typeof(BaseDiscountController).GetMethod("CategoryList");
        Assert.IsNotNull(method, "CategoryList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CategoryList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Preview, attr!.PermissionAction, "CategoryList should require Preview permission");
    }

    [TestMethod]
    public void CategoryDelete_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("CategoryDelete");
        Assert.IsNotNull(method, "CategoryDelete method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CategoryDelete missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "CategoryDelete should require Edit permission");
    }

    [TestMethod]
    public void CategoryAddPopup_Get_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("CategoryAddPopup", [typeof(string)]);
        Assert.IsNotNull(method, "CategoryAddPopup(string) method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CategoryAddPopup(string) missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "CategoryAddPopup(string) should require Edit permission");
    }

    [TestMethod]
    public void CategoryAddPopupList_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("CategoryAddPopupList");
        Assert.IsNotNull(method, "CategoryAddPopupList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CategoryAddPopupList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "CategoryAddPopupList should require Edit permission");
    }

    [TestMethod]
    public void CategoryAddPopup_Post_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("CategoryAddPopup", [typeof(DiscountModel.AddCategoryToDiscountModel)]);
        Assert.IsNotNull(method, "CategoryAddPopup(AddCategoryToDiscountModel) method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "CategoryAddPopup(AddCategoryToDiscountModel) missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "CategoryAddPopup(AddCategoryToDiscountModel) should require Edit permission");
    }

    [TestMethod]
    public void BrandList_HasPermissionAuthorizeActionPreview()
    {
        var method = typeof(BaseDiscountController).GetMethod("BrandList");
        Assert.IsNotNull(method, "BrandList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "BrandList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Preview, attr!.PermissionAction, "BrandList should require Preview permission");
    }

    [TestMethod]
    public void BrandDelete_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("BrandDelete");
        Assert.IsNotNull(method, "BrandDelete method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "BrandDelete missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "BrandDelete should require Edit permission");
    }

    [TestMethod]
    public void BrandAddPopup_Get_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("BrandAddPopup", [typeof(string)]);
        Assert.IsNotNull(method, "BrandAddPopup(string) method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "BrandAddPopup(string) missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "BrandAddPopup(string) should require Edit permission");
    }

    [TestMethod]
    public void BrandAddPopupList_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("BrandAddPopupList");
        Assert.IsNotNull(method, "BrandAddPopupList method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "BrandAddPopupList missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "BrandAddPopupList should require Edit permission");
    }

    [TestMethod]
    public void BrandAddPopup_Post_HasPermissionAuthorizeActionEdit()
    {
        var method = typeof(BaseDiscountController).GetMethod("BrandAddPopup", [typeof(DiscountModel.AddBrandToDiscountModel)]);
        Assert.IsNotNull(method, "BrandAddPopup(AddBrandToDiscountModel) method not found");
        var attr = method!.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), false)
            .Cast<PermissionAuthorizeActionAttribute>()
            .SingleOrDefault();
        Assert.IsNotNull(attr, "BrandAddPopup(AddBrandToDiscountModel) missing [PermissionAuthorizeAction]");
        Assert.AreEqual(PermissionActionName.Edit, attr!.PermissionAction, "BrandAddPopup(AddBrandToDiscountModel) should require Edit permission");
    }
}
