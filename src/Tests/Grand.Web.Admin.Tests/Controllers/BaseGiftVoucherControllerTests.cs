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

    [TestMethod]
    public async Task EditGet_NotFound_RedirectsToList()
    {
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("missing")).ReturnsAsync((GiftVoucher)null);

        var result = await CreateController().Edit("missing") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public async Task EditGet_CanViewFalse_RedirectsToList()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "store-2" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.CanView(giftVoucher)).ReturnsAsync(false);

        var result = await CreateController().Edit("gv-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public async Task EditGet_CanViewTrue_ReturnsViewWithModel()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "" };
        var model = new GiftVoucherModel { Id = "gv-1" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.CanView(giftVoucher)).ReturnsAsync(true);
        _scope.Setup(s => s.DefaultStoreId).Returns((string)null);
        _viewModelService.Setup(s => s.PrepareGiftVoucherModel(giftVoucher)).ReturnsAsync(model);

        var result = await CreateController().Edit("gv-1") as ViewResult;

        Assert.IsNotNull(result);
        Assert.AreSame(model, result.Model);
    }

    [TestMethod]
    public async Task EditPost_HasAccessFalse_RedirectsToEditWithoutSaving()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "store-2" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.HasAccess(giftVoucher)).ReturnsAsync(false);

        var result = await CreateController().Edit(new GiftVoucherModel { Id = "gv-1" }, false) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result.ActionName);
        _viewModelService.Verify(s => s.UpdateGiftVoucherModel(It.IsAny<GiftVoucher>(), It.IsAny<GiftVoucherModel>()), Times.Never);
    }

    [TestMethod]
    public async Task EditPost_HasAccessTrue_ForcesStoreIdWhenScoped_ThenSaves()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "store-1" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.HasAccess(giftVoucher)).ReturnsAsync(true);
        _scope.Setup(s => s.DefaultStoreId).Returns("store-1");
        _viewModelService.Setup(s => s.FillGiftVoucherModel(giftVoucher, It.IsAny<GiftVoucherModel>()))
            .ReturnsAsync((GiftVoucher gv2, GiftVoucherModel m2) => m2);
        _viewModelService.Setup(s => s.UpdateGiftVoucherModel(giftVoucher, It.IsAny<GiftVoucherModel>())).ReturnsAsync(giftVoucher);
        _translationService.Setup(s => s.GetResource(It.IsAny<string>())).Returns("Updated");

        var model = new GiftVoucherModel { Id = "gv-1" };
        var result = await CreateController().Edit(model, false) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        Assert.AreEqual("store-1", model.StoreId);
    }

    [TestMethod]
    public async Task Delete_HasAccessFalse_RedirectsToEditWithoutDeleting()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "store-2" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.HasAccess(giftVoucher)).ReturnsAsync(false);

        var result = await CreateController().Delete(new GiftVoucherDeleteModel("gv-1")) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result.ActionName);
        _viewModelService.Verify(s => s.DeleteGiftVoucher(It.IsAny<GiftVoucher>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_HasAccessTrue_InvalidModelState_DoesNotDeleteAndRedirectsToEdit()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "store-1" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.HasAccess(giftVoucher)).ReturnsAsync(true);

        var controller = CreateController();
        controller.ModelState.AddModelError("Test", "Test error");

        var result = await controller.Delete(new GiftVoucherDeleteModel("gv-1")) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result.ActionName);
        _viewModelService.Verify(s => s.DeleteGiftVoucher(It.IsAny<GiftVoucher>()), Times.Never);
        Assert.IsTrue(controller.TempData["grand.notifications.Error"] is List<string> errors
                      && errors.Contains("Test error"));
    }

    [TestMethod]
    public async Task Delete_HasAccessTrue_DeletesAndRedirectsToList()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "store-1" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.HasAccess(giftVoucher)).ReturnsAsync(true);
        _translationService.Setup(s => s.GetResource(It.IsAny<string>())).Returns("Deleted");

        var result = await CreateController().Delete(new GiftVoucherDeleteModel("gv-1")) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result.ActionName);
        _viewModelService.Verify(s => s.DeleteGiftVoucher(giftVoucher), Times.Once);
    }

    [TestMethod]
    public async Task NotifyRecipient_HasAccessFalse_RedirectsWithoutNotifying()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "store-2" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.HasAccess(giftVoucher)).ReturnsAsync(false);

        var result = await CreateController().NotifyRecipient(new GiftVoucherNotifyRecipient("gv-1")) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result.ActionName);
        _viewModelService.Verify(s => s.NotifyRecipient(It.IsAny<GiftVoucher>()), Times.Never);
    }

    [TestMethod]
    public async Task UsageHistoryList_HasAccessTrue_ReturnsGrid()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "store-1" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.HasAccess(giftVoucher)).ReturnsAsync(true);
        _viewModelService.Setup(s => s.PrepareGiftVoucherUsageHistoryModels(giftVoucher, 1, 10))
            .ReturnsAsync((Enumerable.Empty<GiftVoucherModel.GiftVoucherUsageHistoryModel>(), 0));

        var result = await CreateController().UsageHistoryList("gv-1",
            new DataSourceRequest { Page = 1, PageSize = 10 }) as JsonResult;

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task UsageHistoryList_HasAccessFalse_ThrowsArgumentException()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "store-2" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.HasAccess(giftVoucher)).ReturnsAsync(false);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            CreateController().UsageHistoryList("gv-1", new DataSourceRequest { Page = 1, PageSize = 10 }));
    }

    // Regression test for the leak this fix closes: a global voucher (empty StoreId) is
    // CanView == true (Edit itself stays viewable read-only for it) but must NOT be admitted to
    // UsageHistoryList, because its usage-history rows can reference other stores' orders. Before
    // the fix, UsageHistoryList gated on CanView and would have returned the grid here.
    [TestMethod]
    public async Task UsageHistoryList_GlobalVoucher_CanViewTrueButHasAccessFalse_ThrowsArgumentException()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.CanView(giftVoucher)).ReturnsAsync(true);
        _scope.Setup(s => s.HasAccess(giftVoucher)).ReturnsAsync(false);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            CreateController().UsageHistoryList("gv-1", new DataSourceRequest { Page = 1, PageSize = 10 }));
    }

    [TestMethod]
    public async Task EditPost_InvalidModel_CallsPrepareGiftVoucherModelToRepopulateAvailableStores()
    {
        var giftVoucher = new GiftVoucher { Id = "gv-1", StoreId = "" };
        _giftVoucherService.Setup(s => s.GetGiftVoucherById("gv-1")).ReturnsAsync(giftVoucher);
        _scope.Setup(s => s.HasAccess(giftVoucher)).ReturnsAsync(true);
        _scope.Setup(s => s.DefaultStoreId).Returns((string)null);

        // Simulate invalid model state by returning the model with AvailableStores populated
        var preparedModel = new GiftVoucherModel {
            Id = "gv-1",
            AvailableStores = {
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "", Text = "All" },
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = "store-1", Text = "Store 1" }
            }
        };
        _viewModelService.Setup(s => s.FillGiftVoucherModel(giftVoucher, It.IsAny<GiftVoucherModel>()))
            .ReturnsAsync((GiftVoucher gv2, GiftVoucherModel m2) => m2);
        _viewModelService.Setup(s => s.PrepareGiftVoucherModel(It.IsAny<GiftVoucherModel>()))
            .ReturnsAsync(preparedModel);

        var controller = CreateController();
        // Force ModelState.IsValid to be false by adding a model error
        controller.ModelState.AddModelError("Test", "Test error");

        var model = new GiftVoucherModel { Id = "gv-1" };
        var result = await controller.Edit(model, false) as ViewResult;

        Assert.IsNotNull(result);
        var returnedModel = result.Model as GiftVoucherModel;
        Assert.IsNotNull(returnedModel);
        // Verify that AvailableStores was populated by PrepareGiftVoucherModel
        Assert.AreEqual(2, returnedModel.AvailableStores.Count);
        _viewModelService.Verify(s => s.PrepareGiftVoucherModel(It.IsAny<GiftVoucherModel>()), Times.Once);
    }
}
