using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
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
public class BaseCustomerProductControllerTests
    : CustomerControllerTestsBase<BaseCustomerProductControllerTests.TestCustomerProductController>
{
    public class TestCustomerProductController(
        ICustomerService customerService,
        ICustomerProductViewModelService customerProductViewModelService,
        ICustomerProductService customerProductService,
        IAdminDataScope<Customer> scope)
        : BaseCustomerProductController(customerService, customerProductViewModelService, customerProductService, scope);

    protected override TestCustomerProductController CreateController() => new(
        CustomerServiceMock.Object,
        CustomerProductViewModelServiceMock.Object,
        CustomerProductServiceMock.Object,
        ScopeMock.Object);

    [TestMethod]
    public async Task DeleteProductPrice_GlobalScope_NoOwnershipCheck_CallsServiceDirectly()
    {
        CustomerProductViewModelServiceMock.Setup(v => v.DeleteProductPrice("pp1")).Returns(Task.CompletedTask);

        var result = await Controller.DeleteProductPrice("pp1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerProductViewModelServiceMock.Verify(v => v.DeleteProductPrice("pp1"), Times.Once);
    }

    [TestMethod]
    public async Task DeleteProductPrice_StoreScope_PriceNotFound_NoOpsSilently()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerProductServiceMock.Setup(s => s.GetCustomerProductPriceById("pp1"))
            .ReturnsAsync((CustomerProductPrice)null);

        var result = await Controller.DeleteProductPrice("pp1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerProductViewModelServiceMock.Verify(v => v.DeleteProductPrice(It.IsAny<string>()), Times.Never);
        CustomerProductServiceMock.Verify(s => s.DeleteCustomerProductPrice(It.IsAny<CustomerProductPrice>()), Times.Never);
    }

    [TestMethod]
    public async Task DeleteProductPrice_StoreScope_DeniedOwnership_NoOpsSilently()
    {
        // Price exists but belongs to a customer outside the caller's store — exercises the
        // ownership-denial half of the guard, distinct from the not-found half above.
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        var price = new CustomerProductPrice { Id = "pp1", CustomerId = "c1" };
        CustomerProductServiceMock.Setup(s => s.GetCustomerProductPriceById("pp1")).ReturnsAsync(price);
        var foreignCustomer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(foreignCustomer);
        ScopeMock.Setup(s => s.HasAccess(foreignCustomer)).ReturnsAsync(false);

        var result = await Controller.DeleteProductPrice("pp1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerProductViewModelServiceMock.Verify(v => v.DeleteProductPrice(It.IsAny<string>()), Times.Never);
        CustomerProductServiceMock.Verify(s => s.DeleteCustomerProductPrice(It.IsAny<CustomerProductPrice>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductAddPopup_Get_StoreScope_Denied_RedirectsToCustomerList()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync((Customer)null);

        var result = await Controller.ProductAddPopup("c1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        Assert.AreEqual("Customer", redirect.ControllerName);
    }

    [TestMethod]
    public async Task ProductsPrice_StoreScope_DeniedOwnership_ReturnsEmpty()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync((Customer)null);

        var result = await Controller.ProductsPrice(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        var data = (DataSourceResult)((JsonResult)result).Value!;
        Assert.AreEqual(0, data.Total);
        CustomerProductViewModelServiceMock.Verify(
            v => v.PrepareProductPriceModel(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task ProductsPrice_GlobalScope_NoOwnershipCheck_CallsServiceDirectly()
    {
        CustomerProductViewModelServiceMock.Setup(v => v.PrepareProductPriceModel("c1", 1, 10))
            .ReturnsAsync((Enumerable.Empty<CustomerModel.ProductPriceModel>(), 0));

        var result = await Controller.ProductsPrice(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerServiceMock.Verify(s => s.GetCustomerById(It.IsAny<string>()), Times.Never);
        CustomerProductViewModelServiceMock.Verify(v => v.PrepareProductPriceModel("c1", 1, 10), Times.Once);
    }

    [TestMethod]
    public async Task PersonalizedProducts_StoreScope_DeniedOwnership_ReturnsEmpty()
    {
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync((Customer)null);

        var result = await Controller.PersonalizedProducts(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        var data = (DataSourceResult)((JsonResult)result).Value!;
        Assert.AreEqual(0, data.Total);
        CustomerProductViewModelServiceMock.Verify(
            v => v.PreparePersonalizedProducts(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task PersonalizedProducts_GlobalScope_NoOwnershipCheck_CallsServiceDirectly()
    {
        CustomerProductViewModelServiceMock.Setup(v => v.PreparePersonalizedProducts("c1", 1, 10))
            .ReturnsAsync((Enumerable.Empty<CustomerModel.ProductModel>(), 0));

        var result = await Controller.PersonalizedProducts(new DataSourceRequest { Page = 1, PageSize = 10 }, "c1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerServiceMock.Verify(s => s.GetCustomerById(It.IsAny<string>()), Times.Never);
        CustomerProductViewModelServiceMock.Verify(v => v.PreparePersonalizedProducts("c1", 1, 10), Times.Once);
    }
}
