using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure.Security;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Handlers.Orders;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Orders;

[TestClass]
public class GetOrderDetailsHandlerRecurringTests
{
    private CustomerSettings _customerSettings;
    private Order _initialOrder;
    private Mock<IOrderService> _orderService;
    private GetOrderDetailsHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _initialOrder = new Order { Id = "o1", OrderNumber = 1001, CustomerId = "buyer", StoreId = "s1" };
        _customerSettings = new CustomerSettings();

        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(x => x.ConvertToUserTime(It.IsAny<DateTime>(), It.IsAny<DateTimeKind>()))
            .Returns((DateTime d, DateTimeKind _) => d);
        var giftVoucherService = new Mock<IGiftVoucherService>();
        giftVoucherService.Setup(x => x.GetAllGiftVoucherUsageHistory(It.IsAny<string>()))
            .ReturnsAsync(new List<GiftVoucherUsageHistory>());
        _orderService = new Mock<IOrderService>();
        _orderService.Setup(x => x.GetOrderNotes(It.IsAny<string>())).ReturnsAsync(new List<OrderNote>());
        _orderService.Setup(x => x.GetOrderById("o1")).ReturnsAsync(() => _initialOrder);

        _handler = new GetOrderDetailsHandler(dateTimeService.Object, new Mock<IProductService>().Object,
            new Mock<IRequestTranslationService>().Object, new Mock<IShipmentService>().Object,
            new Mock<IPaymentService>().Object, new Mock<IPaymentTransactionService>().Object,
            new Mock<IPriceFormatter>().Object, giftVoucherService.Object,
            _orderService.Object, new Mock<IPictureService>().Object,
            new Mock<IOrderStatusService>().Object, new Mock<ICurrencyService>().Object,
            new Mock<IMediator>().Object, new CatalogSettings(), new OrderSettings(), new PdfSettings(),
            new TaxSettings(), new Mock<IEnumTranslationService>().Object, new Mock<IOrderApprovalService>().Object,
            _customerSettings, new Mock<IHtmlSanitizationService>().Object);
    }

    private Task<OrderDetailsModel> Details(Order order, string viewerId = "buyer", string storeId = "s1")
    {
        return _handler.Handle(new GetOrderDetails {
            Order = order,
            Customer = new Customer { Id = viewerId },
            Store = new Domain.Stores.Store { Id = storeId },
            Language = new Language { Id = "en" }
        }, CancellationToken.None);
    }

    private static Order CycleOrder()
    {
        return new Order { Id = "o3", CustomerId = "buyer", RecurringCycleNumber = 3, InitialOrderId = "o1" };
    }

    [TestMethod]
    public async Task GetOrderDetails_CycleOrder_LinksTheInitialOrder()
    {
        var model = await Details(CycleOrder());

        Assert.AreEqual(3, model.RecurringCycleNumber);
        Assert.AreEqual("o1", model.InitialOrderId);
        Assert.AreEqual(1001, model.InitialOrderNumber);
        Assert.IsFalse(model.ShowRecurringPaymentsLink);
    }

    [TestMethod]
    public async Task GetOrderDetails_CycleOrderOfDeletedInitialOrder_ShowsTheCycleWithoutTheLink()
    {
        _initialOrder.Deleted = true;

        var model = await Details(CycleOrder());

        Assert.IsNull(model.InitialOrderId);
        Assert.AreEqual(1001, model.InitialOrderNumber);
    }

    [TestMethod]
    public async Task GetOrderDetails_InitialOrder_ViewedByItsCustomer_LinksTheTab()
    {
        _initialOrder.RecurringCycleNumber = 1;

        var model = await Details(_initialOrder);

        Assert.AreEqual(1, model.RecurringCycleNumber);
        Assert.IsTrue(model.ShowRecurringPaymentsLink);
        Assert.IsNull(model.InitialOrderId);
    }

    [TestMethod]
    public async Task GetOrderDetails_InitialOrder_ViewedByTheCompanyOwner_NoTabLink()
    {
        _initialOrder.RecurringCycleNumber = 1;

        var model = await Details(_initialOrder, "owner");

        Assert.IsFalse(model.ShowRecurringPaymentsLink);
    }

    [TestMethod]
    public async Task GetOrderDetails_InitialOrder_ViewedInAnotherStore_NoTabLink()
    {
        _initialOrder.RecurringCycleNumber = 1;

        var model = await Details(_initialOrder, storeId: "s2");

        Assert.IsFalse(model.ShowRecurringPaymentsLink);
    }

    [TestMethod]
    public async Task GetOrderDetails_InitialOrder_TabOff_NoTabLink()
    {
        _initialOrder.RecurringCycleNumber = 1;
        _customerSettings.HideRecurringPaymentsTab = true;

        var model = await Details(_initialOrder);

        Assert.IsFalse(model.ShowRecurringPaymentsLink);
    }

    [TestMethod]
    public async Task GetOrderDetails_OrdinaryOrder_NoRecurringFields()
    {
        var model = await Details(new Order { Id = "o9", CustomerId = "buyer" });

        Assert.AreEqual(0, model.RecurringCycleNumber);
        Assert.IsNull(model.InitialOrderId);
        Assert.AreEqual(0, model.InitialOrderNumber);
        Assert.IsFalse(model.ShowRecurringPaymentsLink);
        _orderService.Verify(x => x.GetOrderById(It.IsAny<string>()), Times.Never);
    }
}
