using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CustomerViewModelServiceTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private CustomerViewModelService _customerViewModelService;

    [TestInitialize]
    public void Setup()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _groupServiceMock = new Mock<IGroupService>();
        _storeServiceMock = new Mock<IStoreService>();

        _customerServiceMock.Setup(c => c.InsertCustomer(It.IsAny<Customer>())).Returns(Task.CompletedTask);
        _customerServiceMock.Setup(c => c.UpdateCustomerInAdminPanel(It.IsAny<Customer>())).Returns(Task.CompletedTask);
        _customerServiceMock
            .Setup(c => c.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _customerServiceMock
            .Setup(c => c.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Grand.Domain.Stores.Store>());

        _groupServiceMock
            .Setup(g => g.GetAllCustomerGroups(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<CustomerGroup>());

        _customerViewModelService = new CustomerViewModelService(
            _customerServiceMock.Object,
            _groupServiceMock.Object,
            new Mock<ICustomerProductService>().Object,
            new Mock<INewsLetterSubscriptionService>().Object,
            new Mock<IDateTimeService>().Object,
            new Mock<ITranslationService>().Object,
            new Mock<ILoyaltyPointsService>().Object,
            new Mock<ICountryService>().Object,
            new Mock<IContextAccessor>().Object,
            new Mock<IVendorService>().Object,
            _storeServiceMock.Object,
            new Mock<ICustomerAttributeParser>().Object,
            new Mock<ICustomerAttributeService>().Object,
            new Mock<IAddressAttributeParser>().Object,
            new Mock<IAddressAttributeService>().Object,
            new Mock<IAffiliateService>().Object,
            new Mock<ICustomerTagService>().Object,
            new Mock<IProductService>().Object,
            new Mock<ISalesEmployeeService>().Object,
            new Mock<ICustomerNoteService>().Object,
            new Mock<IDownloadService>().Object,
            new Mock<IHttpContextAccessor>().Object,
            new CustomerSettings(),
            new TaxSettings(),
            new LoyaltyPointsSettings(),
            new AddressSettings(),
            new CommonSettings(),
            new Mock<IEnumTranslationService>().Object);
    }

    [TestMethod]
    public async Task InsertCustomerModel_MapsStoreIdFromModel()
    {
        var model = new CustomerModel { StoreId = "store-1" };

        var customer = await _customerViewModelService.InsertCustomerModel(model);

        Assert.AreEqual("store-1", customer.StoreId);
        _customerServiceMock.Verify(c => c.InsertCustomer(It.Is<Customer>(x => x.StoreId == "store-1")), Times.Once);
    }

    [TestMethod]
    public async Task UpdateCustomerModel_MapsStoreIdFromModel()
    {
        var customer = new Customer { StoreId = "store-1" };
        var model = new CustomerModel { Email = "customer@example.com", StoreId = "store-2" };

        var result = await _customerViewModelService.UpdateCustomerModel(customer, model);

        Assert.AreEqual("store-2", result.StoreId);
        _customerServiceMock.Verify(
            c => c.UpdateCustomerInAdminPanel(It.Is<Customer>(x => x.StoreId == "store-2")), Times.Once);
    }
}
