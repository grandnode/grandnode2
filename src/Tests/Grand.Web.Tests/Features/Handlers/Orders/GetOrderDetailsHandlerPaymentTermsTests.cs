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
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
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
public class GetOrderDetailsHandlerPaymentTermsTests
{
    private GetOrderDetailsHandler _handler;

    [TestInitialize]
    public void Init()
    {
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
            new TaxSettings(), new Mock<IEnumTranslationService>().Object);
    }

    private Task<OrderDetailsModel> Details(Order order)
    {
        return _handler.Handle(new GetOrderDetails { Order = order, Language = new Language { Id = "en" } },
            CancellationToken.None);
    }

    [TestMethod]
    public async Task PurchaseOrderNumber_Mapped()
    {
        var due = DateTime.UtcNow.Date.AddDays(14);

        var model = await Details(new Order { PurchaseOrderNumber = "PO-9", PaymentDueDateUtc = due });

        Assert.AreEqual("PO-9", model.PurchaseOrderNumber);
        Assert.AreEqual(due, model.PaymentDueDate);
        Assert.IsFalse(model.IsPaymentOverdue);
    }

    [TestMethod]
    public async Task Overdue_Flagged()
    {
        var model = await Details(new Order {
            PaymentDueDateUtc = DateTime.UtcNow.Date.AddDays(-1), PaymentStatusId = PaymentStatus.Pending
        });

        Assert.IsTrue(model.IsPaymentOverdue);
    }

    [TestMethod]
    public async Task NotOnCredit_NoDueDate()
    {
        var model = await Details(new Order { PaymentStatusId = PaymentStatus.Pending });

        Assert.IsNull(model.PaymentDueDate);
        Assert.IsFalse(model.IsPaymentOverdue);
    }
}
