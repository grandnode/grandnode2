using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Utilities;

[TestClass]
public class ShippedUnitsTests
{
    private static Shipment Shipment(int number, DateTime createdOnUtc, params ShipmentItem[] items)
    {
        var shipment = new Shipment { ShipmentNumber = number, CreatedOnUtc = createdOnUtc };
        foreach (var item in items)
            shipment.ShipmentItems.Add(item);
        return shipment;
    }

    [TestMethod]
    public void Of_OrderItem_SerialNumbersBatchesAndWarrantyPerShipment()
    {
        var warranty = new DateTime(2028, 11, 7, 0, 0, 0, DateTimeKind.Utc);
        var shipments = new[] {
            Shipment(2, new DateTime(2026, 11, 8), new ShipmentItem
                { OrderItemId = "oi1", Batches = [new ShipmentItemBatch { Number = "L1", Quantity = 2 }] }),
            Shipment(1, new DateTime(2026, 11, 7),
                new ShipmentItem { OrderItemId = "oi1", SerialNumbers = ["A1"], WarrantyUntilUtc = warranty },
                new ShipmentItem { OrderItemId = "oi2", SerialNumbers = ["B1"] })
        };

        var units = ShippedUnits.Of(shipments, "oi1");

        Assert.AreEqual(2, units.Count);
        Assert.AreEqual(1, units[0].ShipmentNumber);
        CollectionAssert.AreEqual(new[] { "A1" }, units[0].SerialNumbers.ToArray());
        Assert.AreEqual(warranty, units[0].WarrantyUntilUtc);
        Assert.AreEqual("L1", units[1].Batches.Single().Number);
    }

    [TestMethod]
    public void Of_ItemWithoutUnits_Left()
    {
        var shipments = new[] { Shipment(1, DateTime.UtcNow, new ShipmentItem { OrderItemId = "oi1", Quantity = 1 }) };

        Assert.AreEqual(0, ShippedUnits.Of(shipments, "oi1").Count);
    }

    [TestMethod]
    public void InWarranty_EndPassed_False()
    {
        var unit = new ShippedUnit("s1", 1, DateTime.UtcNow, [], [], new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.IsFalse(unit.InWarranty(new DateTime(2026, 11, 7, 0, 0, 0, DateTimeKind.Utc)));
        Assert.IsTrue(unit.InWarranty(new DateTime(2025, 11, 7, 0, 0, 0, DateTimeKind.Utc)));
    }
}
