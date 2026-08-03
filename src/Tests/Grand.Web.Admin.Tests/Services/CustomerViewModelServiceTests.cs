using Grand.Business.Core.Interfaces.Authentication;
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
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Domain.Vendors;
using Grand.Domain.Messages;
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
    private Mock<ICustomerProductService> _customerProductServiceMock;
    private Mock<ILoyaltyPointsService> _loyaltyPointsServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<IDateTimeService> _dateTimeServiceMock;
    private Mock<IDownloadService> _downloadServiceMock;
    private Mock<INewsLetterSubscriptionService> _newsLetterSubscriptionServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Customer _currentCustomer;
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
        _customerProductServiceMock = new Mock<ICustomerProductService>();
        _loyaltyPointsServiceMock = new Mock<ILoyaltyPointsService>();
        _productServiceMock = new Mock<IProductService>();
        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _downloadServiceMock = new Mock<IDownloadService>();
        _newsLetterSubscriptionServiceMock = new Mock<INewsLetterSubscriptionService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);

        _dateTimeServiceMock.Setup(d => d.ConvertToUserTime(It.IsAny<DateTime>(), It.IsAny<DateTimeKind>()))
            .Returns<DateTime, DateTimeKind>((dt, _) => dt);

        _currentCustomer = new Customer { Id = CurrentCustomerId };
        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(_currentCustomer);
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

        //http context with a service provider that resolves the external authentication service
        var externalAuthServiceMock = new Mock<IExternalAuthenticationService>();
        externalAuthServiceMock.Setup(e => e.GetExternalIdentifiers(It.IsAny<Customer>()))
            .ReturnsAsync(new List<ExternalAuthentication>());
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(s => s.GetService(typeof(IExternalAuthenticationService)))
            .Returns(externalAuthServiceMock.Object);
        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.RequestServices).Returns(serviceProviderMock.Object);
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);

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

        _groupServiceMock.Setup(g => g.GetAllByIds(It.IsAny<string[]>()))
            .ReturnsAsync(new List<CustomerGroup>());

        _customerViewModelService = new CustomerViewModelService(
            _customerServiceMock.Object,
            _groupServiceMock.Object,
            _customerProductServiceMock.Object,
            _newsLetterSubscriptionServiceMock.Object,
            _dateTimeServiceMock.Object,
            _translationServiceMock.Object,
            _loyaltyPointsServiceMock.Object,
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
            _productServiceMock.Object,
            salesEmployeeServiceMock.Object,
            _customerNoteServiceMock.Object,
            _downloadServiceMock.Object,
            httpContextAccessorMock.Object,
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
    public async Task PrepareCustomerModel_ExistingCustomer_MapsStoreIdFromEntity()
    {
        var customer = new Customer { Id = "c1", Email = "customer@example.com", StoreId = "store-9" };
        var model = new CustomerModel();

        await _customerViewModelService.PrepareCustomerModel(model, customer, false);

        Assert.AreEqual("store-9", model.StoreId);
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

    [TestMethod]
    public async Task DeleteCustomerNote_DeletesNoteAndAttachment()
    {
        var note = new CustomerNote { Id = "n1", DownloadId = "d1" };
        var download = new Download { Id = "d1" };
        _customerNoteServiceMock.Setup(c => c.GetCustomerNote("n1")).ReturnsAsync(note);
        _customerNoteServiceMock.Setup(c => c.DeleteCustomerNote(It.IsAny<CustomerNote>())).Returns(Task.CompletedTask);
        _downloadServiceMock.Setup(d => d.GetDownloadById("d1")).ReturnsAsync(download);
        _downloadServiceMock.Setup(d => d.DeleteDownload(It.IsAny<Download>())).Returns(Task.CompletedTask);

        await _customerViewModelService.DeleteCustomerNote("n1", "c1");

        _customerNoteServiceMock.Verify(c => c.DeleteCustomerNote(note), Times.Once);
        _downloadServiceMock.Verify(d => d.DeleteDownload(download), Times.Once);
    }

    [TestMethod]
    public async Task DeleteCustomerNote_NotFound_Throws()
    {
        _customerNoteServiceMock.Setup(c => c.GetCustomerNote(It.IsAny<string>()))
            .ReturnsAsync((CustomerNote)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _customerViewModelService.DeleteCustomerNote("missing", "c1"));
    }

    [TestMethod]
    public async Task PrepareLoyaltyPointsHistoryModel_MapsHistory()
    {
        _loyaltyPointsServiceMock
            .Setup(l => l.GetLoyaltyPointsHistory(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<LoyaltyPointsHistory> {
                new() { StoreId = "store-1", Points = 5, PointsBalance = 10, Message = "added" }
            });
        _storeServiceMock.Setup(s => s.GetStoreById("store-1"))
            .ReturnsAsync(new Grand.Domain.Stores.Store { Id = "store-1", Shortcut = "Store 1" });

        var result = (await _customerViewModelService.PrepareLoyaltyPointsHistoryModel("c1")).ToList();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Store 1", result[0].StoreName);
        Assert.AreEqual(5, result[0].Points);
    }

    [TestMethod]
    public async Task InsertLoyaltyPointsHistory_DelegatesToService()
    {
        var history = new LoyaltyPointsHistory();
        _loyaltyPointsServiceMock
            .Setup(l => l.AddLoyaltyPointsHistory("c1", 10, "store-1", "msg", It.IsAny<string>(), It.IsAny<double>()))
            .ReturnsAsync(history);

        var result = await _customerViewModelService.InsertLoyaltyPointsHistory(
            new Customer { Id = "c1" }, "store-1", 10, "msg");

        Assert.AreSame(history, result);
        _loyaltyPointsServiceMock.Verify(
            l => l.AddLoyaltyPointsHistory("c1", 10, "store-1", "msg", It.IsAny<string>(), It.IsAny<double>()),
            Times.Once);
    }

    [TestMethod]
    public async Task DeleteAddress_RemovesAddressAndUpdatesCustomer()
    {
        var address = new Address { Id = "a1" };
        var customer = new Customer();
        customer.Addresses.Add(address);

        await _customerViewModelService.DeleteAddress(customer, address);

        Assert.IsFalse(customer.Addresses.Any(a => a.Id == "a1"));
        _customerServiceMock.Verify(c => c.UpdateCustomerInAdminPanel(customer), Times.Once);
    }

    [TestMethod]
    public async Task UpdateProductPrice_UpdatesWhenFound()
    {
        var price = new CustomerProductPrice { Id = "pp1", Price = 1 };
        _customerProductServiceMock.Setup(c => c.GetCustomerProductPriceById("pp1")).ReturnsAsync(price);
        _customerProductServiceMock.Setup(c => c.UpdateCustomerProductPrice(It.IsAny<CustomerProductPrice>()))
            .Returns(Task.CompletedTask);

        await _customerViewModelService.UpdateProductPrice(
            new CustomerModel.ProductPriceModel { Id = "pp1", Price = 99 });

        _customerProductServiceMock.Verify(
            c => c.UpdateCustomerProductPrice(It.Is<CustomerProductPrice>(x => x.Price == 99)), Times.Once);
    }

    [TestMethod]
    public async Task DeleteProductPrice_DeletesWhenFound()
    {
        var price = new CustomerProductPrice { Id = "pp1" };
        _customerProductServiceMock.Setup(c => c.GetCustomerProductPriceById("pp1")).ReturnsAsync(price);
        _customerProductServiceMock.Setup(c => c.DeleteCustomerProductPrice(It.IsAny<CustomerProductPrice>()))
            .Returns(Task.CompletedTask);

        await _customerViewModelService.DeleteProductPrice("pp1");

        _customerProductServiceMock.Verify(c => c.DeleteCustomerProductPrice(price), Times.Once);
    }

    [TestMethod]
    public async Task DeleteProductPrice_NotFound_Throws()
    {
        _customerProductServiceMock.Setup(c => c.GetCustomerProductPriceById(It.IsAny<string>()))
            .ReturnsAsync((CustomerProductPrice)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _customerViewModelService.DeleteProductPrice("missing"));
    }

    [TestMethod]
    public async Task UpdatePersonalizedProduct_UpdatesWhenFound()
    {
        var customerProduct = new CustomerProduct { Id = "cp1", DisplayOrder = 1 };
        _customerProductServiceMock.Setup(c => c.GetCustomerProduct("cp1")).ReturnsAsync(customerProduct);
        _customerProductServiceMock.Setup(c => c.UpdateCustomerProduct(It.IsAny<CustomerProduct>()))
            .Returns(Task.CompletedTask);

        await _customerViewModelService.UpdatePersonalizedProduct(
            new CustomerModel.ProductModel { Id = "cp1", DisplayOrder = 7 });

        _customerProductServiceMock.Verify(
            c => c.UpdateCustomerProduct(It.Is<CustomerProduct>(x => x.DisplayOrder == 7)), Times.Once);
    }

    [TestMethod]
    public async Task DeletePersonalizedProduct_NotFound_Throws()
    {
        _customerProductServiceMock.Setup(c => c.GetCustomerProduct(It.IsAny<string>()))
            .ReturnsAsync((CustomerProduct)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _customerViewModelService.DeletePersonalizedProduct("missing"));
    }

    [TestMethod]
    public async Task PrepareProductPriceModel_MapsItems()
    {
        _customerProductServiceMock
            .Setup(c => c.GetProductsPriceByCustomer(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<CustomerProductPrice> { new() { Id = "pp1", ProductId = "p1", Price = 3 } });
        _productServiceMock.Setup(p => p.GetProductById("p1", false))
            .ReturnsAsync(new Product { Id = "p1", Name = "Prod 1" });

        var (items, _) = await _customerViewModelService.PrepareProductPriceModel("c1", 1, 10);

        var list = items.ToList();
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("Prod 1", list[0].ProductName);
        Assert.AreEqual(3, list[0].Price);
    }

    [TestMethod]
    public async Task InsertCustomerAddProductModel_Personalized_InsertsCustomerProduct()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false))
            .ReturnsAsync(new Product { Id = "p1", Price = 5 });
        _customerProductServiceMock.Setup(c => c.GetCustomerProduct("c1", "p1")).ReturnsAsync((CustomerProduct)null);
        _customerProductServiceMock.Setup(c => c.InsertCustomerProduct(It.IsAny<CustomerProduct>()))
            .Returns(Task.CompletedTask);

        await _customerViewModelService.InsertCustomerAddProductModel("c1", true,
            new CustomerModel.AddProductModel { SelectedProductIds = new[] { "p1" } });

        _customerProductServiceMock.Verify(
            c => c.InsertCustomerProduct(It.Is<CustomerProduct>(x => x.CustomerId == "c1" && x.ProductId == "p1")),
            Times.Once);
    }

    [TestMethod]
    public async Task InsertCustomerAddProductModel_NotPersonalized_InsertsProductPrice()
    {
        _productServiceMock.Setup(p => p.GetProductById("p1", false))
            .ReturnsAsync(new Product { Id = "p1", Price = 5 });
        _customerProductServiceMock.Setup(c => c.GetPriceByCustomerProduct("c1", "p1"))
            .ReturnsAsync((double?)null);
        _customerProductServiceMock.Setup(c => c.InsertCustomerProductPrice(It.IsAny<CustomerProductPrice>()))
            .Returns(Task.CompletedTask);

        await _customerViewModelService.InsertCustomerAddProductModel("c1", false,
            new CustomerModel.AddProductModel { SelectedProductIds = new[] { "p1" } });

        _customerProductServiceMock.Verify(
            c => c.InsertCustomerProductPrice(
                It.Is<CustomerProductPrice>(x => x.CustomerId == "c1" && x.ProductId == "p1" && x.Price == 5)),
            Times.Once);
    }

    [TestMethod]
    public async Task PreparePersonalizedProducts_MapsItems()
    {
        _customerProductServiceMock
            .Setup(c => c.GetProductsByCustomer(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<CustomerProduct>
                { new() { Id = "cp1", ProductId = "p1", DisplayOrder = 2 } });
        _productServiceMock.Setup(p => p.GetProductById("p1", false))
            .ReturnsAsync(new Product { Id = "p1", Name = "Prod 1" });

        var (items, _) = await _customerViewModelService.PreparePersonalizedProducts("c1", 1, 10);

        var list = items.ToList();
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("Prod 1", list[0].ProductName);
        Assert.AreEqual(2, list[0].DisplayOrder);
    }

    [TestMethod]
    public async Task DeleteCustomer_RemovesNewsletterSubscriptions()
    {
        var customer = new Customer { Id = "c1", Email = "customer@example.com" };
        var subscription = new NewsLetterSubscription { Id = "s1", Email = customer.Email, StoreId = "store-1" };
        _storeServiceMock.Setup(s => s.GetAllStores())
            .ReturnsAsync(new List<Grand.Domain.Stores.Store> { new() { Id = "store-1" } });
        _newsLetterSubscriptionServiceMock
            .Setup(n => n.GetNewsLetterSubscriptionByEmailAndStoreId(customer.Email, "store-1"))
            .ReturnsAsync(subscription);
        _newsLetterSubscriptionServiceMock
            .Setup(n => n.DeleteNewsLetterSubscription(It.IsAny<NewsLetterSubscription>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        await _customerViewModelService.DeleteCustomer(customer);

        _customerServiceMock.Verify(c => c.DeleteCustomer(customer), Times.Once);
        _newsLetterSubscriptionServiceMock.Verify(
            n => n.DeleteNewsLetterSubscription(subscription, It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task PrepareCustomerListModel_MapsSettingsGroupsAndTags()
    {
        var registered = new CustomerGroup { Id = "reg", Name = "Registered" };
        var guests = new CustomerGroup { Id = "gst", Name = "Guests" };
        _groupServiceMock.Setup(g => g.GetCustomerGroupBySystemName(It.IsAny<string>()))
            .ReturnsAsync(registered);
        _groupServiceMock
            .Setup(g => g.GetAllCustomerGroups(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<CustomerGroup> { registered, guests });
        _customerTagServiceMock.Setup(t => t.GetAllCustomerTags())
            .ReturnsAsync(new List<CustomerTag> { new() { Id = "t1", Name = "VIP" } });

        var model = await _customerViewModelService.PrepareCustomerListModel();

        Assert.AreEqual(2, model.AvailableCustomerGroups.Count);
        Assert.IsTrue(model.AvailableCustomerGroups.Any(x => x.Value == "gst" && !x.Selected));
        Assert.IsTrue(model.AvailableCustomerTags.Any(x => x.Value == "t1" && x.Text == "VIP"));
        Assert.AreEqual(new CustomerSettings().UsernamesEnabled, model.UsernamesEnabled);
        Assert.AreEqual(new CustomerSettings().CompanyEnabled, model.CompanyEnabled);
    }

    [TestMethod]
    public async Task PrepareCustomerListModel_RegisteredGroupMissing_NullSearchGroupId()
    {
        _groupServiceMock.Setup(g => g.GetCustomerGroupBySystemName(It.IsAny<string>()))
            .ReturnsAsync(new CustomerGroup { Id = "reg" });
        _groupServiceMock
            .Setup(g => g.GetAllCustomerGroups(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<CustomerGroup> { new() { Id = "other" } });
        _customerTagServiceMock.Setup(t => t.GetAllCustomerTags()).ReturnsAsync(new List<CustomerTag>());

        var model = await _customerViewModelService.PrepareCustomerListModel();

        //registered group is not among the available groups -> the search id resolves to null
        Assert.AreEqual(1, model.SearchCustomerGroupIds.Count);
        Assert.IsNull(model.SearchCustomerGroupIds.First());
        Assert.IsFalse(model.AvailableCustomerGroups.Any(x => x.Selected));
    }

    [TestMethod]
    public async Task PrepareCustomerList_MapsCustomers()
    {
        SetupGetAllCustomers(new PagedList<Customer>
            { new() { Id = "c1", Email = "customer@example.com", Active = true } });

        var (list, _) = await _customerViewModelService.PrepareCustomerList(
            new CustomerListModel(), new[] { "grp" }, new[] { "tag" }, 1, 10);

        var items = list.ToList();
        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("c1", items[0].Id);
        Assert.AreEqual("customer@example.com", items[0].Email);
    }

    [TestMethod]
    public async Task PrepareCustomerList_FiltersBySalesEmployeeAndPaging()
    {
        _currentCustomer.SeId = "se-1";
        SetupGetAllCustomers(new PagedList<Customer>());

        await _customerViewModelService.PrepareCustomerList(
            new CustomerListModel(), new[] { "grp" }, new[] { "tag" }, 2, 15);

        //salesEmployeeId comes from the current customer, pageIndex is zero-based
        _customerServiceMock.Verify(c => c.GetAllCustomers(
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), "se-1",
            It.Is<string[]>(g => g.SequenceEqual(new[] { "grp" })),
            It.Is<string[]>(t => t.SequenceEqual(new[] { "tag" })),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<ShoppingCartType?>(), 1, 15,
            It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>()), Times.Once);
    }

    [TestMethod]
    public async Task PrepareCustomerList_GuestCustomer_UsesGuestResource()
    {
        SetupGetAllCustomers(new PagedList<Customer> { new() { Id = "guest", Email = "", Active = true } });

        var (list, _) = await _customerViewModelService.PrepareCustomerList(
            new CustomerListModel(), Array.Empty<string>(), Array.Empty<string>(), 1, 10);

        Assert.AreEqual("Admin.Customers.Guest", list.First().Email);
    }

    [TestMethod]
    public async Task PrepareCustomerList_NoCustomers_ReturnsEmpty()
    {
        SetupGetAllCustomers(new PagedList<Customer>());

        var (list, _) = await _customerViewModelService.PrepareCustomerList(
            new CustomerListModel(), Array.Empty<string>(), Array.Empty<string>(), 1, 10);

        Assert.AreEqual(0, list.Count());
    }

    private void SetupGetAllCustomers(IPagedList<Customer> result)
    {
        _customerServiceMock
            .Setup(c => c.GetAllCustomers(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(),
                It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>()))
            .ReturnsAsync(result);
    }
}
