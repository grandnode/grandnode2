using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Stores;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class ProductPriceViewModelServiceTests
{
    private Mock<IGroupService> _groupServiceMock;
    private ProductPriceViewModelService _productPriceViewModelService;
    private Mock<IAdminDataScope<Product>> _scopeMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<IRequestTranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        _groupServiceMock = new Mock<IGroupService>();
        _storeServiceMock = new Mock<IStoreService>();
        _translationServiceMock = new Mock<IRequestTranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var currencyServiceMock = new Mock<ICurrencyService>();
        currencyServiceMock.Setup(c => c.GetCurrencyById(It.IsAny<string>())).ReturnsAsync((Currency)null);
        currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(new List<Currency>());

        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());

        _groupServiceMock.Setup(g => g.GetAllCustomerGroups(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<CustomerGroup>());

        // Default: Admin's Global scope - no default store/vendor, store dropdown shows.
        _scopeMock = new Mock<IAdminDataScope<Product>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Admin");
        _scopeMock.Setup(s => s.ShowStoreSelector).Returns(true);
        _scopeMock.Setup(s => s.DefaultVendorId).Returns((string)null);

        _productPriceViewModelService = new ProductPriceViewModelService(
            currencyServiceMock.Object,
            new Mock<IDateTimeService>().Object,
            _translationServiceMock.Object,
            _groupServiceMock.Object,
            _storeServiceMock.Object,
            _scopeMock.Object);
    }

    [TestMethod]
    public async Task PrepareTierPriceModel_VendorScope_HidesStoreDropdown()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");
        _scopeMock.Setup(s => s.ShowStoreSelector).Returns(false);
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store> {
            new() { Id = "store1", Shortcut = "Store 1" }
        });

        var model = new ProductModel.TierPriceModel();
        await _productPriceViewModelService.PrepareTierPriceModel(model);

        Assert.IsFalse(model.AvailableStores.Any(),
            "Vendor doesn't pick stores - no dropdown should be populated at all, matching the original Vendor service's PrepareTierPriceModel.");
    }

    [TestMethod]
    public async Task PrepareTierPriceModel_GlobalScope_IncludesStoreDropdown()
    {
        // Default Setup() scope: Admin's Global scope (DefaultStoreId null, ShowStoreSelector true).
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store> {
            new() { Id = "store1", Shortcut = "Store 1" }
        });

        var model = new ProductModel.TierPriceModel();
        await _productPriceViewModelService.PrepareTierPriceModel(model);

        Assert.IsTrue(model.AvailableStores.Any(x => x.Value == "store1"),
            "Admin should offer a store dropdown for tier prices.");
    }
}
