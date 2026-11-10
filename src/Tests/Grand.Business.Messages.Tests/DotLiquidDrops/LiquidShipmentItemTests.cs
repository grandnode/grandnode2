using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Messages.Tests.DotLiquidDrops;

[TestClass]
public class LiquidShipmentItemTests
{
    private static LiquidShipmentItem Liquid(ShipmentItem item)
    {
        return new LiquidShipmentItem(item, new Shipment(), new Order(), new OrderItem(), new Product(),
            new Language());
    }

    [TestMethod]
    public void SerialNumbers_JoinedAndHtmlEncoded()
    {
        Assert.AreEqual("A1, &lt;b&gt;", Liquid(new ShipmentItem { SerialNumbers = { "A1", "<b>" } }).SerialNumbers);
    }

    [TestMethod]
    public void Batches_WithQuantities()
    {
        var item = new ShipmentItem {
            Batches = { new ShipmentItemBatch { Number = "L1", Quantity = 2 }, new ShipmentItemBatch { Number = "L2", Quantity = 1 } }
        };

        Assert.AreEqual("L1 x 2, L2 x 1", Liquid(item).Batches);
    }

    [TestMethod]
    public void NoUnits_TokensEmpty()
    {
        var liquid = Liquid(new ShipmentItem());

        Assert.AreEqual("", liquid.SerialNumbers);
        Assert.AreEqual("", liquid.Batches);
        Assert.AreEqual("", liquid.WarrantyUntil);
    }

    [TestMethod]
    public void WarrantyUntil_FormattedAsCalendarDate()
    {
        var item = new ShipmentItem { WarrantyUntilUtc = new DateTime(2028, 11, 10, 0, 0, 0, DateTimeKind.Utc) };

        Assert.AreEqual("2028-11-10", Liquid(item).WarrantyUntil);
    }
}
