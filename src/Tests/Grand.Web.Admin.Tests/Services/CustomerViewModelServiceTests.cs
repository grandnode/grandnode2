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
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CustomerViewModelServiceTests
{
    private const string CurrentStoreId = "store-current";
    private const string CurrentCustomerId = "current-user";

    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ICustomerTagService> _customerTagServiceMock;
    private Mock<IVendorService> _vendorServiceMock;
    private Mock<IEnumTranslationService> _enumTranslationServiceMock;
    private Mock<ICustomerNoteService> _customerNoteServiceMock;
    private CustomerViewModelService _customerViewModelService;

    [TestInitialize]
    public void Setup()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _groupServiceMock = new Mock<IGroupService>();
        _storeServiceMock = new Mock<IStoreService>();
        _customerTagServiceMock = new Mock<ICustomerTagService>();
        _vendorServiceMock = new Mock<IVendorService>();
        _enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        _customerNoteServiceMock = new Mock<ICustomerNoteService>();

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { Id = CurrentCustomerId });
        var storeContextMock = new Mock<IStoreContext>();
        storeContextMock.Setup(s => s.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = CurrentStoreId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);
        contextAccessorMock.Setup(c => c.StoreContext).Returns(storeContextMock.Object);

        var salesEmployeeServiceMock = new Mock<ISalesEmployeeService>();
        salesEmployeeServiceMock.Setup(s => s.GetAll()).ReturnsAsync(new List<SalesEmployee>());

        var customerAttributeServiceMock = new Mock<ICustomerAttributeService>();
        customerAttributeServiceMock.Setup(c => c.GetAllCustomerAttributes())
            .ReturnsAsync(new List<CustomerAttribute>());

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

        _customerNoteServiceMock.Setup(c => c.InsertCustomerNote(It.IsAny<CustomerNote>()))
            .Returns(Task.CompletedTask);

        _customerViewModelService = new CustomerViewModelService(
            _customerServiceMock.Object,
            _groupServiceMock.Object,
            new Mock<ICustomerProductService>().Object,
            new Mock<INewsLetterSubscriptionService>().Object,
            new Mock<IDateTimeService>().Object,
            new Mock<ITranslationService>().Object,
            new Mock<ILoyaltyPointsService>().Object,
            new Mock<ICountryService>().Object,
            contextAccessorMock.Object,
            _vendorServiceMock.Object,
            _storeServiceMock.Object,
            new Mock<ICustomerAttributeParser>().Object,
            customerAttributeServiceMock.Object,
            new Mock<IAddressAttributeParser>().Object,
            new Mock<IAddressAttributeService>().Object,
            new Mock<IAffiliateService>().Object,
            _customerTagServiceMock.Object,
            new Mock<IProductService>().Object,
            salesEmployeeServiceMock.Object,
            _customerNoteServiceMock.Object,
            new Mock<IDownloadService>().Object,
            new Mock<IHttpContextAccessor>().Object,
            new CustomerSettings(),
            new TaxSettings(),
            new LoyaltyPointsSettings(),
            new AddressSettings(),
            new CommonSettings(),
            _enumTranslationServiceMock.Object);
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
    public async Task PrepareCustomerModel_StoreManager_PresetsCurrentStoreId()
    {
        _groupServiceMock.Setup(g => g.IsStoreManager(It.IsAny<Customer>())).ReturnsAsync(true);

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model, null, false);

        Assert.AreEqual(CurrentStoreId, model.StoreId);
    }

    [TestMethod]
    public async Task PrepareCustomerModel_NonStoreManager_DoesNotPresetStoreId()
    {
        _groupServiceMock.Setup(g => g.IsStoreManager(It.IsAny<Customer>())).ReturnsAsync(false);

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model, null, false);

        Assert.IsTrue(string.IsNullOrEmpty(model.StoreId));
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

    [TestMethod]
    public async Task PrepareCustomerListModel_SelectsRegisteredGroupByDefault()
    {
        var registered = new CustomerGroup { Id = "reg", Name = "Registered" };
        _groupServiceMock.Setup(g => g.GetCustomerGroupBySystemName(It.IsAny<string>()))
            .ReturnsAsync(registered);
        _groupServiceMock
            .Setup(g => g.GetAllCustomerGroups(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<CustomerGroup> { registered });
        _customerTagServiceMock.Setup(t => t.GetAllCustomerTags()).ReturnsAsync(new List<CustomerTag>());

        var model = await _customerViewModelService.PrepareCustomerListModel();

        Assert.IsTrue(model.AvailableCustomerGroups.Any(x => x.Value == "reg" && x.Selected));
        CollectionAssert.Contains(model.SearchCustomerGroupIds.ToList(), "reg");
    }

    [TestMethod]
    public async Task DeleteCustomer_DeletesCustomer()
    {
        var customer = new Customer { Id = "c1", Email = "customer@example.com" };

        await _customerViewModelService.DeleteCustomer(customer);

        _customerServiceMock.Verify(c => c.DeleteCustomer(customer), Times.Once);
    }

    [TestMethod]
    public async Task DeleteSelected_DeletesAllExceptCurrentUser()
    {
        var other = new Customer { Id = "other" };
        var current = new Customer { Id = CurrentCustomerId };
        _customerServiceMock.Setup(c => c.GetCustomersByIds(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Customer> { other, current });

        await _customerViewModelService.DeleteSelected(new[] { "other", CurrentCustomerId });

        _customerServiceMock.Verify(c => c.DeleteCustomer(other), Times.Once);
        _customerServiceMock.Verify(c => c.DeleteCustomer(current), Times.Never);
    }

    [TestMethod]
    public async Task PrepareCustomerModelAddProductModel_BuildsAvailableLists()
    {
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Grand.Domain.Stores.Store> { new() { Id = "store-1", Shortcut = "Store 1" } });
        _vendorServiceMock
            .Setup(v => v.GetAllVendors(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<Vendor> { new() { Id = "vendor-1", Name = "Vendor 1" } });
        _enumTranslationServiceMock
            .Setup(e => e.ToSelectList(It.IsAny<ProductType>(), It.IsAny<bool>(), It.IsAny<int[]>()))
            .Returns(new SelectList(Enumerable.Empty<SelectListItem>()));

        var model = await _customerViewModelService.PrepareCustomerModelAddProductModel();

        Assert.IsTrue(model.AvailableStores.Any(x => x.Value == "store-1"));
        Assert.IsTrue(model.AvailableVendors.Any(x => x.Value == "vendor-1"));
        Assert.IsTrue(model.AvailableProductTypes.Count > 0);
    }

    [TestMethod]
    public async Task InsertCustomerNote_InsertsNoteWithMappedFields()
    {
        var note = await _customerViewModelService.InsertCustomerNote("c1", "d1", false, "title", "message");

        Assert.AreEqual("c1", note.CustomerId);
        Assert.AreEqual("d1", note.DownloadId);
        Assert.AreEqual("title", note.Title);
        Assert.AreEqual("message", note.Note);
        Assert.IsFalse(note.DisplayToCustomer);
        _customerNoteServiceMock.Verify(
            c => c.InsertCustomerNote(It.Is<CustomerNote>(x => x.CustomerId == "c1" && x.Title == "title")),
            Times.Once);
    }
}
