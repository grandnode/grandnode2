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
using Grand.Domain.Shipping;
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
public class GetOrderDetailsHandlerUnitsTests
{
    private static readonly DateTime Warranty = new(2028, 11, 7, 0, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IShipmentService> _shipments = new();
    private GetOrderDetailsHandler _handler;
    private readonly Order _order = new() { Id = "o1", CustomerId = "buyer" };

    [TestInitialize]
    public void Init()
    {
        var product = new Product { Id = "p1", Name = "p" };
        var productService = new Mock<IProductService>();
        productService.Setup(x => x.GetProductByIdIncludeArch("p1")).ReturnsAsync(product);
        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(x => x.ConvertToUserTime(It.IsAny<DateTime>(), It.IsAny<DateTimeKind>()))
            .Returns((DateTime date, DateTimeKind _) => date);
        var giftVoucherService = new Mock<IGiftVoucherService>();
        giftVoucherService.Setup(x => x.GetAllGiftVoucherUsageHistory(It.IsAny<string>()))
            .ReturnsAsync(new List<GiftVoucherUsageHistory>());
        var orderService = new Mock<IOrderService>();
        orderService.Setup(x => x.GetOrderNotes(It.IsAny<string>())).ReturnsAsync(new List<OrderNote>());
        _order.OrderItems.Add(new OrderItem { Id = "oi1", ProductId = "p1", Quantity = 1, ShipQty = 1 });
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(x => x.GetResource(It.IsAny<string>())).Returns("{0}");
        _handler = new GetOrderDetailsHandler(dateTimeService.Object, productService.Object,
            translation.Object, _shipments.Object,
            new Mock<IPaymentService>().Object, new Mock<IPaymentTransactionService>().Object,
            new Mock<IPriceFormatter>().Object, giftVoucherService.Object,
            orderService.Object, new Mock<IPictureService>().Object,
            new Mock<IOrderStatusService>().Object, new Mock<ICurrencyService>().Object,
            new Mock<IMediator>().Object, new CatalogSettings(), new OrderSettings(), new PdfSettings(),
            new TaxSettings(), new Mock<IEnumTranslationService>().Object, new Mock<IOrderApprovalService>().Object);
    }

    private void Shipped(DateTime? shippedDateUtc, ShipmentItem item)
    {
        var shipment = new Shipment { OrderId = "o1", ShippedDateUtc = shippedDateUtc };
        shipment.ShipmentItems.Add(item);
        _shipments.Setup(x => x.GetShipmentsByOrder("o1")).ReturnsAsync([shipment]);
    }

    private async Task<OrderDetailsModel.OrderItemModel> Item() =>
        (await _handler.Handle(new GetOrderDetails {
            Order = _order, Customer = new Customer { Id = "buyer" }, Language = new Language { Id = "en" }
        }, CancellationToken.None)).Items.Single();

    [TestMethod]
    public async Task GetOrderDetails_ShippedSerials_ShownWithWarranty()
    {
        Shipped(DateTime.UtcNow, new ShipmentItem
            { OrderItemId = "oi1", SerialNumbers = ["A1"], WarrantyUntilUtc = Warranty });

        var item = await Item();

        CollectionAssert.AreEqual(new[] { "A1" }, item.SerialNumbers.ToArray());
        Assert.AreEqual(Warranty, item.WarrantyUntil);
    }

    [TestMethod]
    public async Task GetOrderDetails_Batches_NotShownToCustomer()
    {
        Shipped(DateTime.UtcNow, new ShipmentItem
            { OrderItemId = "oi1", Batches = [new ShipmentItemBatch { Number = "L1", Quantity = 1 }] });

        var item = await Item();

        Assert.AreEqual(0, item.SerialNumbers.Count);
    }

    [TestMethod]
    public async Task GetOrderDetails_UnshippedShipment_NoUnits()
    {
        Shipped(null, new ShipmentItem { OrderItemId = "oi1", SerialNumbers = ["A1"], WarrantyUntilUtc = Warranty });

        var item = await Item();

        Assert.AreEqual(0, item.SerialNumbers.Count);
        Assert.IsNull(item.WarrantyUntil);
    }
}
