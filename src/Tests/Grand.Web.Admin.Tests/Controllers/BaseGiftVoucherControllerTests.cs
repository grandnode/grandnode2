using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
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
public class BaseGiftVoucherControllerTests
{
    private Mock<IGiftVoucherViewModelService> _viewModelService;
    private Mock<IGiftVoucherService> _giftVoucherService;
    private Mock<ITranslationService> _translationService;
    private Mock<IAdminDataScope<GiftVoucher>> _scope;

    private class TestableGiftVoucherController(
        IGiftVoucherViewModelService viewModelService,
        IGiftVoucherService giftVoucherService,
        ITranslationService translationService,
        IAdminDataScope<GiftVoucher> scope)
        : BaseGiftVoucherController(viewModelService, giftVoucherService, translationService, scope);

    private TestableGiftVoucherController CreateController()
    {
        var controller = new TestableGiftVoucherController(_viewModelService.Object, _giftVoucherService.Object,
            _translationService.Object, _scope.Object);

        // Set up HTTP context and controller context for BaseController methods (Success, etc.)
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
        _viewModelService = new Mock<IGiftVoucherViewModelService>();
        _giftVoucherService = new Mock<IGiftVoucherService>();
        _translationService = new Mock<ITranslationService>();
        _scope = new Mock<IAdminDataScope<GiftVoucher>>();
    }

    [TestMethod]
    public void List_ReturnsViewWithModel()
    {
        var listModel = new GiftVoucherListModel();
        _viewModelService.Setup(s => s.PrepareGiftVoucherListModel()).Returns(listModel);

        var result = CreateController().List() as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreSame(listModel, result.Model);
    }

    [TestMethod]
    public async Task GiftVoucherList_Admin_PassesNullDefaultStoreIdAsEmptyString()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns((string)null);
        _viewModelService
            .Setup(s => s.PrepareGiftVoucherModel(It.IsAny<GiftVoucherListModel>(), 1, 10, ""))
            .ReturnsAsync((Enumerable.Empty<GiftVoucherModel>(), 0));

        var result = await CreateController().GiftVoucherList(
            new DataSourceRequest { Page = 1, PageSize = 10 }, new GiftVoucherListModel()) as JsonResult;

        Assert.IsNotNull(result);
        _viewModelService.Verify(s => s.PrepareGiftVoucherModel(It.IsAny<GiftVoucherListModel>(), 1, 10, ""), Times.Once);
    }

    [TestMethod]
    public async Task GiftVoucherList_Store_PassesDefaultStoreId()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");
        _viewModelService
            .Setup(s => s.PrepareGiftVoucherModel(It.IsAny<GiftVoucherListModel>(), 1, 10, "store-1"))
            .ReturnsAsync((Enumerable.Empty<GiftVoucherModel>(), 0));

        await CreateController().GiftVoucherList(
            new DataSourceRequest { Page = 1, PageSize = 10 }, new GiftVoucherListModel());

        _viewModelService.Verify(s => s.PrepareGiftVoucherModel(It.IsAny<GiftVoucherListModel>(), 1, 10, "store-1"), Times.Once);
    }

    [TestMethod]
    public void GenerateCouponCode_ReturnsJsonWithGeneratedCode()
    {
        _giftVoucherService.Setup(s => s.GenerateGiftVoucherCode()).Returns("ABC123");

        var result = CreateController().GenerateCouponCode() as JsonResult;

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task CreateGet_Admin_DoesNotForceStoreId()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns((string)null);
        var model = new GiftVoucherModel {
            AvailableStores = { new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "", Text = "All" },
                                 new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "store-1", Text = "Store 1" } }
        };
        _viewModelService.Setup(s => s.PrepareGiftVoucherModel((GiftVoucherModel)null)).ReturnsAsync(model);

        var result = await CreateController().Create() as ViewResult;
        var returnedModel = result?.Model as GiftVoucherModel;

        Assert.IsNotNull(returnedModel);
        Assert.AreEqual(2, returnedModel.AvailableStores.Count);
        Assert.AreEqual("", returnedModel.StoreId ?? "");
    }

    [TestMethod]
    public async Task CreateGet_Store_ForcesStoreIdAndFiltersAvailableStores()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");
        var model = new GiftVoucherModel {
            AvailableStores = { new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "", Text = "All" },
                                 new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "store-1", Text = "Store 1" },
                                 new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "store-2", Text = "Store 2" } }
        };
        _viewModelService.Setup(s => s.PrepareGiftVoucherModel((GiftVoucherModel)null)).ReturnsAsync(model);

        var result = await CreateController().Create() as ViewResult;
        var returnedModel = result?.Model as GiftVoucherModel;

        Assert.IsNotNull(returnedModel);
        Assert.AreEqual("store-1", returnedModel.StoreId);
        Assert.AreEqual(1, returnedModel.AvailableStores.Count);
        Assert.AreEqual("store-1", returnedModel.AvailableStores[0].Value);
    }

    [TestMethod]
    public async Task CreatePost_ValidModel_ForcesStoreIdWhenScoped_ThenInserts()
    {
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");
        var inserted = new GiftVoucher { Id = "gv-1", StoreId = "store-1" };
        _viewModelService.Setup(s => s.InsertGiftVoucherModel(It.IsAny<GiftVoucherModel>())).ReturnsAsync(inserted);
        _translationService.Setup(s => s.GetResource(It.IsAny<string>())).Returns("Added");

        var controller = CreateController();
        var model = new GiftVoucherModel();

        var result = await controller.Create(model, false) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        Assert.AreEqual("store-1", model.StoreId);
        _viewModelService.Verify(s => s.InsertGiftVoucherModel(model), Times.Once);
    }
}
