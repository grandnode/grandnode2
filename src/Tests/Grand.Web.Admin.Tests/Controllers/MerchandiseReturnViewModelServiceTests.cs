using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class MerchandiseReturnViewModelServiceTests
{
    [TestMethod]
    public void InterfaceExposesRenamedMethod()
    {
        // Compile-time assertion: fails to build if PrepareReturnReqestListModel still exists
        // instead of PrepareReturnRequestListModel (spec §2.4 typo fix).
        IMerchandiseReturnViewModelService service = null;
        Func<MerchandiseReturnListModel> _ = () => service.PrepareReturnRequestListModel();
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_RegisterCustomersPerStore_ScopesEmailLookupToCurrentStore()
    {
        var customerServiceMock = new Mock<ICustomerService>();
        customerServiceMock
            .Setup(c => c.GetCustomerByEmail("test@example.com", "store-1"))
            .ReturnsAsync(new Customer { Id = "customer-1" });

        var storeContextMock = new Mock<IStoreContext>();
        storeContextMock.Setup(s => s.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "store-1" });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.StoreContext).Returns(storeContextMock.Object);

        var customerConfig = new CustomerConfig { RegisterCustomersPerStore = true };

        var service = Build(customerServiceMock.Object, contextAccessorMock.Object, customerConfig);

        var model = new MerchandiseReturnListModel { SearchCustomerEmail = "Test@Example.com" };
        await service.PrepareMerchandiseReturnModel(model, 1, 10);

        // ToLowerInvariant() is kept (spec §2.4: AdminShared's existing lower-casing survives;
        // Vendor's omission of it was the drift, not the other way around).
        customerServiceMock.Verify(c => c.GetCustomerByEmail("test@example.com", "store-1"), Times.Once);
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_RegisterCustomersPerStoreDisabled_LooksUpAcrossAllStores()
    {
        var customerServiceMock = new Mock<ICustomerService>();
        customerServiceMock
            .Setup(c => c.GetCustomerByEmail("test@example.com", ""))
            .ReturnsAsync(new Customer { Id = "customer-1" });

        var contextAccessorMock = new Mock<IContextAccessor>();
        var customerConfig = new CustomerConfig { RegisterCustomersPerStore = false };

        var service = Build(customerServiceMock.Object, contextAccessorMock.Object, customerConfig);

        var model = new MerchandiseReturnListModel { SearchCustomerEmail = "Test@Example.com" };
        await service.PrepareMerchandiseReturnModel(model, 1, 10);

        customerServiceMock.Verify(c => c.GetCustomerByEmail("test@example.com", ""), Times.Once);
    }

    private static MerchandiseReturnViewModelService Build(ICustomerService customerService,
        IContextAccessor contextAccessor, CustomerConfig customerConfig)
    {
        var orderServiceMock = new Mock<IOrderService>();
        var productServiceMock = new Mock<IProductService>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        dateTimeServiceMock.Setup(d => d.CurrentTimeZone).Returns(TimeZoneInfo.Utc);
        var translationServiceMock = new Mock<ITranslationService>();
        var messageProviderServiceMock = new Mock<IMessageProviderService>();
        var languageSettings = new LanguageSettings();
        var merchandiseReturnServiceMock = new Mock<IMerchandiseReturnService>();
        merchandiseReturnServiceMock
            .Setup(m => m.SearchMerchandiseReturns(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new PagedList<MerchandiseReturn>());
        var priceFormatterMock = new Mock<IPriceFormatter>();
        var addressSettings = new AddressSettings();
        var countryServiceMock = new Mock<ICountryService>();
        var addressAttributeServiceMock = new Mock<IAddressAttributeService>();
        var addressAttributeParserMock = new Mock<IAddressAttributeParser>();
        var downloadServiceMock = new Mock<IDownloadService>();
        var orderSettings = new OrderSettings();
        var enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        var scopeMock = new Mock<IAdminDataScope<MerchandiseReturn>>();
        scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        scopeMock.Setup(s => s.DefaultVendorId).Returns((string)null);

        return new MerchandiseReturnViewModelService(
            orderServiceMock.Object,
            productServiceMock.Object,
            customerService,
            dateTimeServiceMock.Object,
            translationServiceMock.Object,
            messageProviderServiceMock.Object,
            languageSettings,
            merchandiseReturnServiceMock.Object,
            priceFormatterMock.Object,
            addressSettings,
            countryServiceMock.Object,
            addressAttributeServiceMock.Object,
            addressAttributeParserMock.Object,
            downloadServiceMock.Object,
            orderSettings,
            enumTranslationServiceMock.Object,
            contextAccessor,
            customerConfig,
            scopeMock.Object);
    }
}
