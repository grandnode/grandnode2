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
            new Mock<IMerchandiseReturnWorkflowService>().Object);
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
            new Mock<IMerchandiseReturnWorkflowService>().Object);
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
            workflowMock.Object);
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
        workflow.InSequence(sequence).Setup(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Accepted, "why"))
            .ReturnsAsync((string)null);

        //Act
        var result = await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Pending),
            new MerchandiseReturnModel {
                Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Accepted, ChangeStatusNote = "why"
            }, []);

        //Assert
        Assert.IsNull(result.Error);
        workflow.Verify(x => x.Update("r1", It.IsAny<Action<MerchandiseReturn>>()), Times.Once);
        workflow.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Accepted, "why"), Times.Once);
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
}
