using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class AdminPaymentTransactionDataScopeTests
{
    private static readonly Guid OrderGuid = Guid.NewGuid();

    private static AdminPaymentTransactionDataScope Build(bool isSalesManager, string currentCustomerSeId,
        Order order)
    {
        var customer = new Customer { SeId = currentCustomerSeId };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(customer);
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        var groupServiceMock = new Mock<IGroupService>();
        groupServiceMock.Setup(g => g.IsSalesManager(customer)).ReturnsAsync(isSalesManager);
        var orderServiceMock = new Mock<IOrderService>();
        orderServiceMock.Setup(o => o.GetOrderByGuid(OrderGuid)).ReturnsAsync(order);

        return new AdminPaymentTransactionDataScope(contextAccessorMock.Object, groupServiceMock.Object,
            orderServiceMock.Object);
    }

    [TestMethod]
    public async Task HasAccess_NotSalesManager_TrueRegardlessOfTheOrder()
    {
        var scope = Build(isSalesManager: false, currentCustomerSeId: "se-1", order: new Order { SeId = "se-2" });

        Assert.IsTrue(await scope.HasAccess(new PaymentTransaction { OrderGuid = OrderGuid }));
    }

    [TestMethod]
    public async Task HasAccess_SalesManager_TheirOrder_True()
    {
        var scope = Build(isSalesManager: true, currentCustomerSeId: "se-1", order: new Order { SeId = "se-1" });

        Assert.IsTrue(await scope.HasAccess(new PaymentTransaction { OrderGuid = OrderGuid }));
    }

    [TestMethod]
    public async Task HasAccess_SalesManager_OrderOfAnotherSalesEmployee_False()
    {
        var scope = Build(isSalesManager: true, currentCustomerSeId: "se-1", order: new Order { SeId = "se-2" });

        Assert.IsFalse(await scope.HasAccess(new PaymentTransaction { OrderGuid = OrderGuid }));
    }

    [TestMethod]
    public async Task HasAccess_SalesManager_NoOrder_False()
    {
        var scope = Build(isSalesManager: true, currentCustomerSeId: "se-1", order: null);

        Assert.IsFalse(await scope.HasAccess(new PaymentTransaction { OrderGuid = OrderGuid }));
    }

    [TestMethod]
    public async Task HasAccess_NullEntity_False()
    {
        var scope = Build(isSalesManager: false, currentCustomerSeId: "se-1", order: null);

        Assert.IsFalse(await scope.HasAccess(null));
    }
}