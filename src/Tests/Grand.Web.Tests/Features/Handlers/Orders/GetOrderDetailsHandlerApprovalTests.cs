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
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Handlers.Orders;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Orders;

[TestClass]
public class GetOrderDetailsHandlerApprovalTests
{
    private OrderApproval _approval;
    private GetOrderDetailsHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _approval = new OrderApproval {
            OrderId = "o1", CustomerId = "buyer", Decider = OrderApprovalDecider.Store,
            Status = OrderApprovalStatus.Pending, Reasons = ["over the limit"]
        };
        var approvals = new Mock<IOrderApprovalService>();
        approvals.Setup(x => x.GetByOrderId("o1")).ReturnsAsync(() => _approval);

        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(x => x.ConvertToUserTime(It.IsAny<DateTime>(), It.IsAny<DateTimeKind>()))
            .Returns((DateTime d, DateTimeKind _) => d);
        var giftVoucherService = new Mock<IGiftVoucherService>();
        giftVoucherService.Setup(x => x.GetAllGiftVoucherUsageHistory(It.IsAny<string>()))
            .ReturnsAsync(new List<GiftVoucherUsageHistory>());
        var orderService = new Mock<IOrderService>();
        orderService.Setup(x => x.GetOrderNotes(It.IsAny<string>())).ReturnsAsync(new List<OrderNote>());

        _handler = new GetOrderDetailsHandler(dateTimeService.Object, new Mock<IProductService>().Object,
            new Mock<IRequestTranslationService>().Object, new Mock<IShipmentService>().Object,
            new Mock<IPaymentService>().Object, new Mock<IPaymentTransactionService>().Object,
            new Mock<IPriceFormatter>().Object, giftVoucherService.Object,
            orderService.Object, new Mock<IPictureService>().Object,
            new Mock<IOrderStatusService>().Object, new Mock<ICurrencyService>().Object,
            new Mock<IMediator>().Object, new CatalogSettings(), new OrderSettings(), new PdfSettings(),
            new TaxSettings(), new Mock<IEnumTranslationService>().Object, approvals.Object);
    }

    private Task<OrderDetailsModel> Details(string customerId)
    {
        return _handler.Handle(new GetOrderDetails {
            Order = new Order { Id = "o1", CustomerId = "buyer" },
            Customer = new Customer { Id = customerId },
            Language = new Language { Id = "en" }
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task GetOrderDetails_StoreDecidedPending_BuyerCanWithdrawAndSeesReasons()
    {
        var model = await Details("buyer");

        Assert.IsTrue(model.CanWithdrawOrderApproval);
        CollectionAssert.AreEqual(new[] { "over the limit" }, model.OrderApprovalReasons.ToArray());
    }

    [TestMethod]
    public async Task GetOrderDetails_StoreDecidedPending_OtherViewerCannotWithdraw()
    {
        var model = await Details("company-owner");

        Assert.IsFalse(model.CanWithdrawOrderApproval);
    }

    [TestMethod]
    public async Task GetOrderDetails_CompanyDecided_ModelUnchanged()
    {
        _approval.Decider = OrderApprovalDecider.Company;
        _approval.DecisionComment = "from the company";

        var model = await Details("buyer");

        Assert.IsFalse(model.CanWithdrawOrderApproval);
        Assert.AreEqual(0, model.OrderApprovalReasons.Count);
        Assert.IsNull(model.OrderApprovalComment);
    }

    [TestMethod]
    public async Task GetOrderDetails_Decided_ShowsCommentNotWithdraw()
    {
        _approval.Status = OrderApprovalStatus.Rejected;
        _approval.DecisionComment = "not this quarter";

        var model = await Details("buyer");

        Assert.IsFalse(model.CanWithdrawOrderApproval);
        Assert.AreEqual("not this quarter", model.OrderApprovalComment);
        Assert.AreEqual(0, model.OrderApprovalReasons.Count);
    }
}
