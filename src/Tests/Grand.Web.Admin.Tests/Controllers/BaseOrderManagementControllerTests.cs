using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Mediator;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Web.Common.Audit;
using Moq;
using Grand.Web.Common.Localization;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseOrderManagementControllerTests
{
    private class TestOrderManagementController(
        IOrderViewModelService orderViewModelService,
        IOrderService orderService,
        IOrderStatusService orderStatusService,
        IRequestTranslationService translationService,
        IContextAccessor contextAccessor,
        IPdfService pdfService,
        IMediator mediator,
        IAdminDataScope<Order> scope,
        IOrderNoteViewModelService orderNoteViewModelService,
        IOrderAddressViewModelService orderAddressViewModelService,
        IOrderProductViewModelService orderProductViewModelService)
        : BaseOrderManagementController(orderViewModelService, orderService, orderStatusService,
            translationService, contextAccessor, pdfService, mediator, scope, orderNoteViewModelService,
            orderAddressViewModelService, orderProductViewModelService);

    private TestOrderManagementController _controller;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IOrderNoteViewModelService> _orderNoteViewModelServiceMock;
    private Mock<IOrderProductViewModelService> _orderProductViewModelServiceMock;
    private Mock<IOrderAddressViewModelService> _orderAddressViewModelServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IAdminDataScope<Order>> _scopeMock;
    private Mock<IOrderStatusService> _orderStatusServiceMock;
    private Mock<IRequestTranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _orderNoteViewModelServiceMock = new Mock<IOrderNoteViewModelService>();
        _orderProductViewModelServiceMock = new Mock<IOrderProductViewModelService>();
        _orderAddressViewModelServiceMock = new Mock<IOrderAddressViewModelService>();
        _mediatorMock = new Mock<IMediator>();
        _scopeMock = new Mock<IAdminDataScope<Order>>();

        _translationServiceMock = new Mock<IRequestTranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");
        _orderStatusServiceMock = new Mock<IOrderStatusService>();
        _orderStatusServiceMock.Setup(s => s.GetByStatusId(It.IsAny<int>()))
            .ReturnsAsync((int statusId) => new OrderStatus { StatusId = statusId, Name = "status" });

        _controller = new TestOrderManagementController(
            new Mock<IOrderViewModelService>().Object, _orderServiceMock.Object,
            _orderStatusServiceMock.Object, _translationServiceMock.Object,
            new Mock<IContextAccessor>().Object, new Mock<IPdfService>().Object,
            _mediatorMock.Object, _scopeMock.Object, _orderNoteViewModelServiceMock.Object,
            _orderAddressViewModelServiceMock.Object, _orderProductViewModelServiceMock.Object);

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
    public async Task CancelOrder_ScopeDenies_RedirectsToList_NoCommandSent()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.CancelOrder("o1");

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<CancelOrderCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task CancelOrder_Authorized_SendsCancelCommand_RedirectsToEdit()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        _mediatorMock.Setup(m => m.Send(It.IsAny<CanCancelOrderQuery>(), default)).ReturnsAsync(true);

        var result = await _controller.CancelOrder("o1");

        var redirect = result as RedirectToActionResult;
        Assert.AreEqual("Edit", redirect?.ActionName);
        _mediatorMock.Verify(m => m.Send(
            It.Is<CancelOrderCommand>(c => c.Order == order && c.NotifyCustomer), default), Times.Once);
    }

    [TestMethod]
    public async Task CancelOrder_NotCancellable_RedirectsToEdit_NoCommandSent()
    {
        //only a pending order may be cancelled; the action no longer trusts the button being hidden
        var order = new Order { Id = "o1", OrderStatusId = (int)OrderStatusSystem.Complete };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        _mediatorMock.Setup(m => m.Send(It.IsAny<CanCancelOrderQuery>(), default)).ReturnsAsync(false);

        var result = await _controller.CancelOrder("o1");

        Assert.AreEqual("Edit", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<CancelOrderCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task ChangeOrderStatus_ScopeDenies_RedirectsToList_NoUpdate()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.ChangeOrderStatus("o1", new OrderModel { OrderStatusId = 30 });

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _orderServiceMock.Verify(s => s.UpdateOrder(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangeOrderStatus_OrderAwaitingApproval_RefusesWithoutUpdate()
    {
        var order = new Order { Id = "o1", OrderStatusId = (int)OrderStatusSystem.AwaitingApproval };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var result = await _controller.ChangeOrderStatus("o1",
            new OrderModel { OrderStatusId = (int)OrderStatusSystem.Processing });

        Assert.AreEqual("Edit", (result as RedirectToActionResult)?.ActionName);
        Assert.AreEqual((int)OrderStatusSystem.AwaitingApproval, order.OrderStatusId);
        _translationServiceMock.Verify(
            t => t.GetResource("Admin.Orders.Fields.OrderStatus.Change.AwaitingApproval"), Times.Once);
        _orderServiceMock.Verify(s => s.UpdateOrder(It.IsAny<Order>()), Times.Never);
        _orderServiceMock.Verify(s => s.InsertOrderNote(It.IsAny<OrderNote>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangeOrderStatus_ToAwaitingApproval_RefusesWithoutUpdate()
    {
        var order = new Order { Id = "o1", OrderStatusId = (int)OrderStatusSystem.Pending };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var result = await _controller.ChangeOrderStatus("o1",
            new OrderModel { OrderStatusId = (int)OrderStatusSystem.AwaitingApproval });

        Assert.AreEqual("Edit", (result as RedirectToActionResult)?.ActionName);
        Assert.AreEqual((int)OrderStatusSystem.Pending, order.OrderStatusId);
        _translationServiceMock.Verify(
            t => t.GetResource("Admin.Orders.Fields.OrderStatus.Change.ToAwaitingApproval"), Times.Once);
        _orderServiceMock.Verify(s => s.UpdateOrder(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangeOrderStatus_OrdinaryChange_UpdatesOrder()
    {
        var order = new Order { Id = "o1", OrderStatusId = (int)OrderStatusSystem.Pending };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var result = await _controller.ChangeOrderStatus("o1",
            new OrderModel { OrderStatusId = (int)OrderStatusSystem.Processing });

        Assert.AreEqual("Edit", (result as RedirectToActionResult)?.ActionName);
        Assert.AreEqual((int)OrderStatusSystem.Processing, order.OrderStatusId);
        _orderServiceMock.Verify(s => s.UpdateOrder(order), Times.Once);
    }

    [TestMethod]
    public async Task Delete_ScopeDenies_RedirectsToList_NoDeleteCommand()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.Delete(new OrderDeleteModel("o1"));

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<DeleteOrderCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task Delete_Authorized_ValidModel_SendsDeleteCommand()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var result = await _controller.Delete(new OrderDeleteModel("o1"));

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(
            It.Is<DeleteOrderCommand>(c => c.Order == order), default), Times.Once);
    }

    [TestMethod]
    public async Task EditOrderTotals_ScopeDenies_RedirectsToList_NoUpdate()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.EditOrderTotals("o1", new OrderModel());

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _orderServiceMock.Verify(s => s.UpdateOrder(It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task EditOrderTotals_Authorized_UpdatesOrderAndInsertsNote()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var model = new OrderModel { OrderTotalValue = 99.0, CurrencyRate = 1.0 };
        var result = await _controller.EditOrderTotals("o1", model);

        Assert.AreEqual("o1", (result as RedirectToActionResult)?.RouteValues["id"]);
        Assert.AreEqual(99.0, order.OrderTotal);
        _orderServiceMock.Verify(s => s.InsertOrderNote(It.Is<OrderNote>(n => n.Note == "Order totals have been edited")), Times.Once);
    }

    [TestMethod]
    public async Task SaveOrderItem_ScopeDenies_RedirectsToList_NoCommandSent()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.SaveOrderItem("o1", new OrderItemsModel(new List<OrderItemModel>(), "i1"));

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateOrderItemCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task SaveOrderItem_OrderCancelled_ErrorsWithoutSendingCommand()
    {
        var order = new Order { Id = "o1", OrderStatusId = (int)OrderStatusSystem.Cancelled };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var result = await _controller.SaveOrderItem("o1", new OrderItemsModel(new List<OrderItemModel>(), "i1"));

        Assert.AreEqual("Edit", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateOrderItemCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task DeleteOrderItem_ScopeDenies_RedirectsToList_NoCommandSent()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.DeleteOrderItem("o1", "i1");

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<DeleteOrderItemCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task UploadLicenseFilePopupGet_ScopeDenies_RedirectsToList()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);
        var productServiceMock = new Mock<Grand.Business.Core.Interfaces.Catalog.Products.IProductService>();

        var result = await _controller.UploadLicenseFilePopup("o1", "i1", productServiceMock.Object);

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
    }

    [TestMethod]
    public async Task DeleteLicenseFilePopup_Authorized_ClearsLicenseAndRedirectsToEdit()
    {
        var orderItem = new OrderItem { Id = "i1", LicenseDownloadId = "dl-1" };
        var order = new Order { Id = "o1" };
        order.OrderItems.Add(orderItem);
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var result = await _controller.DeleteLicenseFilePopup(
            new OrderModel.UploadLicenseModel { OrderId = "o1", OrderItemId = "i1" });

        Assert.IsNull(orderItem.LicenseDownloadId);
        Assert.AreEqual("Edit", (result as RedirectToActionResult)?.ActionName);
    }

    [TestMethod]
    public async Task AddProductToOrder_ScopeDenies_RedirectsToList()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.AddProductToOrder("o1");

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
    }

    [TestMethod]
    public async Task AddProductToOrderDetailsPost_NoWarnings_RedirectsToEdit()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        _orderProductViewModelServiceMock
            .Setup(v => v.AddProductToOrderDetails(order, It.IsAny<Product>(), It.IsAny<AddProductToOrderModel>()))
            .ReturnsAsync(new List<string>());

        var (productService, aclService) = ProductInScope("p1", true);

        var result = await _controller.AddProductToOrderDetails(
            new AddProductToOrderModel("o1", "p1", 0, 0, 1, 0), productService, aclService);

        var redirect = result as RedirectToActionResult;
        Assert.AreEqual("Edit", redirect?.ActionName);
        Assert.AreEqual("o1", redirect?.RouteValues["id"]);
    }

    [TestMethod]
    public async Task AddProductToOrderDetailsPost_ProductOutsideScope_RedirectsBack_NothingAdded()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        var (productService, aclService) = ProductInScope("p1", false);

        var result = await _controller.AddProductToOrderDetails(
            new AddProductToOrderModel("o1", "p1", 0, 0, 1, 0), productService, aclService);

        var redirect = result as RedirectToActionResult;
        Assert.AreEqual("AddProductToOrder", redirect?.ActionName);
        Assert.AreEqual("o1", redirect?.RouteValues["orderId"]);
        _orderProductViewModelServiceMock.Verify(v => v.AddProductToOrderDetails(It.IsAny<Order>(),
            It.IsAny<Product>(), It.IsAny<AddProductToOrderModel>()), Times.Never);
    }

    [TestMethod]
    public async Task AddProductToOrderDetailsGet_ProductOutsideScope_RedirectsBack()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        var (productService, aclService) = ProductInScope("p1", false);

        var result = await _controller.AddProductToOrderDetails("o1", "p1", productService, aclService);

        Assert.AreEqual("AddProductToOrder", (result as RedirectToActionResult)?.ActionName);
        _orderProductViewModelServiceMock.Verify(v => v.PrepareAddProductToOrderModel(It.IsAny<Order>(), It.IsAny<Product>()),
            Times.Never);
    }

    [TestMethod]
    public async Task AddProductToOrderDetailsGet_ChecksProductAgainstOrderStore()
    {
        //Main Admin has no panel store, so the order's store is the only one to check against
        var order = new Order { Id = "o1", StoreId = "s1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        var product = new Product { Id = "p1" };
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductById("p1", It.IsAny<bool>())).ReturnsAsync(product);
        var aclService = new Mock<IAclService>();
        aclService.Setup(s => s.Authorize(product, "s1")).Returns(true);

        var result = await _controller.AddProductToOrderDetails("o1", "p1", productService.Object, aclService.Object);

        Assert.IsInstanceOfType<ViewResult>(result);
        aclService.Verify(s => s.Authorize(product, "s1"), Times.Once);
        _orderProductViewModelServiceMock.Verify(v => v.PrepareAddProductToOrderModel(order, product), Times.Once);
    }

    [TestMethod]
    public async Task AddProductToOrderSearch_SearchesTheOrdersStore()
    {
        var order = new Order { Id = "o1", StoreId = "s1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        var productService = new Mock<IProductService>();
        //answers only a search limited to the order's store
        productService.Setup(s => s.SearchProducts(It.Is<CatalogAccess>(a => a.StoreId == "s1"),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IList<string>>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ProductType?>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<double?>(),
                It.IsAny<double?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<IList<string>>(),
                It.IsAny<ProductSortingEnum>(), It.IsAny<bool?>()))
            .ReturnsAsync((new PagedList<Product>(new List<Product> { new() { Id = "p1" } }, 0, 10), new List<string>()));

        var result = await _controller.AddProductToOrder(
            new Grand.Web.Common.DataSource.DataSourceRequest { Page = 1, PageSize = 10 },
            new OrderModel.AddOrderProductModel { OrderId = "o1" }, productService.Object);

        Assert.AreEqual(1, ((result as JsonResult)?.Value as Grand.Web.Common.DataSource.DataSourceResult)?.Total);
    }

    [TestMethod]
    public async Task AddProductToOrderSearch_ScopeDenies_ReturnsGridError()
    {
        var order = new Order { Id = "o1", StoreId = "s1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.AddProductToOrder(new Grand.Web.Common.DataSource.DataSourceRequest(),
            new OrderModel.AddOrderProductModel { OrderId = "o1" }, new Mock<IProductService>().Object);

        Assert.IsNotNull(GridErrors(result));
    }

    [TestMethod]
    public async Task OrderNoteAdd_RecordedOnTheOrder()
    {
        var order = new Order { Id = "o1", OrderNumber = 7 };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        _orderNoteViewModelServiceMock.Setup(v => v.InsertOrderNote(order, null, false, "msg")).ReturnsAsync(true);
        var recorder = new Mock<IAuditEventRecorder>();

        await _controller.OrderNoteAdd("o1", null, false, "msg", recorder.Object);

        recorder.Verify(r => r.OwnedChange("Order", "o1", "7", "OrderNote", null, "msg"), Times.Once);
    }

    [TestMethod]
    public async Task OrderNoteAdd_FileRefused_ReturnsResultFalseWithMessage()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        _orderNoteViewModelServiceMock.Setup(v => v.InsertOrderNote(order, "d1", false, "msg")).ReturnsAsync(false);

        var result = await _controller.OrderNoteAdd("o1", "d1", false, "msg", new Mock<IAuditEventRecorder>().Object);

        var value = (result as JsonResult)?.Value;
        Assert.IsFalse((bool)value!.GetType().GetProperty("Result")!.GetValue(value)!);
        Assert.AreEqual("resource", value.GetType().GetProperty("Message")!.GetValue(value));
    }

    private (IProductService, IAclService) ProductInScope(string productId, bool inScope)
    {
        var product = new Product { Id = productId };
        var productService = new Mock<IProductService>();
        productService.Setup(s => s.GetProductById(productId, It.IsAny<bool>())).ReturnsAsync(product);
        var aclService = new Mock<IAclService>();
        aclService.Setup(s => s.Authorize(product, It.IsAny<string>())).Returns(inScope);
        return (productService.Object, aclService.Object);
    }

    [TestMethod]
    public async Task AddressEditGet_ScopeDenies_RedirectsToList()
    {
        var order = new Order { Id = "o1", BillingAddress = new Grand.Domain.Common.Address { Id = "a1" } };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.AddressEdit("a1", "o1", true);

        Assert.AreEqual("List", (result as RedirectToActionResult)?.ActionName);
    }

    [TestMethod]
    public async Task AddressEditGet_NoSuchAddressOnOrder_RedirectsToEdit()
    {
        var order = new Order { Id = "o1", BillingAddress = new Grand.Domain.Common.Address { Id = "a1" } };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);

        var result = await _controller.AddressEdit("a2", "o1", false);

        Assert.AreEqual("Edit", (result as RedirectToActionResult)?.ActionName);
    }

    [TestMethod]
    public async Task AddressEditGet_StaleAddressId_EditsTheOrdersAddress()
    {
        //an address stored without _id gets a new id on every load, so the id in the link can't be relied on
        var order = new Order { Id = "o1", BillingAddress = new Grand.Domain.Common.Address { Id = "a1" } };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        _orderAddressViewModelServiceMock.Setup(v => v.PrepareOrderAddressModel(order, order.BillingAddress))
            .ReturnsAsync(new OrderAddressModel());

        var result = await _controller.AddressEdit("stale", "o1", true);

        Assert.IsTrue(((result as ViewResult)?.Model as OrderAddressModel)?.BillingAddress);
    }

    [TestMethod]
    public async Task AddressEditPost_InvalidModel_ShowsTypedValues_NothingSaved()
    {
        AutoMapperConfig.Init(new MapperConfiguration(cfg => cfg.AddProfile<AddressProfile>()));
        var order = new Order { Id = "o1", BillingAddress = new Grand.Domain.Common.Address { Id = "a1", City = "Saved" } };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        Grand.Domain.Common.Address shown = null;
        _orderAddressViewModelServiceMock
            .Setup(v => v.PrepareOrderAddressModel(order, It.IsAny<Grand.Domain.Common.Address>()))
            .Callback<Order, Grand.Domain.Common.Address>((_, a) => shown = a)
            .ReturnsAsync(new OrderAddressModel());
        var addressAttributeService = new Mock<Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeService>();
        addressAttributeService.Setup(s => s.GetAllAddressAttributes())
            .ReturnsAsync(new List<Grand.Domain.Common.AddressAttribute>());
        _controller.ModelState.AddModelError("Address.FirstName", "required");
        var model = new OrderAddressModel {
            OrderId = "o1", BillingAddress = true,
            Address = new Grand.Web.AdminShared.Models.Common.AddressModel { Id = "a1", City = "Typed" }
        };

        var result = await _controller.AddressEdit(model, addressAttributeService.Object,
            new Mock<Grand.Business.Core.Interfaces.Common.Addresses.IAddressAttributeParser>().Object);

        Assert.AreEqual("Typed", shown?.City);
        Assert.AreEqual("Saved", order.BillingAddress.City);
        Assert.IsTrue(((result as ViewResult)?.Model as OrderAddressModel)?.BillingAddress);
        _orderAddressViewModelServiceMock.Verify(v => v.UpdateOrderAddress(It.IsAny<Order>(),
            It.IsAny<Grand.Domain.Common.Address>(), It.IsAny<OrderAddressModel>(),
            It.IsAny<List<Grand.Domain.Common.CustomAttribute>>()), Times.Never);
    }

    [TestMethod]
    public async Task OrderNotesSelect_NotFound_ThrowsArgumentException()
    {
        _orderServiceMock.Setup(s => s.GetOrderById("missing")).ReturnsAsync((Order)null);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => _controller.OrderNotesSelect("missing", new Grand.Web.Common.DataSource.DataSourceRequest()));
    }

    [TestMethod]
    public async Task OrderNotesSelect_ScopeDenies_ReturnsEmptyContent_NoThrow()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.OrderNotesSelect("o1", new Grand.Web.Common.DataSource.DataSourceRequest());

        var content = result as ContentResult;
        Assert.IsNotNull(content);
        Assert.AreEqual("", content.Content);
    }

    [TestMethod]
    public async Task OrderNoteAdd_ScopeDenies_ReturnsJsonResultFalse()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.OrderNoteAdd("o1", null, false, "msg", new Mock<IAuditEventRecorder>().Object);

        var json = result as JsonResult;
        Assert.IsNotNull(json);
        Assert.IsFalse((bool)json.Value.GetType().GetProperty("Result").GetValue(json.Value));
        _orderNoteViewModelServiceMock.Verify(v => v.InsertOrderNote(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }
    private static object GridErrors(IActionResult result) =>
        ((result as JsonResult)?.Value as Grand.Web.Common.DataSource.DataSourceResult)?.Errors;

    private Order OrderWithItem(OrderItem item, int status = (int)OrderStatusSystem.Pending)
    {
        var order = new Order { Id = "o1", OrderStatusId = status };
        order.OrderItems.Add(item);
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(true);
        return order;
    }

    [TestMethod]
    public async Task OrderItemUpdate_ScopeDenies_ReturnsGridError_NoCommandSent()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.OrderItemUpdate("o1", new OrderModel.OrderItemModel { Id = "i1", Quantity = 2 });

        Assert.IsNotNull(GridErrors(result));
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateOrderItemCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task OrderItemUpdate_OrderCancelled_ReturnsGridError_NoCommandSent()
    {
        OrderWithItem(new OrderItem { Id = "i1", Quantity = 1, OpenQty = 1 }, (int)OrderStatusSystem.Cancelled);

        var result = await _controller.OrderItemUpdate("o1", new OrderModel.OrderItemModel { Id = "i1", Quantity = 2 });

        Assert.IsNotNull(GridErrors(result));
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateOrderItemCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task OrderItemUpdate_PartlyShipped_RefusesQuantityChange()
    {
        OrderWithItem(new OrderItem { Id = "i1", Quantity = 3, OpenQty = 1, IsShipEnabled = true, UnitPriceExclTax = 10 });

        var result = await _controller.OrderItemUpdate("o1",
            new OrderModel.OrderItemModel { Id = "i1", Quantity = 2, UnitPriceExclTaxValue = 10 });

        Assert.AreEqual("You can't change quantity", GridErrors(result));
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateOrderItemCommand>(), default), Times.Never);
    }

    //the products grid offers Edit on such an item (edit-visible-if "!IsShipEnabled || ...")
    [TestMethod]
    public async Task OrderItemUpdate_FulfilledItemThatShipsNothing_IsAccepted()
    {
        var item = new OrderItem { Id = "i1", Quantity = 2, OpenQty = 0, IsShipEnabled = false, UnitPriceExclTax = 10 };
        OrderWithItem(item);

        var result = await _controller.OrderItemUpdate("o1",
            new OrderModel.OrderItemModel { Id = "i1", Quantity = 2, UnitPriceExclTaxValue = 12 });

        Assert.IsNull(GridErrors(result));
        Assert.AreEqual(12, item.UnitPriceExclTax);
        _mediatorMock.Verify(m => m.Send(It.Is<UpdateOrderItemCommand>(c => c.OrderItem == item), default), Times.Once);
    }

    [TestMethod]
    public async Task OrderItemUpdate_NothingChanged_ReturnsGridError()
    {
        OrderWithItem(new OrderItem { Id = "i1", Quantity = 2, OpenQty = 2, UnitPriceExclTax = 10 });

        var result = await _controller.OrderItemUpdate("o1",
            new OrderModel.OrderItemModel { Id = "i1", Quantity = 2, UnitPriceExclTaxValue = 10 });

        Assert.AreEqual("Nothing has been changed", GridErrors(result));
    }

    [TestMethod]
    public async Task OrderItemUpdate_NewPrice_RecalculatesOnServerAndSendsCommand()
    {
        var item = new OrderItem { Id = "i1", Quantity = 1, OpenQty = 1, UnitPriceExclTax = 10, UnitPriceInclTax = 12.3, TaxRate = 23, DiscountAmountExclTax = 1 };
        OrderWithItem(item);

        var result = await _controller.OrderItemUpdate("o1",
            new OrderModel.OrderItemModel { Id = "i1", Quantity = 2, UnitPriceExclTaxValue = 20 });

        Assert.IsNull(GridErrors(result));
        Assert.IsNotNull(result as JsonResult);
        Assert.AreEqual(2, item.Quantity);
        Assert.AreEqual(2, item.OpenQty);
        Assert.AreEqual(24.6, item.UnitPriceInclTax, 0.0001);
        Assert.AreEqual(49.2, item.PriceInclTax, 0.0001);
        Assert.AreEqual(40, item.PriceExclTax, 0.0001);
        Assert.AreEqual(0, item.DiscountAmountExclTax);
        _mediatorMock.Verify(m => m.Send(It.Is<UpdateOrderItemCommand>(c => c.OrderItem == item), default), Times.Once);
    }

    [TestMethod]
    public async Task OrderItemDelete_CommandRefuses_ReturnsItsMessage()
    {
        OrderWithItem(new OrderItem { Id = "i1", Quantity = 1, OpenQty = 1 });
        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteOrderItemCommand>(), default))
            .ReturnsAsync((true, "You can't delete this order item."));

        var result = await _controller.OrderItemDelete("o1", new OrderModel.OrderItemModel { Id = "i1" });

        Assert.AreEqual("You can't delete this order item.", GridErrors(result));
    }

    [TestMethod]
    public async Task OrderItemDelete_UnknownItem_ReturnsGridError_NoCommandSent()
    {
        OrderWithItem(new OrderItem { Id = "i1" });

        var result = await _controller.OrderItemDelete("o1", new OrderModel.OrderItemModel { Id = "other" });

        Assert.IsNotNull(GridErrors(result));
        _mediatorMock.Verify(m => m.Send(It.IsAny<DeleteOrderItemCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task OrderItemCancel_ScopeDenies_ReturnsGridError_NoCommandSent()
    {
        var order = new Order { Id = "o1" };
        _orderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(order);
        _scopeMock.Setup(s => s.HasAccess(order)).ReturnsAsync(false);

        var result = await _controller.OrderItemCancel("o1", "i1");

        Assert.IsNotNull(GridErrors(result));
        _mediatorMock.Verify(m => m.Send(It.IsAny<CancelOrderItemCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task OrderItemCancel_Success_ReturnsEmptyResult()
    {
        var item = new OrderItem { Id = "i1", Quantity = 1, OpenQty = 1 };
        OrderWithItem(item);
        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelOrderItemCommand>(), default)).ReturnsAsync((false, ""));

        var result = await _controller.OrderItemCancel("o1", "i1");

        Assert.IsNotNull(result as JsonResult);
        Assert.IsNull(GridErrors(result));
        _mediatorMock.Verify(m => m.Send(It.Is<CancelOrderItemCommand>(c => c.OrderItem == item), default), Times.Once);
    }
}
