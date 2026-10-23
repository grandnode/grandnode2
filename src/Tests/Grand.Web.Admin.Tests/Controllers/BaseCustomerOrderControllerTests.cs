using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseCustomerOrderControllerTests
    : CustomerControllerTestsBase<BaseCustomerOrderControllerTests.TestCustomerOrderController>
{
    public class TestCustomerOrderController(
        ICustomerService customerService,
        IOrderService orderService,
        IOrderViewModelService orderViewModelService,
        IRequestPermissionService permissionService,
        IAdminDataScope<Customer> scope)
        : BaseCustomerOrderController(customerService, orderService, orderViewModelService, permissionService, scope);

    protected override TestCustomerOrderController CreateController() => new(
        CustomerServiceMock.Object,
        OrderServiceMock.Object,
        OrderViewModelServiceMock.Object,
        PermissionServiceMock.Object,
        ScopeMock.Object);

    [TestMethod]
    public async Task OrderDetails_StoreScope_MismatchedStore_ReturnsEmptyNotException()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        OrderServiceMock.Setup(s => s.GetOrderById("o1")).ReturnsAsync(new Order { Id = "o1", StoreId = "store-2" });

        var result = await Controller.OrderDetails("o1");

        var data = (DataSourceResult)((JsonResult)result).Value!;
        Assert.AreEqual(0, data.Total);
    }

    [TestMethod]
    public async Task OrderDetails_GlobalScope_NotFound_ThrowsArgumentException()
    {
        PermissionServiceMock.Setup(p => p.Authorize(StandardPermission.ManageOrders)).ReturnsAsync(true);
        OrderServiceMock.Setup(s => s.GetOrderById("missing")).ReturnsAsync((Order)null);

        await Assert.ThrowsAsync<ArgumentException>(() => Controller.OrderDetails("missing"));
    }

    [TestMethod]
    public async Task OrderList_GlobalScope_WithoutManageOrders_ReturnsEmpty()
    {
        PermissionServiceMock.Setup(p => p.Authorize(StandardPermission.ManageOrders)).ReturnsAsync(false);

        var result = await Controller.OrderList("c1", new DataSourceRequest { Page = 1, PageSize = 10 });

        var data = (DataSourceResult)((JsonResult)result).Value!;
        Assert.AreEqual(0, data.Total);
        OrderViewModelServiceMock.Verify(
            v => v.PrepareOrderModel(It.IsAny<OrderListModel>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}
