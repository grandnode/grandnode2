extern alias StoreHost;

using Grand.Business.Core.Interfaces.Checkout.Orders;
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
public class BaseMerchandiseReturnControllerTests
{
    // BaseMerchandiseReturnController is abstract; minimal subclass so actions can be invoked
    // directly. NotFoundOrDeniedForNotesSelect (Task 8) uses the base's default (throw) here - Store's
    // own override is tested separately in Task 8's tests.
    protected class TestMerchandiseReturnController(
        IMerchandiseReturnViewModelService merchandiseReturnViewModelService,
        ITranslationService translationService,
        IMerchandiseReturnService merchandiseReturnService,
        IOrderService orderService,
        IAdminDataScope<MerchandiseReturn> scope)
        : BaseMerchandiseReturnController(merchandiseReturnViewModelService, translationService,
            merchandiseReturnService, orderService, scope);

    protected TestMerchandiseReturnController _controller;
    protected Mock<IMerchandiseReturnService> _merchandiseReturnServiceMock;
    protected Mock<IMerchandiseReturnViewModelService> _merchandiseReturnViewModelServiceMock;
    protected Mock<IOrderService> _orderServiceMock;
    protected Mock<IAdminDataScope<MerchandiseReturn>> _scopeMock;

    [TestInitialize]
    public void Setup()
    {
        _merchandiseReturnServiceMock = new Mock<IMerchandiseReturnService>();
        _merchandiseReturnViewModelServiceMock = new Mock<IMerchandiseReturnViewModelService>();
        _orderServiceMock = new Mock<IOrderService>();
        _scopeMock = new Mock<IAdminDataScope<MerchandiseReturn>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _controller = new TestMerchandiseReturnController(
            _merchandiseReturnViewModelServiceMock.Object,
            translationServiceMock.Object,
            _merchandiseReturnServiceMock.Object,
            _orderServiceMock.Object,
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
    public void ListGet_ReturnsViewWithPrepareReturnRequestListModelResult()
    {
        var model = new MerchandiseReturnListModel();
        _merchandiseReturnViewModelServiceMock.Setup(v => v.PrepareReturnRequestListModel()).Returns(model);

        var result = _controller.List();

        var view = result as ViewResult;
        Assert.IsNotNull(view);
        Assert.AreSame(model, view.Model);
    }

    [TestMethod]
    public async Task ListPost_StoreScope_ForcesModelStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _merchandiseReturnViewModelServiceMock
            .Setup(v => v.PrepareMerchandiseReturnModel(It.IsAny<MerchandiseReturnListModel>(), 1, 10))
            .ReturnsAsync((new List<MerchandiseReturnModel>(), 0));

        var model = new MerchandiseReturnListModel { StoreId = "attacker-supplied" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("store-1", model.StoreId);
    }

    [TestMethod]
    public async Task ListPost_GlobalScope_LeavesSubmittedStoreIdUntouched()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _merchandiseReturnViewModelServiceMock
            .Setup(v => v.PrepareMerchandiseReturnModel(It.IsAny<MerchandiseReturnListModel>(), 1, 10))
            .ReturnsAsync((new List<MerchandiseReturnModel>(), 0));

        var model = new MerchandiseReturnListModel { StoreId = "admin-submitted" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.AreEqual("admin-submitted", model.StoreId);
    }

    [TestMethod]
    public async Task ListPost_VendorScope_LeavesStoreIdUntouched_ScopingHappensInsideService()
    {
        // Vendor has no store concept - List(POST) never forces model.StoreId for Vendor (spec §5);
        // vendor-scoping happens inside the shared service call via scope.DefaultVendorId (Task 4).
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor-A");
        _merchandiseReturnViewModelServiceMock
            .Setup(v => v.PrepareMerchandiseReturnModel(It.IsAny<MerchandiseReturnListModel>(), 1, 10))
            .ReturnsAsync((new List<MerchandiseReturnModel>(), 0));

        var model = new MerchandiseReturnListModel();
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        Assert.IsNull(model.StoreId);
    }

    // --- GoToId --------------------------------------------------------------------------------

    [TestMethod]
    public async Task GoToId_NullGoDirectlyToId_RedirectsToList()
    {
        var result = await _controller.GoToId(new MerchandiseReturnListModel { GoDirectlyToId = null });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _merchandiseReturnServiceMock.Verify(s => s.GetMerchandiseReturnById(It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task GoToId_NotFound_RedirectsToList()
    {
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById(42)).ReturnsAsync((MerchandiseReturn)null);

        var result = await _controller.GoToId(new MerchandiseReturnListModel { GoDirectlyToId = "42" });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task GoToId_ScopeDenies_RedirectsToList()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById(42)).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(false);

        var result = await _controller.GoToId(new MerchandiseReturnListModel { GoDirectlyToId = "42" });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task GoToId_NonNumericString_TreatedAsNotFound()
    {
        // int.TryParse("abc", out var id) leaves id = 0; GetMerchandiseReturnById(0) returning null
        // reproduces "not found" the same way it did before this phase (spec §2.6 - no regression).
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById(0)).ReturnsAsync((MerchandiseReturn)null);

        var result = await _controller.GoToId(new MerchandiseReturnListModel { GoDirectlyToId = "abc" });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task GoToId_Authorized_RedirectsToEdit()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById(42)).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(true);

        var result = await _controller.GoToId(new MerchandiseReturnListModel { GoDirectlyToId = "42" });

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("mr1", redirect.RouteValues["id"]);
    }

    // --- ProductsForMerchandiseReturn ------------------------------------------------------------

    [TestMethod]
    public async Task ProductsForMerchandiseReturn_NotFound_ReturnsKendoError()
    {
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync((MerchandiseReturn)null);

        var result = await _controller.ProductsForMerchandiseReturn("mr1", new DataSourceRequest());

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsFalse(string.IsNullOrEmpty((string)gridModel.Errors));
    }

    [TestMethod]
    public async Task ProductsForMerchandiseReturn_ScopeDenies_ReturnsKendoError()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(false);

        var result = await _controller.ProductsForMerchandiseReturn("mr1", new DataSourceRequest());

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        var gridModel = (DataSourceResult)json.Value;
        Assert.IsFalse(string.IsNullOrEmpty((string)gridModel.Errors));
    }

    [TestMethod]
    public async Task ProductsForMerchandiseReturn_Authorized_ReturnsItems()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(true);
        var items = new List<MerchandiseReturnModel.MerchandiseReturnItemModel> { new() };
        _merchandiseReturnViewModelServiceMock.Setup(v => v.PrepareMerchandiseReturnItemModel("mr1")).ReturnsAsync(items);

        var result = await _controller.ProductsForMerchandiseReturn("mr1", new DataSourceRequest());

        var json = result as JsonResult;
        var gridModel = (DataSourceResult)json.Value;
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- Edit (GET) ------------------------------------------------------------------------------

    [TestMethod]
    public async Task EditGet_NotFound_RedirectsToList()
    {
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("missing")).ReturnsAsync((MerchandiseReturn)null);

        var result = await _controller.Edit("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _scopeMock.Verify(s => s.CanView(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task EditGet_ScopeDeniesView_RedirectsToList()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.CanView(entity)).ReturnsAsync(false);

        var result = await _controller.Edit("mr1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task EditGet_Authorized_ReturnsViewAndPreparesModel()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.CanView(entity)).ReturnsAsync(true);

        var result = await _controller.Edit("mr1");

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        _merchandiseReturnViewModelServiceMock.Verify(
            v => v.PrepareMerchandiseReturnModel(It.IsAny<MerchandiseReturnModel>(), entity, false), Times.Once);
    }

    // --- Edit (POST) -----------------------------------------------------------------------------

    [TestMethod]
    public async Task EditPost_NotFound_RedirectsToList()
    {
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("missing")).ReturnsAsync((MerchandiseReturn)null);
        var orderSettings = new Grand.Domain.Orders.OrderSettings();

        var result = await _controller.Edit(new MerchandiseReturnModel { Id = "missing" }, false,
            new Mock<Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeService>().Object,
            new Mock<Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeParser>().Object,
            orderSettings);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task EditPost_ScopeDenies_RedirectsToList()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(false);
        var orderSettings = new Grand.Domain.Orders.OrderSettings();

        var result = await _controller.Edit(new MerchandiseReturnModel { Id = "mr1" }, false,
            new Mock<Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeService>().Object,
            new Mock<Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeParser>().Object,
            orderSettings);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _merchandiseReturnViewModelServiceMock.Verify(
            v => v.UpdateMerchandiseReturnModel(It.IsAny<MerchandiseReturn>(), It.IsAny<MerchandiseReturnModel>(),
                It.IsAny<List<Grand.Domain.Common.CustomAttribute>>()), Times.Never);
    }

    [TestMethod]
    public async Task EditPost_ValidAndAuthorized_UpdatesAndRedirectsToList()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(true);
        _merchandiseReturnViewModelServiceMock
            .Setup(v => v.UpdateMerchandiseReturnModel(entity, It.IsAny<MerchandiseReturnModel>(), It.IsAny<List<Grand.Domain.Common.CustomAttribute>>()))
            .ReturnsAsync(entity);
        var orderSettings = new Grand.Domain.Orders.OrderSettings { MerchandiseReturns_AllowToSpecifyPickupAddress = false };

        var result = await _controller.Edit(new MerchandiseReturnModel { Id = "mr1" }, false,
            new Mock<Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeService>().Object,
            new Mock<Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeParser>().Object,
            orderSettings);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    // --- Delete (POST) ---------------------------------------------------------------------------

    [TestMethod]
    public async Task Delete_NotFound_RedirectsToList()
    {
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("missing")).ReturnsAsync((MerchandiseReturn)null);

        var result = await _controller.Delete("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
    }

    [TestMethod]
    public async Task Delete_ScopeDenies_RedirectsToListWithoutDeleting()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(false);

        var result = await _controller.Delete("mr1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _merchandiseReturnViewModelServiceMock.Verify(v => v.DeleteMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_Authorized_DeletesAndRedirectsToList()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(true);

        var result = await _controller.Delete("mr1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        _merchandiseReturnViewModelServiceMock.Verify(v => v.DeleteMerchandiseReturn(entity), Times.Once);
    }

    // --- MerchandiseReturnNotesSelect --------------------------------------------------------------

    [TestMethod]
    public async Task NotesSelect_NotFound_BaseDefault_ThrowsArgumentException()
    {
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("missing")).ReturnsAsync((MerchandiseReturn)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _controller.MerchandiseReturnNotesSelect("missing", new DataSourceRequest()));
    }

    [TestMethod]
    public async Task NotesSelect_ScopeDenies_BaseDefault_ThrowsArgumentException()
    {
        // Base default is the Admin/Vendor shape (throw) - Store's subclass override is tested in
        // Task 10's thin-subclass tests, not here.
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(false);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _controller.MerchandiseReturnNotesSelect("mr1", new DataSourceRequest()));
    }

