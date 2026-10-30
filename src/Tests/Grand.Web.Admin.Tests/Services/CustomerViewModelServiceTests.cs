using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
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
using Grand.Infrastructure.Configuration;
using Grand.Web.AdminShared.Interfaces;
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
    private Mock<IAffiliateService> _affiliateServiceMock;
    private Mock<IEnumTranslationService> _enumTranslationServiceMock;
    private Mock<IDateTimeService> _dateTimeServiceMock;
    private Mock<INewsLetterSubscriptionService> _newsLetterSubscriptionServiceMock;
    private Mock<IRequestTranslationService> _translationServiceMock;
    private Customer _currentCustomer;
    private CustomerConfig _customerConfig;
    private Mock<IAdminDataScope<Customer>> _scopeMock;
    private Mock<IPriceFormatter> _priceFormatterMock;
    private Mock<ICurrencyService> _currencyServiceMock;
    private Mock<ICompanyCreditService> _companyCreditServiceMock;
    private CustomerViewModelService _customerViewModelService;

    [TestInitialize]
    public void Setup()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _groupServiceMock = new Mock<IGroupService>();
        _storeServiceMock = new Mock<IStoreService>();
        _customerTagServiceMock = new Mock<ICustomerTagService>();
        _affiliateServiceMock = new Mock<IAffiliateService>();
        _enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _newsLetterSubscriptionServiceMock = new Mock<INewsLetterSubscriptionService>();
        _translationServiceMock = new Mock<IRequestTranslationService>();
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

        _groupServiceMock.Setup(g => g.GetAllByIds(It.IsAny<string[]>()))
            .ReturnsAsync(new List<CustomerGroup>());

        SetupGetAllCustomers(new PagedList<Customer>());
        _customerConfig = new CustomerConfig();
        //Admin panel: no default store, every customer in reach
        _scopeMock = new Mock<IAdminDataScope<Customer>>();
        _scopeMock.Setup(s => s.GetDefaultStoreIdAsync()).ReturnsAsync((string)null);
        _scopeMock.Setup(s => s.HasAccess(It.IsAny<Customer>())).ReturnsAsync(true);

        _priceFormatterMock = new Mock<IPriceFormatter>();
        _currencyServiceMock = new Mock<ICurrencyService>();
        _currencyServiceMock.Setup(c => c.GetPrimaryStoreCurrency())
            .ReturnsAsync(new Grand.Domain.Directory.Currency { CurrencyCode = "EUR" });

        _companyCreditServiceMock = new Mock<ICompanyCreditService>();

        _customerViewModelService = new CustomerViewModelService(
            _customerServiceMock.Object,
            _groupServiceMock.Object,
            _newsLetterSubscriptionServiceMock.Object,
            _dateTimeServiceMock.Object,
            _translationServiceMock.Object,
            new Mock<ICountryService>().Object,
            contextAccessorMock.Object,
            _storeServiceMock.Object,
            new Mock<ICustomerAttributeParser>().Object,
            customerAttributeServiceMock.Object,
            _affiliateServiceMock.Object,
            _customerTagServiceMock.Object,
            salesEmployeeServiceMock.Object,
            httpContextAccessorMock.Object,
            new CustomerSettings(),
            new TaxSettings(),
            new LoyaltyPointsSettings(),
            new CommonSettings(),
            _enumTranslationServiceMock.Object,
            _customerConfig,
            _scopeMock.Object,
            _priceFormatterMock.Object,
            _currencyServiceMock.Object,
            _companyCreditServiceMock.Object);
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

    /// <summary>
    /// TabInfo.cshtml (Task 13) gates CustomerGroups/CustomerTags/VendorId/StaffStoreId/StoreId/
    /// Owner/SeId/AffiliateId behind "@if (Scope.DefaultStoreId is null)" — Admin-scoped rendering
    /// only. PrepareCustomerModel itself is host-agnostic and always populates these fields; the
    /// Store host's own BaseCustomerController.ApplyPostConstraints (not PrepareCustomerModel)
    /// blanks/forces them back on POST. This test proves PrepareCustomerModel's output has every
    /// field both hosts' TabInfo.cshtml needs, for a global-scoped (Admin) call, so Admin's view
    /// has real data to render behind its gate.
    /// </summary>
    [TestMethod]
    public async Task PrepareCustomerModel_GlobalScopedCall_PopulatesAdminOnlyGatedFields()
    {
        var customer = new Customer
        {
            Id = "c1",
            Email = "customer@example.com",
            VendorId = "vendor-1",
            StaffStoreId = "store-staff",
            StoreId = "store-9",
            SeId = "se-1",
            AffiliateId = "aff-1"
        };
        customer.CustomerTags.Add("tag-1");
        _customerTagServiceMock.Setup(t => t.GetCustomerTagById("tag-1"))
            .ReturnsAsync(new CustomerTag { Id = "tag-1", Name = "VIP" });
        _groupServiceMock.Setup(g => g.IsOwner(customer)).ReturnsAsync(true);
        _affiliateServiceMock.Setup(a => a.GetAffiliateById("aff-1"))
            .ReturnsAsync(new Grand.Domain.Affiliates.Affiliate
            {
                Id = "aff-1",
                Address = new Address { FirstName = "Aff", LastName = "Iliate" }
            });

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model, customer, false);

        //fields TabInfo.cshtml renders unconditionally on both hosts
        Assert.AreEqual("customer@example.com", model.Email);
        //fields TabInfo.cshtml gates behind "Scope.DefaultStoreId is null" (category (a)/(b) per
        //Task 13's audit) — PrepareCustomerModel always populates them; the view/controller layer
        //decides visibility, not this service.
        Assert.AreEqual("VIP, ", model.CustomerTags);
        Assert.IsTrue(string.IsNullOrEmpty(model.Owner)); //IsOwner true -> Owner blanked, by design
        Assert.AreEqual("aff-1", model.AffiliateId);
    }

    /// <summary>
    /// Store-scoped call: BaseCustomerController.ApplyPostConstraints (Store's override, exercised
    /// on POST, not by PrepareCustomerModel) is what actually forces/blanks StoreId/Owner/VendorId/
    /// StaffStoreId/SeId/CustomerGroups. This test proves the GET/prepare path still leaves those
    /// same fields populated from the entity for an existing customer even when scoped to a store,
    /// confirming TabInfo.cshtml's Scope-gate — not this service — is what hides them for Store.
    /// </summary>
    [TestMethod]
    public async Task PrepareCustomerModel_StoreScopedExistingCustomer_StillMapsGatedFieldsFromEntity()
    {
        var customer = new Customer
        {
            Id = "c1",
            Email = "customer@example.com",
            StoreId = "store-9",
            VendorId = "vendor-1"
        };

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model, customer, false);

        //fields Store's TabInfo.cshtml needs (ungated, always rendered)
        Assert.AreEqual("customer@example.com", model.Email);
        //PrepareCustomerModel maps StoreId/VendorId from the entity regardless of host; it is
        //ApplyPostConstraints (controller-level, Store-only) that later forces StoreId back to
        //scope.DefaultStoreId and blanks VendorId on submit -- not this method.
        Assert.AreEqual("store-9", model.StoreId);
        Assert.AreEqual("vendor-1", model.VendorId);
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

    #region Store panel list

    private void VerifyListQuery(string[] excludedGroupIds, bool excludeVendorAndStaff) =>
        _customerServiceMock.Verify(c => c.GetAllCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string[]>(), It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>(),
            It.Is<string[]>(ids => excludedGroupIds == null
                ? ids == null
                : ids != null && ids.OrderBy(x => x).SequenceEqual(excludedGroupIds.OrderBy(x => x))),
            excludeVendorAndStaff), Times.Once);

    [TestMethod]
    public async Task PrepareCustomerList_StorePanel_LeavesTheStaffOutInTheQuery()
    {
        _scopeMock.Setup(s => s.GetDefaultStoreIdAsync()).ReturnsAsync("store-7");
        foreach (var name in StoreCustomerDataScope.StaffGroupSystemNames)
            _groupServiceMock.Setup(g => g.GetCustomerGroupBySystemName(name))
                .ReturnsAsync(new CustomerGroup { Id = "id-" + name, SystemName = name });

        await _customerViewModelService.PrepareCustomerList(new CustomerListModel(), null, null, 1, 10, "store-7");

        VerifyListQuery(StoreCustomerDataScope.StaffGroupSystemNames.Select(n => "id-" + n).ToArray(), true);
    }

    [TestMethod]
    public async Task PrepareCustomerList_AdminPanel_ExcludesNobody()
    {
        await _customerViewModelService.PrepareCustomerList(new CustomerListModel(), null, null, 1, 10);

        VerifyListQuery(null, false);
    }

    #endregion

    #region Owner and sub-accounts

    [TestMethod]
    public async Task UpdateCustomerModel_OwnerEmailUnchanged_KeepsTheLinkedOwner()
    {
        //posted back as shown, the owner is kept even where an email lookup would find someone else
        var owner = new Customer { Id = "owner-1", Email = "boss@firm.com" };
        _customerServiceMock.Setup(c => c.GetCustomerById("owner-1")).ReturnsAsync(owner);
        _customerServiceMock.Setup(c => c.GetCustomerByEmail("boss@firm.com", It.IsAny<string>()))
            .ReturnsAsync(new Customer { Id = "same-email-other-store" });
        var customer = new Customer { Id = "c1", OwnerId = "owner-1" };

        await _customerViewModelService.UpdateCustomerModel(customer,
            new CustomerModel { Email = "c1@firm.com", Owner = "Boss@Firm.com " });

        Assert.AreEqual("owner-1", customer.OwnerId);
    }

    [TestMethod]
    public async Task UpdateCustomerModel_NewOwner_PerStore_IsLookedUpInTheCustomersStore()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        _customerServiceMock.Setup(c => c.GetCustomerByEmail("boss@firm.com", "store-2"))
            .ReturnsAsync(new Customer { Id = "owner-store-2" });
        var customer = new Customer { Id = "c1" };

        await _customerViewModelService.UpdateCustomerModel(customer,
            new CustomerModel { Email = "c1@firm.com", StoreId = "store-2", Owner = "boss@firm.com" });

        //the same scope the validator checks in - the global lookup used to pick any store's account
        Assert.AreEqual("owner-store-2", customer.OwnerId);
        _customerServiceMock.Verify(c => c.GetCustomerByEmail("boss@firm.com", ""), Times.Never);
    }

    [TestMethod]
    public async Task UpdateCustomerModel_PerStore_CustomerMovedToAnotherStore_LooksTheOwnerUpAgain()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        _customerServiceMock.Setup(c => c.GetCustomerById("owner-a"))
            .ReturnsAsync(new Customer { Id = "owner-a", Email = "boss@firm.com" });
        _customerServiceMock.Setup(c => c.GetCustomerByEmail("boss@firm.com", "store-b"))
            .ReturnsAsync(new Customer { Id = "owner-b" });
        var customer = new Customer { Id = "c1", OwnerId = "owner-a", StoreId = "store-a" };

        await _customerViewModelService.UpdateCustomerModel(customer,
            new CustomerModel { Email = "c1@firm.com", StoreId = "store-b", Owner = "boss@firm.com" });

        Assert.AreEqual("owner-b", customer.OwnerId);
    }

    [TestMethod]
    public async Task UpdateCustomerModel_OwnerCleared_UnlinksTheOwner()
    {
        var customer = new Customer { Id = "c1", OwnerId = "owner-1" };

        await _customerViewModelService.UpdateCustomerModel(customer,
            new CustomerModel { Email = "c1@firm.com", Owner = "" });

        Assert.AreEqual(string.Empty, customer.OwnerId);
    }

    [TestMethod]
    public async Task InsertCustomerModel_WithOwner_LinksIt()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail("boss@firm.com", ""))
            .ReturnsAsync(new Customer { Id = "owner-1" });

        var customer = await _customerViewModelService.InsertCustomerModel(
            new CustomerModel { Email = "new@firm.com", Owner = "boss@firm.com" });

        Assert.AreEqual("owner-1", customer.OwnerId);
    }

    [TestMethod]
    public async Task PrepareCustomerModel_Owner_ListsItsSubAccounts()
    {
        var owner = new Customer { Id = "owner-1", Email = "boss@firm.com" };
        _customerServiceMock
            .Setup(c => c.GetAllCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), "owner-1", It.IsAny<string>(), It.IsAny<string[]>(),
                It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>()))
            .ReturnsAsync(new PagedList<Customer> {
                new() { Id = "sub-1", Email = "a@firm.com", Active = true, OwnerId = "owner-1" },
                new() { Id = "sub-2", Email = "b@firm.com", Active = false, OwnerId = "owner-1" }
            });

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model, owner, false);

        CollectionAssert.AreEqual(new[] { "sub-1", "sub-2" }, model.SubAccounts.Select(x => x.Id).ToArray());
        Assert.IsFalse(model.SubAccounts[1].Active);
        Assert.IsTrue(string.IsNullOrEmpty(model.OwnerCustomerId));
    }

    [TestMethod]
    public async Task PrepareCustomerModel_SubAccountsShowRoleAndLimit()
    {
        _enumTranslationServiceMock.Setup(e => e.GetTranslationEnum(SubAccountRole.Buyer)).Returns("Buyer text");
        _enumTranslationServiceMock.Setup(e => e.GetTranslationEnum(SubAccountRole.Approver)).Returns("Approver text");
        _priceFormatterMock.Setup(p => p.FormatPrice(1200, It.Is<Grand.Domain.Directory.Currency>(c => c.CurrencyCode == "EUR")))
            .Returns("EUR 1,200.00");
        SetupGetAllCustomers(new PagedList<Customer> {
            new() { Id = "sub-1", Email = "a@firm.com", OwnerId = "owner-1", SubAccountRole = SubAccountRole.Buyer, OrderApprovalLimit = 1200 },
            new() { Id = "sub-2", Email = "b@firm.com", OwnerId = "owner-1", SubAccountRole = SubAccountRole.Buyer },
            new() { Id = "sub-3", Email = "c@firm.com", OwnerId = "owner-1", SubAccountRole = SubAccountRole.Approver }
        });

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model, new Customer { Id = "owner-1" }, false);

        Assert.AreEqual("Buyer text", model.SubAccounts[0].Role);
        Assert.AreEqual("EUR 1,200.00", model.SubAccounts[0].OrderApprovalLimit);
        //no limit is shown as empty, not as a zero price
        Assert.AreEqual("Buyer text", model.SubAccounts[1].Role);
        Assert.IsTrue(string.IsNullOrEmpty(model.SubAccounts[1].OrderApprovalLimit));
        Assert.AreEqual("Approver text", model.SubAccounts[2].Role);
        Assert.IsTrue(string.IsNullOrEmpty(model.SubAccounts[2].OrderApprovalLimit));
    }

    [TestMethod]
    public async Task PrepareCustomerModel_StorePanel_QueriesOnlyItsStoreAndSkipsOutOfScopeRows()
    {
        _scopeMock.Setup(s => s.GetDefaultStoreIdAsync()).ReturnsAsync("store-7");
        var foreign = new Customer { Id = "sub-x", Email = "x@firm.com", OwnerId = "owner-1" };
        _scopeMock.Setup(s => s.HasAccess(foreign)).ReturnsAsync(false);
        SetupGetAllCustomers(new PagedList<Customer> {
            new() { Id = "sub-1", Email = "a@firm.com", OwnerId = "owner-1" }, foreign
        });

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model, new Customer { Id = "owner-1" }, false);

        CollectionAssert.AreEqual(new[] { "sub-1" }, model.SubAccounts.Select(x => x.Id).ToArray());
        _customerServiceMock.Verify(c => c.GetAllCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<string>(), "store-7", "owner-1", It.IsAny<string>(), It.IsAny<string[]>(),
            It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>()), Times.Once);
    }

    [TestMethod]
    public async Task PrepareCustomerModel_SalesManager_ListsOnlyTheirSubAccounts()
    {
        _currentCustomer.SeId = "se-1";

        await _customerViewModelService.PrepareCustomerModel(new CustomerModel(), new Customer { Id = "owner-1" },
            false);

        _customerServiceMock.Verify(c => c.GetAllCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "owner-1", "se-1", It.IsAny<string[]>(),
            It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>()), Times.Once);
    }

    [TestMethod]
    public async Task PrepareCustomerModel_SubAccount_LinksTheOwnerInReach()
    {
        var owner = new Customer { Id = "owner-1", Email = "boss@firm.com" };
        _customerServiceMock.Setup(c => c.GetCustomerById("owner-1")).ReturnsAsync(owner);

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model,
            new Customer { Id = "sub-1", OwnerId = "owner-1" }, false);

        Assert.AreEqual("owner-1", model.OwnerCustomerId);
        Assert.AreEqual("boss@firm.com", model.OwnerCustomerEmail);
        Assert.AreEqual(0, model.SubAccounts.Count);
    }

    [TestMethod]
    public async Task PrepareCustomerModel_SubAccount_OwnerOutOfReach_IsNotShown()
    {
        var owner = new Customer { Id = "owner-1", Email = "boss@other-store.com" };
        _customerServiceMock.Setup(c => c.GetCustomerById("owner-1")).ReturnsAsync(owner);
        _scopeMock.Setup(s => s.HasAccess(owner)).ReturnsAsync(false);

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model,
            new Customer { Id = "sub-1", OwnerId = "owner-1" }, false);

        Assert.IsTrue(string.IsNullOrEmpty(model.OwnerCustomerId));
        Assert.IsTrue(string.IsNullOrEmpty(model.OwnerCustomerEmail));
    }

    #endregion

    private void SetupGetAllCustomers(IPagedList<Customer> result)
    {
        _customerServiceMock
            .Setup(c => c.GetAllCustomers(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>(),
                It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>(),
                It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(result);
    }

    [TestMethod]
    public async Task UpdateCustomerModel_MapsTradeCredit()
    {
        var customer = new Customer { StoreId = "store-1" };
        var model = new CustomerModel {
            Email = "boss@firm.com", StoreId = "store-1", CreditLimit = 1000, PaymentTermDays = 30
        };

        var result = await _customerViewModelService.UpdateCustomerModel(customer, model);

        Assert.AreEqual(1000, result.CreditLimit);
        Assert.AreEqual(30, result.PaymentTermDays);
    }

    [TestMethod]
    public async Task UpdateCustomerModel_SubAccount_ClearsTradeCredit()
    {
        //the Store panel does not post the owner, so a crafted post of a sub-account reaches the service
        var customer = new Customer { StoreId = "store-1", OwnerId = "owner-1", CreditLimit = 5 };
        _customerServiceMock.Setup(c => c.GetCustomerById("owner-1"))
            .ReturnsAsync(new Customer { Id = "owner-1", Email = "boss@firm.com" });
        var model = new CustomerModel {
            Email = "buyer@firm.com", StoreId = "store-1", Owner = "boss@firm.com", CreditLimit = 1000,
            PaymentTermDays = 30
        };

        var result = await _customerViewModelService.UpdateCustomerModel(customer, model);

        Assert.AreEqual("owner-1", result.OwnerId);
        Assert.IsNull(result.CreditLimit);
        Assert.IsNull(result.PaymentTermDays);
    }

    [TestMethod]
    public async Task InsertCustomerModel_MapsTradeCredit()
    {
        var model = new CustomerModel { StoreId = "store-1", CreditLimit = 500, PaymentTermDays = 14 };

        var result = await _customerViewModelService.InsertCustomerModel(model);

        Assert.AreEqual(500, result.CreditLimit);
        Assert.AreEqual(14, result.PaymentTermDays);
    }

    [TestMethod]
    public async Task PrepareCustomerModel_FillsOutstandingForOwner()
    {
        _companyCreditServiceMock.Setup(c => c.GetOutstandingAmount("owner-1")).ReturnsAsync(250);
        _companyCreditServiceMock.Setup(c => c.HasOverdue("owner-1", It.IsAny<DateTime>())).ReturnsAsync(true);
        _priceFormatterMock.Setup(p => p.FormatPrice(250, It.Is<Grand.Domain.Directory.Currency>(c => c.CurrencyCode == "EUR")))
            .Returns("EUR 250.00");
        SetupGetAllCustomers(new PagedList<Customer>());

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model,
            new Customer { Id = "owner-1", CreditLimit = 1000, PaymentTermDays = 30 }, false);

        Assert.AreEqual(1000, model.CreditLimit);
        Assert.AreEqual(30, model.PaymentTermDays);
        Assert.AreEqual("EUR 250.00", model.OutstandingAmount);
        Assert.IsTrue(model.HasOverdue);
    }

    [TestMethod]
    public async Task PrepareCustomerModel_SubAccount_NoOutstanding()
    {
        _customerServiceMock.Setup(c => c.GetCustomerById("owner-1")).ReturnsAsync(new Customer { Id = "owner-1" });

        var model = new CustomerModel();
        await _customerViewModelService.PrepareCustomerModel(model,
            new Customer { Id = "sub-1", OwnerId = "owner-1" }, false);

        Assert.IsNull(model.OutstandingAmount);
        _companyCreditServiceMock.Verify(c => c.GetOutstandingAmount(It.IsAny<string>()), Times.Never);
    }
}
