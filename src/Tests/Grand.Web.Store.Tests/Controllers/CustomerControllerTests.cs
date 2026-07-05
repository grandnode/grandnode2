using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Models;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class CustomerControllerTests
{
    private const string StoreId = "store-current";

    private Mock<ICustomerService> _customerServiceMock;
    private Mock<ICustomerViewModelService> _customerViewModelServiceMock;
    private Mock<ICustomerManagerService> _customerManagerServiceMock;
    private Mock<ICustomerAttributeService> _customerAttributeServiceMock;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IContextAccessor> _contextAccessorMock;

    [TestInitialize]
    public void Setup()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _customerViewModelServiceMock = new Mock<ICustomerViewModelService>();
        _customerManagerServiceMock = new Mock<ICustomerManagerService>();
        _customerAttributeServiceMock = new Mock<ICustomerAttributeService>();
        _groupServiceMock = new Mock<IGroupService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = StoreId });
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
    }

    private CustomerController BuildController(bool perStoreEnabled)
    {
        var controller = new CustomerController(
            _customerServiceMock.Object,
            _customerViewModelServiceMock.Object,
            _customerManagerServiceMock.Object,
            new Mock<ICustomerProductService>().Object,
            new Mock<IProductReviewService>().Object,
            new Mock<IProductReviewViewModelService>().Object,
            new Mock<IProductViewModelService>().Object,
            new Mock<ICustomerAttributeParser>().Object,
            _customerAttributeServiceMock.Object,
            new Mock<IAddressAttributeParser>().Object,
            new Mock<IAddressAttributeService>().Object,
            new Mock<IMessageProviderService>().Object,
            _groupServiceMock.Object,
            _translationServiceMock.Object,
            _contextAccessorMock.Object,
            new CustomerSettings(),
            new CustomerConfig { RegisterCustomersPerStore = perStoreEnabled });

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
        return controller;
    }

    [TestMethod]
    public async Task CustomerList_PerStoreDisabled_DoesNotFilterByCurrentStore()
    {
        var controller = BuildController(perStoreEnabled: false);
        _groupServiceMock.Setup(g => g.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered))
            .ReturnsAsync(new CustomerGroup { Id = "registered-group" });
        var capturedStoreId = "";
        _customerViewModelServiceMock.Setup(s => s.PrepareCustomerList(It.IsAny<CustomerListModel>(),
                It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .Callback<CustomerListModel, string[], string[], int, int, string>((_, _, _, _, _, storeId) =>
                capturedStoreId = storeId)
            .ReturnsAsync((new List<CustomerModel>(), 0));

        var result = await controller.CustomerList(new DataSourceRequest { Page = 1, PageSize = 20 }, new CustomerListModel());
        Assert.IsInstanceOfType(result, typeof(JsonResult));
        Assert.AreEqual("", capturedStoreId);
    }

    [TestMethod]
    public void PerStoreDisabled_ReturnsView()
    {
        var controller = BuildController(perStoreEnabled: false);
        var result = controller.PerStoreDisabled();
        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    public async Task Create_ForcesStoreScopedRegisteredOnlyConstraints()
    {
        var controller = BuildController(perStoreEnabled: true);

        _groupServiceMock.Setup(g => g.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered))
            .ReturnsAsync(new CustomerGroup { Id = "registered-group" });
        _customerAttributeServiceMock.Setup(a => a.GetAllCustomerAttributes())
            .ReturnsAsync(new List<CustomerAttribute>());

        CustomerModel captured = null;
        _customerViewModelServiceMock.Setup(s => s.InsertCustomerModel(It.IsAny<CustomerModel>()))
            .Callback<CustomerModel>(m => captured = m)
            .ReturnsAsync(new Customer { Id = "c1", StoreId = StoreId });

        //a malicious payload trying to assign a foreign store/role/ownership
        var model = new CustomerModel {
            Email = "new@customer.com",
            StoreId = "foreign-store",
            Owner = "owner@x.com",
            VendorId = "vendor-1",
            StaffStoreId = "staff-store",
            SeId = "sales-1",
            CustomerGroups = ["administrators-group"],
            SelectedAttributes = new List<CustomAttributeModel>()
        };

        var result = await controller.Create(model, false);

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.IsNotNull(captured);
        Assert.AreEqual(StoreId, captured.StoreId);
        Assert.AreEqual("", captured.Owner);
        Assert.AreEqual("", captured.VendorId);
        Assert.AreEqual("", captured.StaffStoreId);
        Assert.AreEqual("", captured.SeId);
        CollectionAssert.AreEqual(new[] { "registered-group" }, captured.CustomerGroups);
    }

    [TestMethod]
    public async Task Create_PerStoreDisabled_DoesNotOverrideStoreId()
    {
        var controller = BuildController(perStoreEnabled: false);

        _groupServiceMock.Setup(g => g.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered))
            .ReturnsAsync(new CustomerGroup { Id = "registered-group" });
        _customerAttributeServiceMock.Setup(a => a.GetAllCustomerAttributes())
            .ReturnsAsync(new List<CustomerAttribute>());

        CustomerModel captured = null;
        _customerViewModelServiceMock.Setup(s => s.InsertCustomerModel(It.IsAny<CustomerModel>()))
            .Callback<CustomerModel>(m => captured = m)
            .ReturnsAsync(new Customer { Id = "c1", StoreId = "foreign-store" });

        var model = new CustomerModel {
            Email = "new@customer.com",
            StoreId = "foreign-store",
            Owner = "owner@x.com",
            VendorId = "vendor-1",
            StaffStoreId = "staff-store",
            SeId = "sales-1",
            CustomerGroups = ["administrators-group"],
            SelectedAttributes = new List<CustomAttributeModel>()
        };

        var result = await controller.Create(model, false);

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.IsNotNull(captured);
        Assert.AreEqual("foreign-store", captured.StoreId);
        Assert.AreEqual("", captured.Owner);
        Assert.AreEqual("", captured.VendorId);
        Assert.AreEqual("", captured.StaffStoreId);
        Assert.AreEqual("", captured.SeId);
        CollectionAssert.AreEqual(new[] { "registered-group" }, captured.CustomerGroups);
    }
}
