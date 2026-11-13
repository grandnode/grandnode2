using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Enums.Checkout;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
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
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
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
    public void PrepareReturnRequestListModel_FiltersAllStatusesAtFirst()
    {
        var (service, _) = BuildForUpdate();

        var model = service.PrepareReturnRequestListModel();

        Assert.AreEqual(-1, model.SearchMerchandiseReturnStatusId);
        Assert.AreEqual("-1", model.MerchandiseReturnStatus[0].Value);
        Assert.AreEqual("-1", model.MerchandiseReturnStatus.Single(x => x.Selected).Value);
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
            new Mock<IInventoryDocumentService>().Object,
            new Mock<IWarehouseService>().Object,
            new Mock<IAdminDataScope<Warehouse>>().Object,
            new Mock<IRequestPermissionService>().Object,
            new Mock<IPermissionService>().Object,
            new Mock<IMerchandiseReturnRefundService>().Object,
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
            new Mock<IInventoryDocumentService>().Object,
            new Mock<IWarehouseService>().Object,
            new Mock<IAdminDataScope<Warehouse>>().Object,
            new Mock<IRequestPermissionService>().Object,
            new Mock<IPermissionService>().Object,
            new Mock<IMerchandiseReturnRefundService>().Object,
            new Mock<ILogger<MerchandiseReturnViewModelService>>().Object);
    }

    private static (MerchandiseReturnViewModelService service, Mock<IMerchandiseReturnWorkflowService> workflow)
        BuildForUpdate(Mock<IInventoryDocumentService> documents = null,
            ManageInventoryMethod manageInventory = ManageInventoryMethod.ManageStock, string vendorId = null)
    {
        //the edit form's pickup address lists the countries and the address attributes
        var countryServiceMock = new Mock<ICountryService>();
        countryServiceMock.Setup(x => x.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<Country>());
        var addressAttributeServiceMock = new Mock<IAddressAttributeService>();
        addressAttributeServiceMock.Setup(x => x.GetAllAddressAttributes()).ReturnsAsync(new List<AddressAttribute>());
        addressAttributeServiceMock.Setup(x => x.GetAllAddressAttributes(It.IsAny<string>()))
            .ReturnsAsync(new List<AddressAttribute>());
        var orderServiceMock = new Mock<IOrderService>();
        var order = new Order { Id = "o1" };
        order.OrderItems.Add(new OrderItem { Id = "oi1", ProductId = "p1" });
        orderServiceMock.Setup(x => x.GetOrderById("o1")).ReturnsAsync(order);
        //the returned product, which a return receipt books unless it does not track stock
        var productServiceMock = new Mock<IProductService>();
        productServiceMock.Setup(x => x.GetProductByIdIncludeArch("p1"))
            .ReturnsAsync(new Product { Id = "p1", ManageInventoryMethodId = manageInventory });
        var scopeMock = new Mock<IAdminDataScope<MerchandiseReturn>>();
        scopeMock.Setup(x => x.DefaultVendorId).Returns(vendorId);
        var enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        enumTranslationServiceMock.Setup(x => x.GetTranslationEnum(MerchandiseReturnStatus.Accepted))
            .Returns("Accepted (translated)");
        enumTranslationServiceMock.Setup(x => x.ToSelectList(MerchandiseReturnStatus.Pending, false, null))
            .Returns(new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                new[] { new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Text = "Pending", Value = "0" } },
                "Value", "Text"));
        var workContext = new Mock<IWorkContext>();
        //the staff member the status change is noted for
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { Id = "staff", Email = "john@shop.com" });
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
            productServiceMock.Object,
            new Mock<ICustomerService>().Object,
            new Mock<IDateTimeService>().Object,
            new Mock<IRequestTranslationService>().Object,
            new Mock<IMerchandiseReturnMessageProviderService>().Object,
            new Mock<IMerchandiseReturnService>().Object,
            new Mock<IPriceFormatter>().Object,
            new AddressSettings(),
            countryServiceMock.Object,
            addressAttributeServiceMock.Object,
            new Mock<IAddressAttributeParser>().Object,
            new Mock<IDownloadService>().Object,
            new OrderSettings(),
            enumTranslationServiceMock.Object,
            contextAccessor.Object,
            new CustomerConfig(),
            scopeMock.Object,
            workflowMock.Object,
            labelServiceMock.Object,
            returnShippingServiceMock.Object,
            currencyServiceMock.Object,
            (documents ?? new Mock<IInventoryDocumentService>()).Object,
            new Mock<IWarehouseService>().Object,
            new Mock<IAdminDataScope<Warehouse>>().Object,
            new Mock<IRequestPermissionService>().Object,
            new Mock<IPermissionService>().Object,
            new Mock<IMerchandiseReturnRefundService>().Object,
            new Mock<ILogger<MerchandiseReturnViewModelService>>().Object);
        return (service, workflowMock);
    }

    private static MerchandiseReturn StoredReturn(MerchandiseReturnStatus status)
    {
        var merchandiseReturn = new MerchandiseReturn { Id = "r1", OrderId = "o1", MerchandiseReturnStatus = status };
        merchandiseReturn.MerchandiseReturnItems.Add(new MerchandiseReturnItem { OrderItemId = "oi1", Quantity = 1 });
        return merchandiseReturn;
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_WithChangeStatusTo_SavesFieldsThenChangesStatus()
    {
        //Arrange - the status button posts the whole form: what was typed is saved before the status changes
        var (service, workflow) = BuildForUpdate();
        var sequence = new MockSequence();
        workflow.InSequence(sequence).Setup(x => x.Update("r1", It.IsAny<Action<MerchandiseReturn>>()))
            .ReturnsAsync((string)null);
        workflow.InSequence(sequence).Setup(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Rejected, "why", It.IsAny<string>()))
            .ReturnsAsync((string)null);

        //Act
        var result = await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Pending),
            new MerchandiseReturnModel {
                Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Rejected, ChangeStatusNote = "why"
            }, []);

        //Assert
        Assert.IsNull(result.Error);
        workflow.Verify(x => x.Update("r1", It.IsAny<Action<MerchandiseReturn>>()), Times.Once);
        workflow.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Rejected, "why", It.IsAny<string>()), Times.Once);
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
        workflow.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Accepted, null, It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_StatusChange_NamesTheStaffMember()
    {
        var (service, workflow) = BuildForUpdate();

        await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Pending),
            new MerchandiseReturnModel { Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Accepted }, []);

        workflow.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Accepted, null, "john@shop.com"),
            Times.Once);
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
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_StatusChangeRefused_ReturnsItsError()
    {
        var (service, workflow) = BuildForUpdate();
        workflow.Setup(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Completed, null, It.IsAny<string>()))
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
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_FillsNextStatusesFromStoredStatus()
    {
        var (service, _) = BuildForUpdate();

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            StoredReturn(MerchandiseReturnStatus.Accepted), true);

        //received comes only from a posted return receipt: no button offers it
        CollectionAssert.AreEqual(new[] { 40, 20, 30 }, model.NextStatuses.ToArray());
        Assert.AreEqual("Accepted (translated)", model.MerchandiseReturnStatusStr);
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_Pending_DoesNotOfferReceived()
    {
        var (service, _) = BuildForUpdate();

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            StoredReturn(MerchandiseReturnStatus.Pending), true);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, model.NextStatuses.ToArray());
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_PostedReceived_RefusedAndNothingSaved()
    {
        //a crafted post: the panel does not offer it, the goods of a product tracking stock come back through a
        //return receipt
        var (service, workflow) = BuildForUpdate();

        var result = await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Accepted),
            new MerchandiseReturnModel { Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Received }, []);

        Assert.AreEqual(MerchandiseReturnErrors.StatusChangeNotAllowed, result.Error);
        workflow.Verify(x => x.Update(It.IsAny<string>(), It.IsAny<Action<MerchandiseReturn>>()), Times.Never);
        workflow.Verify(x => x.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_EditPage_NoPostedReceipt_DoesNotOfferReceived()
    {
        var (service, _) = BuildForUpdate();

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            StoredReturn(MerchandiseReturnStatus.Accepted), false);

        CollectionAssert.AreEqual(new[] { 40, 20, 30 }, model.NextStatuses.ToArray());
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_EditPage_ReceiptPostedButNotMarked_OffersReceived()
    {
        //the receipt was posted while the return was busy: MarkReceived refused, the staff finishes it by hand
        var documents = new Mock<IInventoryDocumentService>();
        documents.Setup(x => x.IsReturnReceived("r1")).ReturnsAsync(true);
        var (service, _) = BuildForUpdate(documents);

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            StoredReturn(MerchandiseReturnStatus.Accepted), false);

        CollectionAssert.AreEqual(new[] { 15, 40, 20, 30 }, model.NextStatuses.ToArray());
    }

    [TestMethod]
    public async Task PrepareMerchandiseReturnModel_EditPage_ProductDoesNotTrackStock_OffersReceived()
    {
        //no receipt can book a product that does not track stock: the staff marks the return received by hand
        var (service, _) = BuildForUpdate(manageInventory: ManageInventoryMethod.DontManageStock);

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            StoredReturn(MerchandiseReturnStatus.Accepted), false);

        CollectionAssert.AreEqual(new[] { 15, 40, 20, 30 }, model.NextStatuses.ToArray());
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_PostedReceived_VendorPanel_ChangesStatus()
    {
        //the vendor panel has no inventory documents to post a receipt with
        var (service, workflow) = BuildForUpdate(vendorId: "v1");

        var result = await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Accepted),
            new MerchandiseReturnModel { Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Received }, []);

        Assert.IsNull(result.Error);
        workflow.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Received, null, "john@shop.com"),
            Times.Once);
    }

    [TestMethod]
    public async Task UpdateMerchandiseReturnModel_PostedReceived_ReceiptPosted_ChangesStatus()
    {
        var documents = new Mock<IInventoryDocumentService>();
        documents.Setup(x => x.IsReturnReceived("r1")).ReturnsAsync(true);
        var (service, workflow) = BuildForUpdate(documents);

        var result = await service.UpdateMerchandiseReturnModel(StoredReturn(MerchandiseReturnStatus.Accepted),
            new MerchandiseReturnModel { Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Received }, []);

        Assert.IsNull(result.Error);
        workflow.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Received, null, "john@shop.com"),
            Times.Once);
    }

    // --- Return receipts -------------------------------------------------------------------------

    private sealed class ReturnReceiptMocks
    {
        public Mock<IInventoryDocumentService> Documents { get; } = new();
        public Mock<IWarehouseService> Warehouses { get; } = new();
        public Mock<IAdminDataScope<Warehouse>> WarehouseScope { get; } = new();
        public Mock<IRequestPermissionService> Permissions { get; } = new();
    }

    private static (MerchandiseReturnViewModelService service, ReturnReceiptMocks mocks) BuildForReturnReceipts(
        MerchandiseReturnReceipts receipts)
    {
        var mocks = new ReturnReceiptMocks();
        mocks.Documents.Setup(x => x.GetReturnReceipts("r1")).ReturnsAsync(receipts);
        mocks.Permissions.Setup(x => x.Authorize(PermissionSystemName.InventoryDocuments)).ReturnsAsync(true);
        var enumTranslation = new Mock<IEnumTranslationService>();
        enumTranslation.Setup(x => x.GetTranslationEnum(It.IsAny<InventoryDocumentStatus>()))
            .Returns<InventoryDocumentStatus>(status => status.ToString());

        var service = new MerchandiseReturnViewModelService(
            new Mock<IOrderService>().Object,
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
            enumTranslation.Object,
            new Mock<IContextAccessor>().Object,
            new CustomerConfig(),
            new Mock<IAdminDataScope<MerchandiseReturn>>().Object,
            new Mock<IMerchandiseReturnWorkflowService>().Object,
            new Mock<IMerchandiseReturnLabelService>().Object,
            new Mock<IReturnShippingService>().Object,
            new Mock<ICurrencyService>().Object,
            mocks.Documents.Object,
            mocks.Warehouses.Object,
            mocks.WarehouseScope.Object,
            mocks.Permissions.Object,
            new Mock<IPermissionService>().Object,
            new Mock<IMerchandiseReturnRefundService>().Object,
            new Mock<ILogger<MerchandiseReturnViewModelService>>().Object);
        return (service, mocks);
    }

    [TestMethod]
    public async Task PrepareReturnReceipts_QuantityLeft_OffersReceiveToStock()
    {
        var (service, _) = BuildForReturnReceipts(new MerchandiseReturnReceipts([], true));
        var model = new MerchandiseReturnModel { Id = "r1" };

        await service.PrepareReturnReceipts(model);

        Assert.IsTrue(model.CanReceiveToStock);
        Assert.AreEqual(0, model.ReturnReceipts.Count);
    }

    [TestMethod]
    public async Task PrepareReturnReceipts_AllOnReceipts_ListsThemWithoutReceiveToStock()
    {
        var (service, mocks) = BuildForReturnReceipts(new MerchandiseReturnReceipts([
            new ReturnReceiptSummary("d1", "PZZ/2026/0001", InventoryDocumentStatus.Posted, "", "w1"),
            new ReturnReceiptSummary("d2", "PZZ/2026/0002", InventoryDocumentStatus.Draft, "", "")
        ], false));
        var warehouse = new Warehouse { Id = "w1" };
        mocks.Warehouses.Setup(x => x.GetWarehouseById("w1")).ReturnsAsync(warehouse);
        mocks.WarehouseScope.Setup(x => x.HasAccess(warehouse)).ReturnsAsync(true);
        var model = new MerchandiseReturnModel { Id = "r1" };

        await service.PrepareReturnReceipts(model);

        Assert.IsFalse(model.CanReceiveToStock);
        Assert.AreEqual(2, model.ReturnReceipts.Count);
        Assert.AreEqual("d1", model.ReturnReceipts[0].Id);
        Assert.AreEqual("PZZ/2026/0001", model.ReturnReceipts[0].Number);
        Assert.AreEqual("Posted", model.ReturnReceipts[0].Status);
        Assert.IsTrue(model.ReturnReceipts[0].CanOpen);
        //a draft naming no warehouse yet opens, as the inventory document panel allows it
        Assert.AreEqual("Draft", model.ReturnReceipts[1].Status);
        Assert.IsTrue(model.ReturnReceipts[1].CanOpen);
    }

    [TestMethod]
    public async Task PrepareReturnReceipts_StorePanel_ReceiptOfAnotherStoreOrWarehouse_NotOpened()
    {
        var (service, mocks) = BuildForReturnReceipts(new MerchandiseReturnReceipts([
            new ReturnReceiptSummary("d1", "PZZ/2026/0001", InventoryDocumentStatus.Posted, "s2", "w2"),
            new ReturnReceiptSummary("d2", "PZZ/2026/0002", InventoryDocumentStatus.Posted, "s1", "shared"),
            new ReturnReceiptSummary("d3", "PZZ/2026/0003", InventoryDocumentStatus.Posted, "s1", "w1")
        ], false));
        mocks.WarehouseScope.Setup(x => x.GetDefaultStoreIdAsync()).ReturnsAsync("s1");
        var own = new Warehouse { Id = "w1", StoreId = "s1" };
        var shared = new Warehouse { Id = "shared", StoreId = "" };
        mocks.Warehouses.Setup(x => x.GetWarehouseById("w1")).ReturnsAsync(own);
        mocks.Warehouses.Setup(x => x.GetWarehouseById("shared")).ReturnsAsync(shared);
        mocks.WarehouseScope.Setup(x => x.HasAccess(own)).ReturnsAsync(true);
        mocks.WarehouseScope.Setup(x => x.HasAccess(shared)).ReturnsAsync(false);
        var model = new MerchandiseReturnModel { Id = "r1" };

        await service.PrepareReturnReceipts(model);

        //every receipt is named, only the panel's own is a link
        CollectionAssert.AreEqual(new[] { false, false, true },
            model.ReturnReceipts.Select(x => x.CanOpen).ToArray());
    }

    [TestMethod]
    public async Task PrepareReturnReceipts_NoInventoryDocumentsPermission_NoButtonAndNoLinks()
    {
        var (service, mocks) = BuildForReturnReceipts(new MerchandiseReturnReceipts([
            new ReturnReceiptSummary("d1", "PZZ/2026/0001", InventoryDocumentStatus.Draft, "", "")
        ], true));
        mocks.Permissions.Setup(x => x.Authorize(PermissionSystemName.InventoryDocuments)).ReturnsAsync(false);
        var model = new MerchandiseReturnModel { Id = "r1" };

        await service.PrepareReturnReceipts(model);

        Assert.IsFalse(model.CanReceiveToStock);
        //the receipt is still named, as plain text
        Assert.AreEqual("PZZ/2026/0001", model.ReturnReceipts.Single().Number);
        Assert.IsFalse(model.ReturnReceipts.Single().CanOpen);
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
            new Mock<IInventoryDocumentService>().Object,
            new Mock<IWarehouseService>().Object,
            new Mock<IAdminDataScope<Warehouse>>().Object,
            new Mock<IRequestPermissionService>().Object,
            new Mock<IPermissionService>().Object,
            new Mock<IMerchandiseReturnRefundService>().Object,
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
        Assert.AreEqual("Locker 7, Main 1, 00-001 Town", card.DropOffPoint);
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
    [DataRow(MerchandiseReturnStatus.Rejected)]
    [DataRow(MerchandiseReturnStatus.Cancelled)]
    [DataRow(MerchandiseReturnStatus.Received)]
    [DataRow(MerchandiseReturnStatus.Completed)]
    public async Task PrepareMerchandiseReturnModel_FailedLabelOfReturnNotWaitingForTheGoods_CannotRetry(
        MerchandiseReturnStatus status)
    {
        var (service, _) = BuildForReturnShipping();

        var model = await service.PrepareMerchandiseReturnModel(new MerchandiseReturnModel(),
            ReturnWithShipping(ReturnLabelStatus.Failed, status), false);

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
                Id = "r1", ChangeStatusTo = (int)MerchandiseReturnStatus.Completed,
                ReturnShipping = new MerchandiseReturnModel.ReturnShippingModel {
                    TrackingNumber = "T2", ShownTrackingNumber = "T1"
                }
            }, []);

        Assert.AreEqual(MerchandiseReturnErrors.Busy, result.Error);
        mocks.Workflow.Verify(x => x.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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

    // --- Refund ----------------------------------------------------------------------------------

    private sealed class RefundMocks
    {
        public Mock<IMerchandiseReturnRefundService> Refunds { get; } = new();
        public Mock<IPermissionService> Permissions { get; } = new();
        public Mock<IPriceFormatter> PriceFormatter { get; } = new();
        public Mock<IRequestTranslationService> Translation { get; } = new();
    }

    private static (MerchandiseReturnViewModelService service, RefundMocks mocks) BuildForRefund()
    {
        var mocks = new RefundMocks();
        var orderServiceMock = new Mock<IOrderService>();
        orderServiceMock.Setup(x => x.GetOrderById("o1"))
            .ReturnsAsync(new Order { Id = "o1", CustomerCurrencyCode = "EUR" });
        var currencyMock = new Mock<ICurrencyService>();
        currencyMock.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(new Currency { CurrencyCode = "EUR" });
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { Email = "john@shop.com" });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        var scopeMock = new Mock<IAdminDataScope<MerchandiseReturn>>();
        scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Admin");
        mocks.Permissions.Setup(x => x.AuthorizeAction(PermissionSystemName.PaymentTransactions,
            PermissionActionName.Edit, It.IsAny<Customer>())).ReturnsAsync(true);
        mocks.PriceFormatter.Setup(x => x.FormatPrice(It.IsAny<double>(), It.IsAny<Currency>()))
            .Returns<double, Currency>((value, currency) => $"{value:0.00} {currency.CurrencyCode}");
        mocks.Translation.Setup(x => x.GetResource(It.IsAny<string>())).Returns<string>(key => key);

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
            new Mock<ICountryService>().Object,
            new Mock<IAddressAttributeService>().Object,
            new Mock<IAddressAttributeParser>().Object,
            new Mock<IDownloadService>().Object,
            new OrderSettings(),
            new Mock<IEnumTranslationService>().Object,
            contextAccessor.Object,
            new CustomerConfig(),
            scopeMock.Object,
            new Mock<IMerchandiseReturnWorkflowService>().Object,
            new Mock<IMerchandiseReturnLabelService>().Object,
            new Mock<IReturnShippingService>().Object,
            currencyMock.Object,
            new Mock<IInventoryDocumentService>().Object,
            new Mock<IWarehouseService>().Object,
            new Mock<IAdminDataScope<Warehouse>>().Object,
            new Mock<IRequestPermissionService>().Object,
            mocks.Permissions.Object,
            mocks.Refunds.Object,
            new Mock<ILogger<MerchandiseReturnViewModelService>>().Object);
        return (service, mocks);
    }

    private static MerchandiseReturn ReceivedReturn()
    {
        return new MerchandiseReturn
            { Id = "r1", OrderId = "o1", MerchandiseReturnStatus = MerchandiseReturnStatus.Received };
    }

    [TestMethod]
    public async Task PrepareRefund_Received_ShowsTheQuote_WithTheFullAmountToRefund()
    {
        var (service, mocks) = BuildForRefund();
        mocks.Refunds.Setup(x => x.GetQuote(It.Is<MerchandiseReturn>(r => r.Id == "r1"), It.IsAny<Order>()))
            .ReturnsAsync(new MerchandiseReturnRefundQuote {
                ItemsAmount = 200, ReturnShippingCost = 5, Amount = 195, MaxAmount = 195, CurrencyCode = "EUR",
                CanRefundOnline = false, CanRefundOffline = true
            });
        var model = new MerchandiseReturnModel();

        await service.PrepareRefund(model, ReceivedReturn(), true);

        Assert.IsFalse(model.Refund.Started);
        Assert.AreEqual("200.00 EUR", model.Refund.ItemsAmount);
        Assert.AreEqual("5.00 EUR", model.Refund.ReturnShippingCost);
        Assert.AreEqual("195.00 EUR", model.Refund.Amount);
        Assert.AreEqual("195.00 EUR", model.Refund.MaxAmount);
        Assert.AreEqual(195, model.RefundAmount);
        Assert.IsFalse(model.Refund.CanRefundOnline);
        Assert.IsTrue(model.Refund.CanRefundOffline);
        Assert.IsNull(model.Refund.Error);
    }

    [TestMethod]
    public async Task PrepareRefund_QuoteWithError_ShowsItTranslated_OffersNoButton()
    {
        var (service, mocks) = BuildForRefund();
        mocks.Refunds.Setup(x => x.GetQuote(It.Is<MerchandiseReturn>(r => r.Id == "r1"), It.IsAny<Order>()))
            .ReturnsAsync(new MerchandiseReturnRefundQuote {
                Error = MerchandiseReturnErrors.NothingToRefund, CanRefundOnline = true, CanRefundOffline = true
            });
        var model = new MerchandiseReturnModel();

        await service.PrepareRefund(model, ReceivedReturn(), true);

        Assert.AreEqual("Admin." + MerchandiseReturnErrors.NothingToRefund, model.Refund.Error);
        Assert.IsFalse(model.Refund.CanRefundOnline);
        Assert.IsFalse(model.Refund.CanRefundOffline);
    }

    [TestMethod]
    public async Task PrepareRefund_WithoutTheEditActionOnPaymentTransactions_ShowsNothing()
    {
        var (service, mocks) = BuildForRefund();
        mocks.Permissions.Setup(x => x.AuthorizeAction(PermissionSystemName.PaymentTransactions,
            PermissionActionName.Edit, It.IsAny<Customer>())).ReturnsAsync(false);
        var model = new MerchandiseReturnModel();

        await service.PrepareRefund(model, ReceivedReturn(), true);

        Assert.IsNull(model.Refund);
        mocks.Refunds.Verify(x => x.GetQuote(It.IsAny<MerchandiseReturn>(), It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task PrepareRefund_RefundMade_ShownInAPanelThatDoesNotRefund()
    {
        var (service, mocks) = BuildForRefund();
        var merchandiseReturn = ReceivedReturn();
        merchandiseReturn.MerchandiseReturnStatus = MerchandiseReturnStatus.Completed;
        merchandiseReturn.Refund = new MerchandiseReturnRefund {
            ItemsAmount = 200, ReturnShippingCost = 5, Amount = 150, Offline = true, RefundedBy = "john@shop.com",
            RefundedOnUtc = DateTime.UtcNow
        };
        var model = new MerchandiseReturnModel();

        await service.PrepareRefund(model, merchandiseReturn, false);

        Assert.IsTrue(model.Refund.Started);
        Assert.AreEqual("150.00 EUR", model.Refund.Amount);
        Assert.IsTrue(model.Refund.Offline);
        Assert.AreEqual("john@shop.com", model.Refund.RefundedBy);
        mocks.Refunds.Verify(x => x.GetQuote(It.IsAny<MerchandiseReturn>(), It.IsAny<Order>()), Times.Never);
    }

    [TestMethod]
    public async Task RefundMerchandiseReturn_WithoutTheEditActionOnPaymentTransactions_RefundsNothing()
    {
        var (service, mocks) = BuildForRefund();
        mocks.Permissions.Setup(x => x.AuthorizeAction(PermissionSystemName.PaymentTransactions,
            PermissionActionName.Edit, It.IsAny<Customer>())).ReturnsAsync(false);

        var error = await service.RefundMerchandiseReturn(ReceivedReturn(), 100, true);

        Assert.AreEqual(MerchandiseReturnErrors.RefundNotAllowed, error);
        mocks.Refunds.Verify(x => x.Refund(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<bool>(),
            It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task RefundMerchandiseReturn_NamesTheStaffMember()
    {
        var (service, mocks) = BuildForRefund();

        await service.RefundMerchandiseReturn(ReceivedReturn(), 100, true);

        mocks.Refunds.Verify(x => x.Refund("r1", 100, true, "john@shop.com"), Times.Once);
    }

    private static MerchandiseReturn AwaitingCheck()
    {
        var merchandiseReturn = ReceivedReturn();
        merchandiseReturn.Refund = new MerchandiseReturnRefund
            { Amount = 150, OutcomeUnknown = true, CreatedOnUtc = DateTime.UtcNow };
        return merchandiseReturn;
    }

    [TestMethod]
    public async Task PrepareRefund_AwaitingCheck_OffersTheCheck_InAPanelThatRefunds()
    {
        var (service, _) = BuildForRefund();
        var model = new MerchandiseReturnModel();

        await service.PrepareRefund(model, AwaitingCheck(), true);

        Assert.IsTrue(model.Refund.Started);
        Assert.IsTrue(model.Refund.AwaitsCheck);
    }

    [TestMethod]
    public async Task PrepareRefund_AwaitingCheck_NotOffered_InAPanelThatDoesNotRefund_OrWithoutTheEditAction()
    {
        var (service, mocks) = BuildForRefund();
        var vendorModel = new MerchandiseReturnModel();
        await service.PrepareRefund(vendorModel, AwaitingCheck(), false);
        mocks.Permissions.Setup(x => x.AuthorizeAction(PermissionSystemName.PaymentTransactions,
            PermissionActionName.Edit, It.IsAny<Customer>())).ReturnsAsync(false);
        var model = new MerchandiseReturnModel();

        await service.PrepareRefund(model, AwaitingCheck(), true);

        Assert.IsFalse(vendorModel.Refund.AwaitsCheck);
        Assert.IsTrue(model.Refund.Started);
        Assert.IsFalse(model.Refund.AwaitsCheck);
    }

    [TestMethod]
    public async Task PrepareRefund_UnderWayAndStillRunning_OffersNoCheck()
    {
        var (service, _) = BuildForRefund();
        var merchandiseReturn = ReceivedReturn();
        merchandiseReturn.Refund = new MerchandiseReturnRefund { Amount = 150, CreatedOnUtc = DateTime.UtcNow };
        var model = new MerchandiseReturnModel();

        await service.PrepareRefund(model, merchandiseReturn, true);

        Assert.IsFalse(model.Refund.AwaitsCheck);
    }

    [TestMethod]
    public async Task ResolveRefund_WithoutTheEditActionOnPaymentTransactions_RecordsNothing()
    {
        var (service, mocks) = BuildForRefund();
        mocks.Permissions.Setup(x => x.AuthorizeAction(PermissionSystemName.PaymentTransactions,
            PermissionActionName.Edit, It.IsAny<Customer>())).ReturnsAsync(false);

        var error = await service.ResolveRefund(AwaitingCheck(), true);

        Assert.AreEqual(MerchandiseReturnErrors.RefundNotAllowed, error);
        mocks.Refunds.Verify(x => x.ResolveRefund(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ResolveRefund_NamesTheStaffMember()
    {
        var (service, mocks) = BuildForRefund();

        await service.ResolveRefund(AwaitingCheck(), false);

        mocks.Refunds.Verify(x => x.ResolveRefund("r1", false, "john@shop.com"), Times.Once);
    }
}
