using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseProductReviewControllerTests
{
    private Mock<IProductReviewViewModelService> _viewModelService;
    private Mock<IProductReviewService> _productReviewService;
    private Mock<ITranslationService> _translationService;
    private Mock<IAdminDataScope<ProductReview>> _scope;

    private class TestableProductReviewController(
        IProductReviewViewModelService viewModelService,
        IProductReviewService productReviewService,
        ITranslationService translationService,
        IAdminDataScope<ProductReview> scope)
        : BaseProductReviewController(viewModelService, productReviewService, translationService, scope);

    private TestableProductReviewController CreateController()
    {
        var controller = new TestableProductReviewController(_viewModelService.Object, _productReviewService.Object,
            _translationService.Object, _scope.Object);

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

        return controller;
    }

    [TestInitialize]
    public void Setup()
    {
        _viewModelService = new Mock<IProductReviewViewModelService>();
        _productReviewService = new Mock<IProductReviewService>();
        _translationService = new Mock<ITranslationService>();
        _scope = new Mock<IAdminDataScope<ProductReview>>();
    }

    [TestMethod]
    public async Task ListGet_Admin_CallsPrepareProductReviewListModel_ReturnsPopulatedModel()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns((string)null);
        var listModel = new ProductReviewListModel { AvailableStores = { new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "", Text = "All" } } };
        _viewModelService.Setup(s => s.PrepareProductReviewListModel("")).ReturnsAsync(listModel);

        var result = await CreateController().List() as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreSame(listModel, result.Model);
        _viewModelService.Verify(s => s.PrepareProductReviewListModel(""), Times.Once);
    }

    [TestMethod]
    public async Task ListGet_Store_CallsPrepareProductReviewListModelWithStoreId()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");
        var listModel = new ProductReviewListModel();
        _viewModelService.Setup(s => s.PrepareProductReviewListModel("store-1")).ReturnsAsync(listModel);

        var result = await CreateController().List() as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreSame(listModel, result.Model);
    }

    [TestMethod]
    public async Task ListPost_Admin_PreservesCallerSubmittedSearchStoreId()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns((string)null);
        _viewModelService
            .Setup(s => s.PrepareProductReviewsModel(It.IsAny<ProductReviewListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<ProductReviewModel>(), 0));

        var model = new ProductReviewListModel { SearchStoreId = "caller-chosen-store" };
        await CreateController().List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("caller-chosen-store", model.SearchStoreId);
    }

    [TestMethod]
    public async Task ListPost_Store_ForcesSearchStoreIdRegardlessOfSubmittedValue()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");
        _viewModelService
            .Setup(s => s.PrepareProductReviewsModel(It.IsAny<ProductReviewListModel>(), 1, 10))
            .ReturnsAsync((Enumerable.Empty<ProductReviewModel>(), 0));

        var model = new ProductReviewListModel { SearchStoreId = "some-other-store" };
        await CreateController().List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.SearchStoreId);
    }

    [TestMethod]
    public async Task EditGet_NotFound_RedirectsToList()
    {
        _productReviewService.Setup(s => s.GetProductReviewById("missing")).ReturnsAsync((ProductReview)null);

        var result = await CreateController().Edit("missing") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public async Task EditGet_HasAccessFalse_RedirectsToList()
    {
        var review = new ProductReview { Id = "pr-1", StoreId = "store-2" };
        _productReviewService.Setup(s => s.GetProductReviewById("pr-1")).ReturnsAsync(review);
        _scope.Setup(s => s.HasAccess(review)).ReturnsAsync(false);

        var result = await CreateController().Edit("pr-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public async Task EditGet_HasAccessTrue_ReturnsViewWithModel()
    {
        var review = new ProductReview { Id = "pr-1", StoreId = "store-1" };
        _productReviewService.Setup(s => s.GetProductReviewById("pr-1")).ReturnsAsync(review);
        _scope.Setup(s => s.HasAccess(review)).ReturnsAsync(true);

        var result = await CreateController().Edit("pr-1") as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result.Model, typeof(ProductReviewModel));
        _viewModelService.Verify(s => s.PrepareProductReviewModel(It.IsAny<ProductReviewModel>(), review, false, false), Times.Once);
    }

    [TestMethod]
    public async Task EditPost_HasAccessFalse_RedirectsToListWithoutSaving()
    {
        var review = new ProductReview { Id = "pr-1", StoreId = "store-2" };
        _productReviewService.Setup(s => s.GetProductReviewById("pr-1")).ReturnsAsync(review);
        _scope.Setup(s => s.HasAccess(review)).ReturnsAsync(false);

        var result = await CreateController().Edit(new ProductReviewModel { Id = "pr-1" }, false) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        _viewModelService.Verify(s => s.UpdateProductReview(It.IsAny<ProductReview>(), It.IsAny<ProductReviewModel>()), Times.Never);
    }

    [TestMethod]
    public async Task EditPost_HasAccessTrue_ValidModel_UpdatesAndRedirectsToList()
    {
        var review = new ProductReview { Id = "pr-1", StoreId = "store-1", ProductId = "prod-1" };
        _productReviewService.Setup(s => s.GetProductReviewById("pr-1")).ReturnsAsync(review);
        _scope.Setup(s => s.HasAccess(review)).ReturnsAsync(true);
        _viewModelService.Setup(s => s.UpdateProductReview(review, It.IsAny<ProductReviewModel>())).ReturnsAsync(review);
        _translationService.Setup(s => s.GetResource(It.IsAny<string>())).Returns("Updated");

        var result = await CreateController().Edit(new ProductReviewModel { Id = "pr-1" }, false) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public async Task EditPost_ValidModel_ContinueEditing_RedirectsToEdit()
    {
        var review = new ProductReview { Id = "pr-1", StoreId = "store-1", ProductId = "prod-1" };
        _productReviewService.Setup(s => s.GetProductReviewById("pr-1")).ReturnsAsync(review);
        _scope.Setup(s => s.HasAccess(review)).ReturnsAsync(true);
        _viewModelService.Setup(s => s.UpdateProductReview(review, It.IsAny<ProductReviewModel>())).ReturnsAsync(review);
        _translationService.Setup(s => s.GetResource(It.IsAny<string>())).Returns("Updated");

        var result = await CreateController().Edit(new ProductReviewModel { Id = "pr-1" }, true) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result.ActionName);
    }

    [TestMethod]
    public async Task EditPost_InvalidModel_RedisplaysView()
    {
        var review = new ProductReview { Id = "pr-1", StoreId = "store-1" };
        _productReviewService.Setup(s => s.GetProductReviewById("pr-1")).ReturnsAsync(review);
        _scope.Setup(s => s.HasAccess(review)).ReturnsAsync(true);

        var controller = CreateController();
        controller.ModelState.AddModelError("Test", "Test error");

        var model = new ProductReviewModel { Id = "pr-1" };
        var result = await controller.Edit(model, false) as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreSame(model, result.Model);
        _viewModelService.Verify(s => s.PrepareProductReviewModel(model, review, true, false), Times.Once);
        _viewModelService.Verify(s => s.UpdateProductReview(It.IsAny<ProductReview>(), It.IsAny<ProductReviewModel>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_NotFound_RedirectsToList()
    {
        _productReviewService.Setup(s => s.GetProductReviewById("missing")).ReturnsAsync((ProductReview)null);

        var result = await CreateController().Delete("missing") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public async Task Delete_HasAccessFalse_RedirectsToListWithoutDeleting()
    {
        var review = new ProductReview { Id = "pr-1", StoreId = "store-2" };
        _productReviewService.Setup(s => s.GetProductReviewById("pr-1")).ReturnsAsync(review);
        _scope.Setup(s => s.HasAccess(review)).ReturnsAsync(false);

        var result = await CreateController().Delete("pr-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        _viewModelService.Verify(s => s.DeleteProductReview(It.IsAny<ProductReview>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_HasAccessTrue_DeletesAndRedirectsToList()
    {
        var review = new ProductReview { Id = "pr-1", StoreId = "store-1" };
        _productReviewService.Setup(s => s.GetProductReviewById("pr-1")).ReturnsAsync(review);
        _scope.Setup(s => s.HasAccess(review)).ReturnsAsync(true);
        _translationService.Setup(s => s.GetResource(It.IsAny<string>())).Returns("Deleted");

        var result = await CreateController().Delete("pr-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        _viewModelService.Verify(s => s.DeleteProductReview(review), Times.Once);
    }

    [TestMethod]
    public async Task Delete_HasAccessTrue_InvalidModelState_DoesNotDeleteAndRedirectsToEdit()
    {
        var review = new ProductReview { Id = "pr-1", StoreId = "store-1" };
        _productReviewService.Setup(s => s.GetProductReviewById("pr-1")).ReturnsAsync(review);
        _scope.Setup(s => s.HasAccess(review)).ReturnsAsync(true);

        var controller = CreateController();
        controller.ModelState.AddModelError("Test", "Test error");

        var result = await controller.Delete("pr-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result.ActionName);
        _viewModelService.Verify(s => s.DeleteProductReview(It.IsAny<ProductReview>()), Times.Never);
    }

    [TestMethod]
    public async Task ApproveSelected_PassesDefaultStoreId()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");

        await CreateController().ApproveSelected(new List<string> { "pr-1:prod-1" });

        _viewModelService.Verify(s => s.ApproveSelected(It.Is<IEnumerable<string>>(ids => ids.Contains("pr-1:prod-1")), "store-1"), Times.Once);
    }

    [TestMethod]
    public async Task ApproveSelected_Admin_PassesEmptyStringNotNull()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns((string)null);

        await CreateController().ApproveSelected(new List<string> { "pr-1:prod-1" });

        _viewModelService.Verify(s => s.ApproveSelected(It.IsAny<IEnumerable<string>>(), ""), Times.Once);
    }

    [TestMethod]
    public async Task DisapproveSelected_PassesDefaultStoreId()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");

        await CreateController().DisapproveSelected(new List<string> { "pr-1:prod-1" });

        _viewModelService.Verify(s => s.DisapproveSelected(It.Is<IEnumerable<string>>(ids => ids.Contains("pr-1:prod-1")), "store-1"), Times.Once);
    }

    [TestMethod]
    public async Task ApproveSelected_NullSelectedIds_DoesNotCallService()
    {
        await CreateController().ApproveSelected(null);

        _viewModelService.Verify(s => s.ApproveSelected(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductSearchAutoComplete_TermTooShort_ReturnsEmptyContent()
    {
        var productService = new Mock<IProductService>();

        var result = await CreateController().ProductSearchAutoComplete("ab", productService.Object) as ContentResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("", result.Content);
        productService.Verify(s => s.SearchProducts(
            loadFilterableSpecificationAttributeOptionIds: It.IsAny<bool>(),
            pageIndex: It.IsAny<int>(),
            pageSize: It.IsAny<int>(),
            categoryIds: It.IsAny<IList<string>>(),
            brandId: It.IsAny<string>(),
            collectionId: It.IsAny<string>(),
            storeId: It.IsAny<string>(),
            vendorId: It.IsAny<string>(),
            warehouseId: It.IsAny<string>(),
            productType: It.IsAny<Grand.Domain.Catalog.ProductType?>(),
            visibleIndividuallyOnly: It.IsAny<bool>(),
            markedAsNewOnly: It.IsAny<bool>(),
            showOnHomePage: It.IsAny<bool?>(),
            featuredProducts: It.IsAny<bool?>(),
            priceMin: It.IsAny<double?>(),
            priceMax: It.IsAny<double?>(),
            productTag: It.IsAny<string>(),
            keywords: It.IsAny<string>(),
            searchDescriptions: It.IsAny<bool>(),
            searchSku: It.IsAny<bool>(),
            searchProductTags: It.IsAny<bool>(),
            languageId: It.IsAny<string>(),
            filteredSpecs: It.IsAny<IList<string>>(),
            specificationOptions: It.IsAny<IList<string>>(),
            orderBy: It.IsAny<Grand.Domain.Catalog.ProductSortingEnum>(),
            showHidden: It.IsAny<bool>(),
            overridePublished: It.IsAny<bool?>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductSearchAutoComplete_Store_PassesDefaultStoreId()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.SearchProducts(
                loadFilterableSpecificationAttributeOptionIds: false,
                pageIndex: 0,
                pageSize: 15,
                categoryIds: null,
                brandId: "",
                collectionId: "",
                storeId: "store-1",
                vendorId: "",
                warehouseId: "",
                productType: null,
                visibleIndividuallyOnly: false,
                markedAsNewOnly: false,
                showOnHomePage: null,
                featuredProducts: null,
                priceMin: null,
                priceMax: null,
                productTag: "",
                keywords: "widget",
                searchDescriptions: false,
                searchSku: true,
                searchProductTags: false,
                languageId: "",
                filteredSpecs: null,
                specificationOptions: null,
                orderBy: Grand.Domain.Catalog.ProductSortingEnum.Position,
                showHidden: true,
                overridePublished: null))
            .ReturnsAsync((new Grand.Domain.PagedList<Grand.Domain.Catalog.Product>(new List<Grand.Domain.Catalog.Product>(), 0, 15), new List<string>()));

        var result = await CreateController().ProductSearchAutoComplete("widget", productService.Object) as JsonResult;

        Assert.IsNotNull(result);
    }
}
