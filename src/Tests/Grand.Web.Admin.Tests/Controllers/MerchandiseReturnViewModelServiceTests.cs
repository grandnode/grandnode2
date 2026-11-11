using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
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

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_ForwardsScopeDefaultStoreIdAndVendorIdToService()
    {
        // Regression test for coverage gap noted in Task 4: verify that scope.DefaultStoreId
        // and scope.DefaultVendorId actually reach IMerchandiseReturnService.SearchMerchandiseReturns
        // with the exact values (not just It.IsAny).
        var merchandiseReturnServiceMock = new Mock<IMerchandiseReturnService>();
        // Method signature: SearchMerchandiseReturns(storeId, customerId, orderItemId, vendorId, ownerId, rs, pageIndex, pageSize, createdFromUtc, createdToUtc)
        merchandiseReturnServiceMock
            .Setup(m => m.SearchMerchandiseReturns(
                "store-42", It.IsAny<string>(), It.IsAny<string>(), "vendor-99", It.IsAny<string>(),
                It.IsAny<MerchandiseReturnStatus?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new PagedList<MerchandiseReturn>());

        var contextAccessorMock = new Mock<IContextAccessor>();
        var scopeMock = new Mock<IAdminDataScope<MerchandiseReturn>>();
        scopeMock.Setup(s => s.DefaultStoreId).Returns("store-42");
        scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor-99");

        var service = BuildWithCustomMocks(
            merchandiseReturnServiceMock.Object,
            contextAccessorMock.Object,
            scopeMock.Object);

        var model = new MerchandiseReturnListModel();
        await service.PrepareMerchandiseReturnModel(model, 1, 10);

        // Verify exact call with scope values, not It.IsAny
        merchandiseReturnServiceMock.Verify(m => m.SearchMerchandiseReturns(
            "store-42", It.IsAny<string>(), It.IsAny<string>(), "vendor-99", It.IsAny<string>(),
            It.IsAny<MerchandiseReturnStatus?>(), 0, 10,
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Once);
    }

    private static MerchandiseReturnViewModelService Build(ICustomerService customerService,
        IContextAccessor contextAccessor, CustomerConfig customerConfig)
    {
        var orderServiceMock = new Mock<IOrderService>();
        var productServiceMock = new Mock<IProductService>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        dateTimeServiceMock.Setup(d => d.CurrentTimeZone).Returns(TimeZoneInfo.Utc);
        var translationServiceMock = new Mock<IRequestTranslationService>();
        var merchandiseReturnMessageProviderServiceMock = new Mock<IMerchandiseReturnMessageProviderService>();
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
            merchandiseReturnMessageProviderServiceMock.Object,
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
            scopeMock.Object,
            new Mock<IMediator>().Object,
            new Mock<IResourceLock>().Object,
            new Mock<IInventoryDocumentService>().Object);
    }

    private static MerchandiseReturnViewModelService BuildWithCustomMocks(
        IMerchandiseReturnService merchandiseReturnService,
        IContextAccessor contextAccessor,
        IAdminDataScope<MerchandiseReturn> scope)
    {
        var orderServiceMock = new Mock<IOrderService>();
        var productServiceMock = new Mock<IProductService>();
        var customerServiceMock = new Mock<ICustomerService>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        dateTimeServiceMock.Setup(d => d.CurrentTimeZone).Returns(TimeZoneInfo.Utc);
        var translationServiceMock = new Mock<IRequestTranslationService>();
        var merchandiseReturnMessageProviderServiceMock = new Mock<IMerchandiseReturnMessageProviderService>();
        var languageSettings = new LanguageSettings();
        var priceFormatterMock = new Mock<IPriceFormatter>();
        var addressSettings = new AddressSettings();
        var countryServiceMock = new Mock<ICountryService>();
        var addressAttributeServiceMock = new Mock<IAddressAttributeService>();
        var addressAttributeParserMock = new Mock<IAddressAttributeParser>();
        var downloadServiceMock = new Mock<IDownloadService>();
        var orderSettings = new OrderSettings();
        var enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        var customerConfig = new CustomerConfig();

        return new MerchandiseReturnViewModelService(
            orderServiceMock.Object,
            productServiceMock.Object,
            customerServiceMock.Object,
            dateTimeServiceMock.Object,
            translationServiceMock.Object,
            merchandiseReturnMessageProviderServiceMock.Object,
            languageSettings,
            merchandiseReturnService,
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
            scope,
            new Mock<IMediator>().Object,
            new Mock<IResourceLock>().Object,
            new Mock<IInventoryDocumentService>().Object);
    }

    private static MerchandiseReturn RejectedReturn(int quantity)
    {
        var merchandiseReturn = new MerchandiseReturn
            { Id = "r1", OrderId = "o1", MerchandiseReturnStatus = MerchandiseReturnStatus.Rejected };
        merchandiseReturn.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "a", Quantity = quantity });
        return merchandiseReturn;
    }

    private static (MerchandiseReturnViewModelService service, Mock<IMerchandiseReturnService> merchandiseReturnService,
        Mock<IMediator> mediator, Mock<IResourceLock> resourceLock) BuildForUpdate(int returnable,
        MerchandiseReturn stored = null, bool received = false)
    {
        var orderServiceMock = new Mock<IOrderService>();
        orderServiceMock.Setup(x => x.GetOrderById("o1")).ReturnsAsync(new Order { Id = "o1" });
        var merchandiseReturnServiceMock = new Mock<IMerchandiseReturnService>();
        if (stored != null)
            merchandiseReturnServiceMock.Setup(x => x.GetMerchandiseReturnById(stored.Id)).ReturnsAsync(stored);
        var inventoryDocumentServiceMock = new Mock<IInventoryDocumentService>();
        inventoryDocumentServiceMock.Setup(x => x.Search(It.Is<InventoryDocumentSearch>(s =>
                    s.MerchandiseReturnId == "r1" && s.Type == InventoryDocumentType.ReturnReceipt &&
                    s.Status == InventoryDocumentStatus.Posted), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<InventoryDocument>(received ? [new InventoryDocument()] : [], 0, 1));
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(x => x.Send(It.Is<GetReturnableQuantitiesQuery>(q => q.ForStaff),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { { "a", returnable } });
        var resourceLockMock = new Mock<IResourceLock>();
        resourceLockMock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        var service = new MerchandiseReturnViewModelService(
            orderServiceMock.Object,
            new Mock<IProductService>().Object,
            new Mock<ICustomerService>().Object,
            new Mock<IDateTimeService>().Object,
            new Mock<IRequestTranslationService>().Object,
            new Mock<IMerchandiseReturnMessageProviderService>().Object,
            new LanguageSettings(),
            merchandiseReturnServiceMock.Object,
            new Mock<IPriceFormatter>().Object,
            new AddressSettings(),
            new Mock<ICountryService>().Object,
            new Mock<IAddressAttributeService>().Object,
            new Mock<IAddressAttributeParser>().Object,
            new Mock<IDownloadService>().Object,
            new OrderSettings(),
            new Mock<IEnumTranslationService>().Object,
            new Mock<IContextAccessor>().Object,
            new CustomerConfig(),
            new Mock<IAdminDataScope<MerchandiseReturn>>().Object,
            mediatorMock.Object,
            resourceLockMock.Object,
            inventoryDocumentServiceMock.Object);
        return (service, merchandiseReturnServiceMock, mediatorMock, resourceLockMock);
    }

    private static MerchandiseReturnModel Model(MerchandiseReturnStatus status)
    {
        return new MerchandiseReturnModel { Id = "r1", MerchandiseReturnStatusId = (int)status };
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_RejectedSetBackAboveWhatIsLeft_SavesNothing()
    {
        //Arrange - another return took the units in the meantime: 1 left, the rejected return asks for 2
        var (service, merchandiseReturnService, _, _) = BuildForUpdate(1);
        var merchandiseReturn = RejectedReturn(2);

        //Act
        var result = await service.UpdateMerchandiseReturnModel(merchandiseReturn,
            Model(MerchandiseReturnStatus.Pending), []);

        //Assert
        Assert.IsNull(result.MerchandiseReturn);
        Assert.AreEqual("Orders.MerchandiseReturns.QuantityExceeded", result.Error);
        Assert.AreEqual(MerchandiseReturnStatus.Rejected, merchandiseReturn.MerchandiseReturnStatus);
        merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_RejectedSetBackWithinWhatIsLeft_SavesUnderTheOrderLock()
    {
        //Arrange
        var (service, merchandiseReturnService, _, resourceLock) = BuildForUpdate(2);
        var merchandiseReturn = RejectedReturn(2);

        //Act
        var result = await service.UpdateMerchandiseReturnModel(merchandiseReturn,
            Model(MerchandiseReturnStatus.Pending), []);

        //Assert
        Assert.AreEqual(MerchandiseReturnStatus.Pending, result.MerchandiseReturn.MerchandiseReturnStatus);
        merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(merchandiseReturn), Times.Once);
        resourceLock.Verify(x => x.Acquire(It.Is<IEnumerable<string>>(k => k.Single() == "MerchandiseReturn:o1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_StatusStillTakesNothing_DoesNotCheckTheQuantity()
    {
        //Arrange - rejected to cancelled takes nothing, so nothing has to be left
        var (service, merchandiseReturnService, mediator, _) = BuildForUpdate(0);

        //Act
        var result = await service.UpdateMerchandiseReturnModel(RejectedReturn(2),
            Model(MerchandiseReturnStatus.Cancelled), []);

        //Assert
        Assert.IsNull(result.Error);
        merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Once);
        mediator.Verify(x => x.Send(It.IsAny<GetReturnableQuantitiesQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_CopyLoadedBeforeAnotherSave_ChecksTheStoredStatus()
    {
        //Arrange - the form was loaded while the return was pending; meanwhile it was rejected and its units taken
        var stored = RejectedReturn(2);
        var (service, merchandiseReturnService, _, _) = BuildForUpdate(0, stored);
        var loaded = RejectedReturn(2);
        loaded.MerchandiseReturnStatus = MerchandiseReturnStatus.Pending;

        //Act
        var result = await service.UpdateMerchandiseReturnModel(loaded, Model(MerchandiseReturnStatus.Pending), []);

        //Assert
        Assert.AreEqual("Orders.MerchandiseReturns.QuantityExceeded", result.Error);
        merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_RejectingWhatIsInStock_SavesNothing()
    {
        //Arrange - a posted return receipt took the units in
        var stored = RejectedReturn(2);
        stored.MerchandiseReturnStatus = MerchandiseReturnStatus.Accepted;
        var (service, merchandiseReturnService, _, _) = BuildForUpdate(0, stored, received: true);

        //Act
        var result = await service.UpdateMerchandiseReturnModel(stored, Model(MerchandiseReturnStatus.Rejected), []);

        //Assert
        Assert.AreEqual("Orders.MerchandiseReturns.AlreadyReceived", result.Error);
        Assert.AreEqual(MerchandiseReturnStatus.Accepted, stored.MerchandiseReturnStatus);
        merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(It.IsAny<MerchandiseReturn>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_RejectingWhatIsNotInStock_Saves()
    {
        //Arrange
        var stored = RejectedReturn(2);
        stored.MerchandiseReturnStatus = MerchandiseReturnStatus.Pending;
        var (service, merchandiseReturnService, _, _) = BuildForUpdate(0, stored);

        //Act
        var result = await service.UpdateMerchandiseReturnModel(stored, Model(MerchandiseReturnStatus.Rejected), []);

        //Assert
        Assert.IsNull(result.Error);
        merchandiseReturnService.Verify(x => x.UpdateMerchandiseReturn(stored), Times.Once);
    }

    [TestMethod]
    public async Task DeleteMerchandiseReturn_DeletesUnderTheOrderLock()
    {
        //Arrange
        var (service, merchandiseReturnService, _, resourceLock) = BuildForUpdate(0);
        var merchandiseReturn = RejectedReturn(2);

        //Act
        await service.DeleteMerchandiseReturn(merchandiseReturn);

        //Assert
        merchandiseReturnService.Verify(x => x.DeleteMerchandiseReturn(merchandiseReturn), Times.Once);
        resourceLock.Verify(x => x.Acquire(It.Is<IEnumerable<string>>(k => k.Single() == "MerchandiseReturn:o1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
