using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseCustomerOutOfStockSubscriptionControllerTests
    : CustomerControllerTestsBase<BaseCustomerOutOfStockSubscriptionControllerTests.TestCustomerOutOfStockSubscriptionController>
{
    public class TestCustomerOutOfStockSubscriptionController(
        ICustomerService customerService,
        ICustomerOutOfStockSubscriptionViewModelService customerOutOfStockSubscriptionViewModelService,
        IAdminDataScope<Customer> scope)
        : BaseCustomerOutOfStockSubscriptionController(customerService, customerOutOfStockSubscriptionViewModelService,
            scope);

    protected override TestCustomerOutOfStockSubscriptionController CreateController() => new(
        CustomerServiceMock.Object,
        CustomerOutOfStockSubscriptionViewModelServiceMock.Object,
        ScopeMock.Object);

    [TestMethod]
    public async Task OutOfStockSubscriptionList_GlobalScope_NoOwnershipCheck()
    {
        CustomerOutOfStockSubscriptionViewModelServiceMock
            .Setup(v => v.PrepareOutOfStockSubscriptionModel("c1", 1, 10))
            .ReturnsAsync((Enumerable.Empty<CustomerModel.OutOfStockSubscriptionModel>(), 0));

        var result = await Controller.OutOfStockSubscriptionList(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerServiceMock.Verify(s => s.GetCustomerById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task OutOfStockSubscriptionList_StoreScope_DeniedOwnership_ReturnsEmpty()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync((Customer)null);

        var result = await Controller.OutOfStockSubscriptionList(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        var data = (DataSourceResult)((JsonResult)result).Value!;
        Assert.AreEqual(0, data.Total);
        CustomerOutOfStockSubscriptionViewModelServiceMock.Verify(
            v => v.PrepareOutOfStockSubscriptionModel(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}
