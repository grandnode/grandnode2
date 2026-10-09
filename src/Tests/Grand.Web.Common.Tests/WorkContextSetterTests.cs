using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests;

[TestClass]
public class WorkContextSetterTests
{
    private Mock<IStoreService> _storeServiceMock;
    private TestableWorkContextSetter _setter;

    [TestInitialize]
    public void Setup()
    {
        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(s => s.GetStoreById("store-a")).ReturnsAsync(new Store { Id = "store-a", Shortcut = "A" });
        _storeServiceMock.Setup(s => s.GetStoreById("store-b")).ReturnsAsync(new Store { Id = "store-b", Shortcut = "B" });

        _setter = new TestableWorkContextSetter(
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IGrandAuthenticationService>(),
            Mock.Of<ICurrencyService>(),
            Mock.Of<ICustomerService>(),
            Mock.Of<ILanguageService>(),
            _storeServiceMock.Object,
            Mock.Of<IAclService>(),
            Mock.Of<IVendorService>(),
            new TaxSettings(),
            new AppConfig(),
            []);
    }

    [TestMethod]
    public async Task GetStoreManager_StaffStoreWithoutRegistrationStore_ReturnsStaffStore()
    {
        //an account created by the admin: no registration store, but assigned to manage store-a
        var customer = new Customer { StaffStoreId = "store-a", StoreId = "" };

        var store = await _setter.GetStoreManagerForTest(customer);

        Assert.IsNotNull(store);
        Assert.AreEqual("store-a", store.Id);
    }

    [TestMethod]
    public async Task GetStoreManager_StaffStoreDiffersFromRegistrationStore_ReturnsStaffStore()
    {
        var customer = new Customer { StaffStoreId = "store-a", StoreId = "store-b" };

        var store = await _setter.GetStoreManagerForTest(customer);

        Assert.AreEqual("store-a", store.Id);
    }

    [TestMethod]
    public async Task GetStoreManager_NoStaffStore_ReturnsNull()
    {
        //a regular customer registered in a store does not manage it
        var customer = new Customer { StaffStoreId = "", StoreId = "store-b" };

        var store = await _setter.GetStoreManagerForTest(customer);

        Assert.IsNull(store);
    }

    [TestMethod]
    public async Task GetStoreManager_StaffStoreNotFound_ReturnsNull()
    {
        var customer = new Customer { StaffStoreId = "deleted-store" };

        var store = await _setter.GetStoreManagerForTest(customer);

        Assert.IsNull(store);
    }

    [TestMethod]
    public async Task GetStoreManager_NullCustomer_ReturnsNull()
    {
        var store = await _setter.GetStoreManagerForTest(null);

        Assert.IsNull(store);
        _storeServiceMock.Verify(s => s.GetStoreById(It.IsAny<string>()), Times.Never);
    }

    private class TestableWorkContextSetter(
        IHttpContextAccessor httpContextAccessor,
        IGrandAuthenticationService authenticationService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        ILanguageService languageService,
        IStoreService storeService,
        IAclService aclService,
        IVendorService vendorService,
        TaxSettings taxSettings,
        AppConfig config,
        IEnumerable<IRequestCustomerResolver> requestCustomerResolvers)
        : WorkContextSetter(httpContextAccessor, authenticationService, currencyService, customerService,
            languageService, storeService, aclService, vendorService, taxSettings, config,
            requestCustomerResolvers)
    {
        public Task<Store> GetStoreManagerForTest(Customer customer) => GetStoreManager(customer);
    }
}