    [TestMethod]
    public async Task NotesSelect_Authorized_ReturnsNotes()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(true);
        var notes = new List<MerchandiseReturnModel.MerchandiseReturnNote> { new() };
        _merchandiseReturnViewModelServiceMock.Setup(v => v.PrepareMerchandiseReturnNotes(entity)).ReturnsAsync(notes);

        var result = await _controller.MerchandiseReturnNotesSelect("mr1", new DataSourceRequest());

        var json = result as JsonResult;
        var gridModel = (DataSourceResult)json.Value;
        Assert.AreEqual(1, gridModel.Total);
    }

    // --- MerchandiseReturnNoteAdd ------------------------------------------------------------------

    [TestMethod]
    public async Task NoteAdd_NotFound_ReturnsJsonResultFalse()
    {
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("missing")).ReturnsAsync((MerchandiseReturn)null);

        var result = await _controller.MerchandiseReturnNoteAdd("missing", null, false, "msg");

        var json = result as JsonResult;
        Assert.IsFalse((bool)json.Value.GetType().GetProperty("Result").GetValue(json.Value));
        _orderServiceMock.Verify(o => o.GetOrderById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task NoteAdd_ScopeDenies_ReturnsJsonResultFalse()
    {
        var entity = new MerchandiseReturn { Id = "mr1", OrderId = "o1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(false);

        var result = await _controller.MerchandiseReturnNoteAdd("mr1", null, false, "msg");

        var json = result as JsonResult;
        Assert.IsFalse((bool)json.Value.GetType().GetProperty("Result").GetValue(json.Value));
        _merchandiseReturnViewModelServiceMock.Verify(
            v => v.InsertMerchandiseReturnNote(It.IsAny<MerchandiseReturn>(), It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task NoteAdd_Authorized_ResolvesOrderFromEntity_NotFromAParameter()
    {
        // Spec §2.5/§11 (approved): no orderId request parameter exists on this action at all -
        // order is always resolved server-side from merchandiseReturn.OrderId.
        var entity = new MerchandiseReturn { Id = "mr1", OrderId = "o1" };
        var order = new Order { Id = "o1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(true);
        _orderServiceMock.Setup(o => o.GetOrderById("o1")).ReturnsAsync(order);

        var result = await _controller.MerchandiseReturnNoteAdd("mr1", "download-1", true, "msg");

        var json = result as JsonResult;
        Assert.IsTrue((bool)json.Value.GetType().GetProperty("Result").GetValue(json.Value));
        _orderServiceMock.Verify(o => o.GetOrderById("o1"), Times.Once);
        _merchandiseReturnViewModelServiceMock.Verify(
            v => v.InsertMerchandiseReturnNote(entity, order, "download-1", true, "msg"), Times.Once);
    }

    // --- MerchandiseReturnNoteDelete ---------------------------------------------------------------

    [TestMethod]
    public async Task NoteDelete_NotFound_ThrowsArgumentException()
    {
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("missing")).ReturnsAsync((MerchandiseReturn)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            async () => await _controller.MerchandiseReturnNoteDelete("note1", "missing"));
    }

    [TestMethod]
    public async Task NoteDelete_ScopeDenies_ReturnsJsonResultFalse_ForEveryHost()
    {
        // Store's existing check moves here uniformly; Admin/Vendor gain it for free
        // (GlobalAdminDataScope's always-true HasAccess keeps Admin unaffected) - spec §5.
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(false);

        var result = await _controller.MerchandiseReturnNoteDelete("note1", "mr1");

        var json = result as JsonResult;
        Assert.IsFalse((bool)json.Value.GetType().GetProperty("Result").GetValue(json.Value));
        _merchandiseReturnViewModelServiceMock.Verify(
            v => v.DeleteMerchandiseReturnNote(It.IsAny<MerchandiseReturn>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task NoteDelete_Authorized_Deletes()
    {
        var entity = new MerchandiseReturn { Id = "mr1" };
        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("mr1")).ReturnsAsync(entity);
        _scopeMock.Setup(s => s.HasAccess(entity)).ReturnsAsync(true);

        var result = await _controller.MerchandiseReturnNoteDelete("note1", "mr1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        _merchandiseReturnViewModelServiceMock.Verify(v => v.DeleteMerchandiseReturnNote(entity, "note1"), Times.Once);
    }

    [TestMethod]
    public async Task NotesSelect_NotFoundOrDenied_StoreSubclass_ReturnsEmptyContent_NoThrow()
    {
        var storeController = new StoreHost::Grand.Web.Store.Controllers.MerchandiseReturnController(
            _merchandiseReturnViewModelServiceMock.Object,
            new Mock<ITranslationService>().Object,
            _merchandiseReturnServiceMock.Object,
            _orderServiceMock.Object,
            _scopeMock.Object);
        storeController.ControllerContext = _controller.ControllerContext;
        storeController.TempData = _controller.TempData;

        _merchandiseReturnServiceMock.Setup(s => s.GetMerchandiseReturnById("missing")).ReturnsAsync((MerchandiseReturn)null);

        var result = await storeController.MerchandiseReturnNotesSelect("missing", new DataSourceRequest());

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
    }
}
