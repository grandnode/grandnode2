using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
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
using Microsoft.Extensions.Logging;
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
            new Mock<IMerchandiseReturnWorkflowService>().Object,
            new Mock<IMerchandiseReturnLabelService>().Object,
            new Mock<IReturnShippingService>().Object,
            new Mock<ICurrencyService>().Object,
            new Mock<ILogger<MerchandiseReturnViewModelService>>().Object);
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
            new Mock<IMerchandiseReturnWorkflowService>().Object,
            new Mock<IMerchandiseReturnLabelService>().Object,
            new Mock<IReturnShippingService>().Object,
            new Mock<ICurrencyService>().Object,
            new Mock<ILogger<MerchandiseReturnViewModelService>>().Object);
    }

    private static (MerchandiseReturnViewModelService service, Mock<IMerchandiseReturnWorkflowService> workflow)
        BuildForUpdate()
    {
        var orderServiceMock = new Mock<IOrderService>();
        orderServiceMock.Setup(x => x.GetOrderById("o1")).ReturnsAsync(new Order { Id = "o1" });
        var enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        enumTranslationServiceMock.Setup(x => x.GetTranslationEnum(MerchandiseReturnStatus.Accepted))
            .Returns("Accepted (translated)");
        var workContext = new Mock<IWorkContext>();
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        var workflowMock = new Mock<IMerchandiseReturnWorkflowService>();
        workflowMock.Setup(x => x.Update(It.IsAny<string>(), It.IsAny<Action<MerchandiseReturn>>()))
            .ReturnsAsync((string)null);
        var labelServiceMock = new Mock<IMerchandiseReturnLabelService>();
        var returnShippingServiceMock = new Mock<IReturnShippingService>();
        var currencyServiceMock = new Mock<ICurrencyService>();

        var service = new MerchandiseReturnViewModelService(
            orderServiceMock.Object,
            new Mock<IProductService>().Object,
            new Mock<ICustomerService>().Object,
            new Mock<IDateTimeService>().Object,
            new Mock<IRequestTranslationService>().Object,
            new Mock<IMerchandiseReturnMessageProviderService>().Object,
            new Mock<IMerchandiseReturnService>().Object,
            new Mock<IPriceFormatter>().Object,
            new AddressSettings(),
            new Mock<ICountryService>().Object,
            new Mock<IAddressAttributeService>().Object,
            new Mock<IAddressAttributeParser>().Object,
            new Mock<IDownloadService>().Object,
            new OrderSettings(),
            enumTranslationServiceMock.Object,
            contextAccessor.Object,
            new CustomerConfig(),
            new Mock<IAdminDataScope<MerchandiseReturn>>().Object,
            workflowMock.Object,
            labelServiceMock.Object,
            returnShippingServiceMock.Object,
            currencyServiceMock.Object,
            new Mock<ILogger<MerchandiseReturnViewModelService>>().Object);
        return (service, workflowMock);
    }

    private static MerchandiseReturn StoredReturn(MerchandiseReturnStatus status)
    {
        return new MerchandiseReturn { Id = "r1", OrderId = "o1", MerchandiseReturnStatus = status };
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_WithChangeStatusTo_SavesFieldsThenChangesStatus()
    {
        //Arrange - the status button posts the whole form: what was typed is saved before the status changes
        var (service, workflow) = BuildForUpdate();
        var sequence = new MockSequence();
        workflow.InSequence(sequence).Setup(x => x.Update("r1", It.IsAny<Action<MerchandiseReturn>>()))
            .ReturnsAsync((string)null);
        workflow.InSequence(sequence).Setup(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Rejected, "why"))
            .ReturnsAsync((string)null);

        //Act
        var result = await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Pending),
            new MerchandiseReturnModel {
                Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Rejected, ChangeStatusNote = "why"
            }, []);

        //Assert
        Assert.IsNull(result.Error);
        workflow.Verify(x => x.Update("r1", It.IsAny<Action<MerchandiseReturn>>()), Times.Once);
        workflow.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Rejected, "why"), Times.Once);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_ReasonTypedThenOtherButton_SendsNoNote()
    {
        //Arrange - the reason box was opened and typed in, then Accept was pressed: the reason is not the customer's
        var (service, workflow) = BuildForUpdate();

        //Act
        await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Pending),
            new MerchandiseReturnModel {
                Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Accepted, ChangeStatusNote = "damaged by you"
            }, []);

        //Assert
        workflow.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Accepted, null), Times.Once);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_AppliesTheFormFields()
    {
        //Arrange
        var (service, workflow) = BuildForUpdate();
        Action<MerchandiseReturn> apply = null;
        workflow.Setup(x => x.Update("r1", It.IsAny<Action<MerchandiseReturn>>()))
            .Callback<string, Action<MerchandiseReturn>>((_, a) => apply = a)
            .ReturnsAsync((string)null);

        //Act
        await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Pending),
            new MerchandiseReturnModel {
                Id = "r1", StaffNotes = "notes", CustomerComments = "comments", ExternalId = "ext"
            }, []);
        var stored = StoredReturn(MerchandiseReturnStatus.Pending);
        apply(stored);

        //Assert
        Assert.AreEqual("notes", stored.StaffNotes);
        Assert.AreEqual("comments", stored.CustomerComments);
        Assert.AreEqual("ext", stored.ExternalId);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_WithoutChangeStatusTo_NeverChangesStatus()
    {
        var (service, workflow) = BuildForUpdate();

        var result = await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Pending),
            new MerchandiseReturnModel { Id = "r1" }, []);

        Assert.IsNull(result.Error);
        workflow.Verify(x => x.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
            It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_StatusChangeRefused_ReturnsItsError()
    {
        var (service, workflow) = BuildForUpdate();
        workflow.Setup(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Completed, null))
            .ReturnsAsync(MerchandiseReturnErrors.StatusChangeNotAllowed);

        var result = await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Pending),
            new MerchandiseReturnModel { Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Completed }, []);

        Assert.AreEqual(MerchandiseReturnErrors.StatusChangeNotAllowed, result.Error);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_SaveRefused_DoesNotChangeStatus()
    {
        var (service, workflow) = BuildForUpdate();
        workflow.Setup(x => x.Update("r1", It.IsAny<Action<MerchandiseReturn>>()))
            .ReturnsAsync(MerchandiseReturnErrors.Busy);

        var result = await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Pending),
            new MerchandiseReturnModel { Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Accepted }, []);

        Assert.AreEqual(MerchandiseReturnErrors.Busy, result.Error);
        workflow.Verify(x => x.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
            It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_FillsNextStatusesFromStoredStatus()
    {
        var (service, _) = BuildForUpdate();

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            StoredReturn(MerchandiseReturnStatus.Accepted), true);

        CollectionAssert.AreEqual(new[] { 15, 40, 20, 30 }, model.NextStatuses.ToArray());
        Assert.AreEqual("Accepted (translated)", model.MerchandiseReturnStatusStr);
    }

    [TestMethod]
    public async Task DeleteMerchandiseReturn_DeletesThroughTheWorkflow()
    {
        var (service, workflow) = BuildForUpdate();
        workflow.Setup(x => x.Delete("r1")).ReturnsAsync(MerchandiseReturnErrors.Busy);

        var error = await service.DeleteMerchandiseReturn(StoredReturn(MerchandiseReturnStatus.Rejected));

        Assert.AreEqual(MerchandiseReturnErrors.Busy, error);
        workflow.Verify(x => x.Delete("r1"), Times.Once);
    }

    // --- Return method card ----------------------------------------------------------------------

    private sealed class ReturnShippingMocks
    {
        public Mock<IPriceFormatter> PriceFormatter { get; } = new();
        public Mock<IRequestTranslationService> Translation { get; } = new();
        public Mock<IReturnShippingService> ReturnShipping { get; } = new();
        public Mock<ICurrencyService> Currency { get; } = new();
        public Mock<IMerchandiseReturnLabelService> LabelService { get; } = new();
        public Mock<IMerchandiseReturnWorkflowService> Workflow { get; } = new();
        public Mock<IShipmentTracker> Tracker { get; } = new();
        public Currency OrderCurrency { get; } = new() { CurrencyCode = "EUR" };
    }

    private static (MerchandiseReturnViewModelService service, ReturnShippingMocks mocks) BuildForReturnShipping()
    {
        var mocks = new ReturnShippingMocks();
        var orderServiceMock = new Mock<IOrderService>();
        orderServiceMock.Setup(x => x.GetOrderById("o1"))
            .ReturnsAsync(new Order { Id = "o1", CustomerCurrencyCode = "EUR" });
        var workContext = new Mock<IWorkContext>();
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        var scopeMock = new Mock<IAdminDataScope<MerchandiseReturn>>();
        scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");
        mocks.Currency.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(mocks.OrderCurrency);
        mocks.Translation.Setup(x => x.GetResource(It.IsAny<string>())).Returns<string>(key => key);
        var provider = new Mock<IReturnShippingProvider>();
        provider.Setup(x => x.FriendlyName).Returns("Carrier");
        provider.Setup(x => x.ShipmentTracker).Returns(mocks.Tracker.Object);
        mocks.ReturnShipping.Setup(x => x.LoadProviderBySystemName("ReturnShipping.Carrier")).Returns(provider.Object);
        mocks.Workflow.Setup(x => x.Update(It.IsAny<string>(), It.IsAny<Action<MerchandiseReturn>>()))
            .ReturnsAsync((string)null);

        //the edit form's pickup address lists the countries and the address attributes
        var countryServiceMock = new Mock<ICountryService>();
        countryServiceMock.Setup(x => x.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<Country>());
        var addressAttributeServiceMock = new Mock<IAddressAttributeService>();
        addressAttributeServiceMock.Setup(x => x.GetAllAddressAttributes()).ReturnsAsync(new List<AddressAttribute>());
        addressAttributeServiceMock.Setup(x => x.GetAllAddressAttributes(It.IsAny<string>()))
            .ReturnsAsync(new List<AddressAttribute>());

        var service = new MerchandiseReturnViewModelService(
            orderServiceMock.Object,
            new Mock<IProductService>().Object,
            new Mock<ICustomerService>().Object,
            new Mock<IDateTimeService>().Object,
            mocks.Translation.Object,
            new Mock<IMerchandiseReturnMessageProviderService>().Object,
            new Mock<IMerchandiseReturnService>().Object,
            mocks.PriceFormatter.Object,
            new AddressSettings(),
            countryServiceMock.Object,
            addressAttributeServiceMock.Object,
            new Mock<IAddressAttributeParser>().Object,
            new Mock<IDownloadService>().Object,
            new OrderSettings(),
            new Mock<IEnumTranslationService>().Object,
            contextAccessor.Object,
            new CustomerConfig(),
            scopeMock.Object,
            mocks.Workflow.Object,
            mocks.LabelService.Object,
            mocks.ReturnShipping.Object,
            mocks.Currency.Object,
            new Mock<ILogger<MerchandiseReturnViewModelService>>().Object);
        return (service, mocks);
    }

    private static MerchandiseReturn ReturnWithShipping(ReturnLabelStatus labelStatus,
        MerchandiseReturnStatus status = MerchandiseReturnStatus.Pending)
    {
        return new MerchandiseReturn {
            Id = "r1", OrderId = "o1", MerchandiseReturnStatus = status,
            ReturnShipping = new MerchandiseReturnShipping {
                ProviderSystemName = "ReturnShipping.Carrier", OptionName = "Parcel locker",
                Cost = 10, CurrencyRate = 4.5, LabelStatus = labelStatus, LabelDownloadId = "d1",
                LabelError = "carrier down", TrackingNumber = "T1", ReturnCode = "RC",
                DropOffPoint = new ReturnDropOffPoint {
                    Name = "Locker 7",
                    Address = new Address { Address1 = "Main 1", ZipPostalCode = "00-001", City = "Town" }
                }
            }
        };
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_ReturnShipping_FillsTheCard()
    {
        var (service, mocks) = BuildForReturnShipping();
        mocks.PriceFormatter.Setup(x => x.FormatPrice(45, mocks.OrderCurrency)).Returns("45,00 EUR");
        mocks.Tracker.Setup(x => x.GetShipmentEvents("T1"))
            .ReturnsAsync(new List<ShipmentStatusEvent> { new() { EventName = "Posted", Location = "Town" } });

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            ReturnWithShipping(ReturnLabelStatus.Created), false);

        var card = model.ReturnShipping;
        Assert.IsNotNull(card);
        Assert.AreEqual("Carrier", card.MethodName);
        Assert.AreEqual("Parcel locker", card.OptionName);
        //the cost in the order's currency, at the order's rate
        Assert.AreEqual("45,00 EUR", card.Cost);
        Assert.AreEqual("Locker 7, Main 1, 00-001, Town", card.DropOffPoint);
        Assert.AreEqual("T1", card.TrackingNumber);
        Assert.AreEqual("T1", card.ShownTrackingNumber);
        Assert.IsTrue(card.HasLabel);
        Assert.IsFalse(card.CanRetryLabel);
        Assert.AreEqual("Vendor.Orders.MerchandiseReturns.ReturnShipping.LabelStatus.Created", card.LabelStatus);
        Assert.AreEqual("carrier down", card.LabelError);
        Assert.AreEqual("Posted", card.TrackingEvents.Single().EventName);
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_ReturnShippingFree_CostShownFree()
    {
        var (service, _) = BuildForReturnShipping();
        var merchandiseReturn = ReturnWithShipping(ReturnLabelStatus.Created);
        merchandiseReturn.ReturnShipping.Cost = 0;

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(), merchandiseReturn,
            false);

        Assert.AreEqual("Vendor.Orders.MerchandiseReturns.ReturnShipping.Free", model.ReturnShipping.Cost);
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_NoReturnMethod_NoCard()
    {
        var (service, _) = BuildForReturnShipping();

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            new MerchandiseReturn { Id = "r1", OrderId = "o1" }, false);

        Assert.IsNull(model.ReturnShipping);
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_ListRow_AsksNoCarrier()
    {
        var (service, mocks) = BuildForReturnShipping();

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            ReturnWithShipping(ReturnLabelStatus.Created), true);

        Assert.IsNull(model.ReturnShipping);
        mocks.Tracker.Verify(x => x.GetShipmentEvents(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_TrackerThrows_KeepsThePageWithoutEvents()
    {
        var (service, mocks) = BuildForReturnShipping();
        mocks.Tracker.Setup(x => x.GetShipmentEvents("T1")).ThrowsAsync(new HttpRequestException("down"));

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            ReturnWithShipping(ReturnLabelStatus.Failed), false);

        Assert.IsNotNull(model.ReturnShipping);
        Assert.AreEqual(0, model.ReturnShipping.TrackingEvents.Count);
        Assert.IsTrue(model.ReturnShipping.CanRetryLabel);
        Assert.IsFalse(model.ReturnShipping.HasLabel);
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_FailedLabelOfRejectedReturn_CannotRetry()
    {
        var (service, _) = BuildForReturnShipping();

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            ReturnWithShipping(ReturnLabelStatus.Failed, MerchandiseReturnStatus.Rejected), false);

        Assert.IsFalse(model.ReturnShipping.CanRetryLabel);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_TrackingNumberSaveRefused_DoesNotChangeStatus()
    {
        var (service, mocks) = BuildForReturnShipping();
        mocks.LabelService.Setup(x => x.UpdateShipping("r1", It.IsAny<Action<MerchandiseReturnShipping>>(),
            It.IsAny<Func<MerchandiseReturn, bool>>())).ReturnsAsync(MerchandiseReturnErrors.Busy);

        var result = await service.UpdateMerchandiseReturnModel(ReturnWithShipping(ReturnLabelStatus.Created),
            new MerchandiseReturnModel {
                Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Received,
                ReturnShipping = new MerchandiseReturnModel.ReturnShippingModel {
                    TrackingNumber = "T2", ShownTrackingNumber = "T1"
                }
            }, []);

        Assert.AreEqual(MerchandiseReturnErrors.Busy, result.Error);
        mocks.Workflow.Verify(x => x.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
            It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_TrackingNumberCleared_SavedAsNone()
    {
        var (service, mocks) = BuildForReturnShipping();
        Action<MerchandiseReturnShipping> apply = null;
        mocks.LabelService.Setup(x => x.UpdateShipping("r1", It.IsAny<Action<MerchandiseReturnShipping>>(),
                It.IsAny<Func<MerchandiseReturn, bool>>()))
            .Callback<string, Action<MerchandiseReturnShipping>, Func<MerchandiseReturn, bool>>((_, a, _) => apply = a)
            .ReturnsAsync((string)null);

        await service.UpdateMerchandiseReturnModel(ReturnWithShipping(ReturnLabelStatus.Created),
            new MerchandiseReturnModel {
                Id = "r1",
                ReturnShipping = new MerchandiseReturnModel.ReturnShippingModel {
                    TrackingNumber = "  ", ShownTrackingNumber = "T1"
                }
            }, []);

        var shipping = new MerchandiseReturnShipping { TrackingNumber = "T1" };
        apply(shipping);
        Assert.IsNull(shipping.TrackingNumber);
    }
}
