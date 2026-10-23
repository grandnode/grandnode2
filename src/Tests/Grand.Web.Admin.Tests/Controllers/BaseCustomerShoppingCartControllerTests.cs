using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.ShoppingCart;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseCustomerShoppingCartControllerTests
    : CustomerControllerTestsBase<BaseCustomerShoppingCartControllerTests.TestCustomerShoppingCartController>
{
    public class TestCustomerShoppingCartController(
        ICustomerService customerService,
        ICustomerShoppingCartViewModelService customerShoppingCartViewModelService,
        IAdminDataScope<Customer> scope)
        : BaseCustomerShoppingCartController(customerService, customerShoppingCartViewModelService, scope);

    protected override TestCustomerShoppingCartController CreateController() => new(
        CustomerServiceMock.Object,
        CustomerShoppingCartViewModelServiceMock.Object,
        ScopeMock.Object);

    [TestMethod]
    public async Task GetCartList_Authorized_PassesTheLoadedCustomerToTheService()
    {
        var customer = SetupAuthorizedCustomer("c1");
        CustomerShoppingCartViewModelServiceMock.Setup(v => v.PrepareShoppingCartItemModel(customer, 1))
            .ReturnsAsync(new List<ShoppingCartItemModel> { new() });

        var result = await Controller.GetCartList("c1", 1);

        var data = (DataSourceResult)((JsonResult)result).Value!;
        Assert.AreEqual(1, data.Total);
        CustomerServiceMock.Verify(s => s.GetCustomerById("c1"), Times.Once);
    }

    [TestMethod]
    public async Task GetCartList_GlobalScope_UnknownCustomer_ReturnsEmptyGrid()
    {
        CustomerServiceMock.Setup(s => s.GetCustomerById("missing")).ReturnsAsync((Customer)null);

        var result = await Controller.GetCartList("missing", 1);

        var data = (DataSourceResult)((JsonResult)result).Value!;
        Assert.AreEqual(0, data.Total);
        CustomerShoppingCartViewModelServiceMock.Verify(
            v => v.PrepareShoppingCartItemModel(It.IsAny<Customer>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task GetCartList_StoreScope_DeniedOwnership_ReturnsEmptyGrid()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(false);

        var result = await Controller.GetCartList("c1", 1);

        var data = (DataSourceResult)((JsonResult)result).Value!;
        Assert.AreEqual(0, data.Total);
        CustomerShoppingCartViewModelServiceMock.Verify(
            v => v.PrepareShoppingCartItemModel(It.IsAny<Customer>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteCart_Denied_ThrowsArgumentException_NothingDeleted()
    {
        CustomerServiceMock.Setup(s => s.GetCustomerById("missing")).ReturnsAsync((Customer)null);

        await Assert.ThrowsAsync<ArgumentException>(() => Controller.DeleteCart("sci1", "missing"));
        CustomerShoppingCartViewModelServiceMock.Verify(v => v.DeleteCart(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
    }
}
