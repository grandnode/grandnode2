using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class OrderViewModelServiceRecurringTests
{
    private Mock<IOrderService> _orderService;
    private Mock<IRecurringPaymentService> _recurringPaymentService;
    private Mock<IAdminDataScope<Order>> _scope;
    private OrderViewModelService _service;

    public OrderViewModelServiceRecurringTests()
    {
        _orderService = new Mock<IOrderService>();
        _recurringPaymentService = new Mock<IRecurringPaymentService>();
        _scope = new Mock<IAdminDataScope<Order>>();

        _service = new OrderViewModelService(_orderService.Object, new Mock<IDateTimeService>().Object,
            new Mock<IPriceFormatter>().Object, new Mock<IDiscountService>().Object,
            new Mock<IRequestTranslationService>().Object, new Mock<IContextAccessor>().Object,
            new Mock<ICurrencyService>().Object,
            new Mock<IPaymentService>().Object, new Mock<IPaymentTransactionService>().Object,
            new Mock<ICountryService>().Object, new Mock<IProductService>().Object,
            new Mock<IGiftVoucherService>().Object, new Mock<IDownloadService>().Object,
            new Mock<IStoreService>().Object, new Mock<IVendorService>().Object,
            new Mock<ISalesEmployeeService>().Object, new Mock<IAddressAttributeParser>().Object,
            new Mock<IAffiliateService>().Object, new Mock<IPictureService>().Object,
            new Mock<ICustomerService>().Object,
            new Mock<IWarehouseService>().Object, new CurrencySettings(), new TaxSettings(), new AddressSettings(),
            new Mock<IOrderTagService>().Object, new Mock<IOrderStatusService>().Object, new Mock<IMediator>().Object,
            new Mock<IEnumTranslationService>().Object, _scope.Object, new Mock<IOrderApprovalService>().Object,
            new Mock<IShipmentService>().Object, new Mock<IAdminDataScope<MerchandiseReturn>>().Object,
            _recurringPaymentService.Object);
    }

    private async Task<OrderModel> Prepare(Order order)
    {
        var model = new OrderModel();
        await _service.PrepareOrderRecurringPayment(model, order);
        return model;
    }

    [TestMethod]
    public async Task InitialOrder_LinksItsRecurringPayment()
    {
        _recurringPaymentService.Setup(x => x.GetRecurringPaymentByInitialOrderId("o1"))
            .ReturnsAsync(new RecurringPayment { Id = "rp1" });

        var model = await Prepare(new Order { Id = "o1", RecurringCycleNumber = 1 });

        Assert.AreEqual("rp1", model.RecurringPaymentId);
        Assert.AreEqual(0, model.InitialOrderNumber);
    }

    [TestMethod]
    public async Task CycleOrder_LinksTheInitialOrder()
    {
        _orderService.Setup(x => x.GetOrderById("o1")).ReturnsAsync(new Order { Id = "o1", OrderNumber = 1042 });

        var model = await Prepare(new Order { Id = "o3", RecurringCycleNumber = 3, InitialOrderId = "o1" });

        Assert.AreEqual(3, model.RecurringCycleNumber);
        Assert.AreEqual(1042, model.InitialOrderNumber);
        Assert.AreEqual("o1", model.InitialOrderId);
        Assert.IsNull(model.RecurringPaymentId);
    }

    [TestMethod]
    public async Task CycleOrder_DeletedInitialOrder_ShowsTheNumberWithoutALink()
    {
        _orderService.Setup(x => x.GetOrderById("o1"))
            .ReturnsAsync(new Order { Id = "o1", OrderNumber = 1042, Deleted = true });

        var model = await Prepare(new Order { Id = "o3", RecurringCycleNumber = 3, InitialOrderId = "o1" });

        Assert.AreEqual(1042, model.InitialOrderNumber);
        Assert.IsNull(model.InitialOrderId);
    }

    [TestMethod]
    public async Task OrdinaryOrder_LinksNothing()
    {
        var model = await Prepare(new Order { Id = "o1" });

        Assert.IsNull(model.RecurringPaymentId);
        Assert.AreEqual(0, model.InitialOrderNumber);
        _recurringPaymentService.Verify(x => x.GetRecurringPaymentByInitialOrderId(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task VendorPanel_LinksNothing()
    {
        _scope.Setup(x => x.DefaultVendorId).Returns("vendor");
        _recurringPaymentService.Setup(x => x.GetRecurringPaymentByInitialOrderId("o1"))
            .ReturnsAsync(new RecurringPayment { Id = "rp1" });

        var model = await Prepare(new Order { Id = "o1", RecurringCycleNumber = 1 });

        Assert.IsNull(model.RecurringPaymentId);
        Assert.AreEqual(0, model.RecurringCycleNumber);
    }
}
