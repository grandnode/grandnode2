using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseCustomerAddressControllerTests
    : CustomerControllerTestsBase<BaseCustomerAddressControllerTests.TestCustomerAddressController>
{
    public class TestCustomerAddressController(
        ICustomerService customerService,
        ICustomerAddressViewModelService customerAddressViewModelService,
        IAddressAttributeParser addressAttributeParser,
        IAddressAttributeService addressAttributeService,
        IRequestTranslationService translationService,
        IAdminDataScope<Customer> scope)
        : BaseCustomerAddressController(customerService, customerAddressViewModelService, addressAttributeParser,
            addressAttributeService, translationService, scope);

    protected override TestCustomerAddressController CreateController() => new(
        CustomerServiceMock.Object,
        CustomerAddressViewModelServiceMock.Object,
        AddressAttributeParserMock.Object,
        AddressAttributeServiceMock.Object,
        TranslationServiceMock.Object,
        ScopeMock.Object);

    [TestMethod]
    public async Task AddressesSelect_Denied_ThrowsArgumentException()
    {
        CustomerServiceMock.Setup(s => s.GetCustomerById("missing")).ReturnsAsync((Customer)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Controller.AddressesSelect("missing", new DataSourceRequest()));
    }

    [TestMethod]
    public async Task AddressCreatePost_Authorized_InsertsAndRedirectsToAddressEdit()
    {
        var customer = SetupAuthorizedCustomer("c1");
        var model = new CustomerAddressModel { CustomerId = "c1", Address = new AddressModel() };
        CustomerAddressViewModelServiceMock
            .Setup(v => v.InsertAddressModel(customer, model, It.IsAny<List<CustomAttribute>>()))
            .ReturnsAsync(new Address { Id = "a1" });

        var result = await Controller.AddressCreate(model);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("AddressEdit", redirect.ActionName);
        Assert.IsNull(redirect.ControllerName);
    }

    [TestMethod]
    public async Task AddressCreateGet_Denied_RedirectsToCustomerList()
    {
        CustomerServiceMock.Setup(s => s.GetCustomerById("missing")).ReturnsAsync((Customer)null);

        var result = await Controller.AddressCreate("missing");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("List", redirect.ActionName);
        Assert.AreEqual("Customer", redirect.ControllerName);
    }

    [TestMethod]
    public async Task AddressEditGet_UnknownAddress_RedirectsToCustomerEdit()
    {
        SetupAuthorizedCustomer("c1");

        var result = await Controller.AddressEdit("missing-address", "c1");

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Edit", redirect.ActionName);
        Assert.AreEqual("Customer", redirect.ControllerName);
        Assert.AreEqual("c1", redirect.RouteValues!["id"]);
    }
}
