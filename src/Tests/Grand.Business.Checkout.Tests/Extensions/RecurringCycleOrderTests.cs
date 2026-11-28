using System.Collections;
using System.Reflection;
using Grand.Business.Checkout.Extensions;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Extensions;

[TestClass]
public class RecurringCycleOrderTests
{
    private static readonly DateTime CreatedOn = new(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc);

    // properties the copy sets or resets, with the value it must hold
    private static readonly Dictionary<string, object?> ResetProperties = new() {
        { nameof(Order.Id), "c2" },
        { nameof(Order.OrderNumber), 0 },
        { nameof(Order.Code), "ABC" },
        { nameof(Order.InitialOrderId), null }, // checked in the loop: the initial order's id
        { nameof(Order.RecurringCycleNumber), 2 },
        { nameof(Order.CreatedOnUtc), CreatedOn },
        { nameof(Order.CreatedBy), null },
        { nameof(Order.UpdatedOnUtc), null },
        { nameof(Order.UpdatedBy), null },
        { nameof(Order.OrderStatusId), (int)OrderStatusSystem.Pending },
        { nameof(Order.PaymentStatusId), PaymentStatus.Pending },
        { nameof(Order.ShippingStatusId), ShippingStatus.Pending },
        { nameof(Order.PaidAmount), 0d },
        { nameof(Order.PaidDateUtc), null },
        { nameof(Order.PaymentDueDateUtc), null },
        { nameof(Order.PaymentReminderSentOnUtc), null },
        { nameof(Order.PaymentOverdueNoticeSentOnUtc), null },
        { nameof(Order.RefundedAmount), 0d },
        { nameof(Order.RedeemedLoyaltyPoints), 0 },
        { nameof(Order.RedeemedLoyaltyPointsAmount), 0d },
        { nameof(Order.LoyaltyPointsWereAdded), false },
        { nameof(Order.CalcLoyaltyPoints), 0 },
        { nameof(Order.OrderTotal), 100d }, // the initial total (90) plus the points amount (10)
        { nameof(Order.Deleted), false },
        { nameof(Order.Imported), false },
        { nameof(Order.OrderGuid), null }, // checked in the loop: a new guid
        { nameof(Order.UserFields), null } // checked in the loop: empty
    };

    private static OrderItem Item(string productId, bool ship = true)
    {
        return new OrderItem {
            OrderItemGuid = Guid.NewGuid(),
            ProductId = productId,
            Sku = "sku-" + productId,
            VendorId = "v1",
            WarehouseId = "w1",
            SeId = "se1",
            Quantity = 3,
            IsShipEnabled = ship,
            OpenQty = 1,
            ShipQty = 2,
            CancelQty = 1,
            CancelAmount = 5,
            ReturnQty = 1,
            Status = OrderItemStatus.Close,
            TaxRate = 23,
            UnitPriceWithoutDiscInclTax = 12.5,
            UnitPriceWithoutDiscExclTax = 10.2,
            UnitPriceInclTax = 12,
            UnitPriceExclTax = 9.76,
            PriceInclTax = 36,
            PriceExclTax = 29.28,
            DiscountAmountInclTax = 1.5,
            DiscountAmountExclTax = 1.2,
            OriginalProductCost = 4,
            AttributeDescription = "Color: red",
            Attributes = new List<CustomAttribute> { new() { Key = "color", Value = "red" } },
            DownloadCount = 4,
            IsDownloadActivated = true,
            LicenseDownloadId = "dl1",
            ItemWeight = 2.5,
            RentalStartDateUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            RentalEndDateUtc = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            CreatedOnUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            Commission = 0.7,
            cId = "c-owner"
        };
    }

    private static Order Initial()
    {
        var order = new Order {
            Id = "o1",
            OrderGuid = Guid.NewGuid(),
            OrderNumber = 17,
            Code = "INIT",
            OrderTotal = 120,
            CurrencyRate = 4.3,
            PaidAmount = 120,
            PaidDateUtc = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc),
            OrderStatusId = (int)OrderStatusSystem.Complete,
            PaymentStatusId = PaymentStatus.Paid,
            ShippingStatusId = ShippingStatus.Delivered,
            BillingAddress = new Address {
                City = "Warsaw", Attributes = new List<CustomAttribute> { new() { Key = "a", Value = "b" } }
            },
            ShippingAddress = new Address { City = "Krakow" },
            IsRecurring = true,
            RecurringCycleNumber = 1
        };
        order.OrderItems.Add(Item("p1"));
        order.OrderItems.Add(Item("p2"));
        return order;
    }

    [TestMethod]
    public void Copy_KeepsPricesAddressesAndItems()
    {
        var initial = Initial();
        initial.OrderTaxes.Add(new OrderTax { Percent = 23, Amount = 22.4 });

        var copy = RecurringCycleOrder.Copy(initial, "c2", 2, "ABC", CreatedOn, 0);

        Assert.AreEqual(120, copy.OrderTotal);
        Assert.AreEqual(4.3, copy.CurrencyRate);
        Assert.AreEqual(2, copy.OrderItems.Count);
        var source = initial.OrderItems.ToList();
        var items = copy.OrderItems.ToList();
        for (var i = 0; i < items.Count; i++)
        {
            Assert.AreNotSame(source[i], items[i]);
            Assert.AreNotEqual(source[i].Id, items[i].Id);
            Assert.AreEqual(source[i].UnitPriceInclTax, items[i].UnitPriceInclTax);
            Assert.AreEqual(source[i].PriceInclTax, items[i].PriceInclTax);
            Assert.AreEqual("w1", items[i].WarehouseId);
            Assert.AreEqual(source[i].ProductId, items[i].ProductId);
            Assert.AreEqual(source[i].cId, items[i].cId);
            Assert.AreNotSame(source[i].Attributes, items[i].Attributes);
            Assert.AreEqual(1, items[i].Attributes.Count);
            Assert.AreEqual("color", items[i].Attributes[0].Key);
            Assert.AreEqual("red", items[i].Attributes[0].Value);
            Assert.AreEqual(CreatedOn, items[i].CreatedOnUtc);
        }

        Assert.AreEqual(1, copy.OrderTaxes.Count);
        Assert.AreEqual(23, copy.OrderTaxes.First().Percent);
        Assert.AreEqual(22.4, copy.OrderTaxes.First().Amount);
        Assert.AreNotSame(initial.OrderTaxes.First(), copy.OrderTaxes.First());
        Assert.AreEqual(0, copy.OrderTags.Count);

        Assert.AreNotSame(initial.BillingAddress, copy.BillingAddress);
        Assert.IsNotNull(copy.BillingAddress);
        Assert.AreEqual("Warsaw", copy.BillingAddress.City);
        Assert.IsNotNull(initial.BillingAddress);
        Assert.AreNotSame(initial.BillingAddress.Attributes, copy.BillingAddress.Attributes);
        Assert.AreEqual("a", copy.BillingAddress.Attributes[0].Key);
        Assert.AreNotSame(initial.ShippingAddress, copy.ShippingAddress);
        Assert.IsNotNull(copy.ShippingAddress);
        Assert.AreEqual("Krakow", copy.ShippingAddress.City);
    }

    [TestMethod]
    public void Copy_IsANewUnpaidOrderLinkedToTheInitialOne()
    {
        var initial = Initial();

        var copy = RecurringCycleOrder.Copy(initial, "c2", 2, "ABC", CreatedOn, 0);

        Assert.AreEqual("c2", copy.Id);
        Assert.AreEqual("ABC", copy.Code);
        Assert.AreEqual(initial.Id, copy.InitialOrderId);
        Assert.AreEqual(2, copy.RecurringCycleNumber);
        Assert.AreEqual(PaymentStatus.Pending, copy.PaymentStatusId);
        Assert.AreEqual(0, copy.PaidAmount);
        Assert.IsNull(copy.PaidDateUtc);
        Assert.AreEqual((int)OrderStatusSystem.Pending, copy.OrderStatusId);
        Assert.AreEqual(0, copy.OrderNumber);
        Assert.AreNotEqual(initial.OrderGuid, copy.OrderGuid);
        Assert.AreEqual(CreatedOn, copy.CreatedOnUtc);
        Assert.AreEqual(ShippingStatus.Pending, copy.ShippingStatusId);
        Assert.AreEqual(2, copy.OrderItems.Count);
        foreach (var item in copy.OrderItems)
        {
            Assert.IsFalse(initial.OrderItems.Any(i => i.OrderItemGuid == item.OrderItemGuid));
            Assert.AreEqual(item.Quantity, item.OpenQty);
            Assert.AreEqual(0, item.ShipQty);
            Assert.AreEqual(0, item.CancelQty);
            Assert.AreEqual(0, item.ReturnQty);
            Assert.AreEqual(0, item.CancelAmount);
            Assert.AreEqual(OrderItemStatus.Open, item.Status);
            Assert.AreEqual(0, item.DownloadCount);
            Assert.IsFalse(item.IsDownloadActivated);
            Assert.AreEqual("", item.LicenseDownloadId);
        }
    }

    [TestMethod]
    public void Copy_LoyaltyPointsSpentOnTheInitialOrderAreNotSpentAgain()
    {
        var initial = Initial();
        initial.OrderTotal = 90;
        initial.RedeemedLoyaltyPoints = 100;
        initial.RedeemedLoyaltyPointsAmount = 10;

        var copy = RecurringCycleOrder.Copy(initial, "c2", 2, "ABC", CreatedOn, 0);

        Assert.AreEqual(100, copy.OrderTotal);
        Assert.AreEqual(0, copy.RedeemedLoyaltyPoints);
        Assert.AreEqual(0, copy.RedeemedLoyaltyPointsAmount);
    }

    [TestMethod]
    public void Copy_GiftVouchersSpentOnTheInitialOrderAreNotSpentAgain()
    {
        var initial = Initial();
        initial.OrderTotal = 75;

        var copy = RecurringCycleOrder.Copy(initial, "c2", 2, "ABC", CreatedOn, 25);

        Assert.AreEqual(100, copy.OrderTotal);
    }

    [TestMethod]
    public void Copy_NothingToShip_ShippingNotRequired()
    {
        var initial = Initial();
        foreach (var item in initial.OrderItems) item.IsShipEnabled = false;

        var copy = RecurringCycleOrder.Copy(initial, "c2", 2, "ABC", CreatedOn, 0);

        Assert.AreEqual(ShippingStatus.ShippingNotRequired, copy.ShippingStatusId);
    }

    [TestMethod]
    public void Copy_EveryOrderPropertyIsCopiedOrDeliberatelyReset()
    {
        // OrderItems, OrderTaxes and OrderTags have protected setters; the explicit tests cover them
        var properties = typeof(Order).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true }).ToList();
        foreach (var name in ResetProperties.Keys)
            Assert.IsTrue(properties.Any(p => p.Name == name), $"{name} is not a settable Order property");

        var initial = new Order();
        var n = 1;
        foreach (var property in properties)
            property.SetValue(initial, Distinct(property.PropertyType, property.Name, n++));
        initial.OrderItems.Add(Item("p1"));
        initial.OrderTotal = 90;
        initial.RedeemedLoyaltyPointsAmount = 10;

        var copy = RecurringCycleOrder.Copy(initial, "c2", 2, "ABC", CreatedOn, 0);

        foreach (var property in properties)
        {
            var actual = property.GetValue(copy);
            if (!ResetProperties.TryGetValue(property.Name, out var expected))
            {
                AssertSameValue(property.GetValue(initial), actual, property.Name);
                continue;
            }

            switch (property.Name)
            {
                case nameof(Order.OrderGuid):
                    Assert.AreNotEqual(Guid.Empty, actual);
                    Assert.AreNotEqual(initial.OrderGuid, actual);
                    break;
                case nameof(Order.UserFields):
                    Assert.AreEqual(0, Assert.IsInstanceOfType<ICollection<UserField>>(actual).Count);
                    break;
                case nameof(Order.InitialOrderId):
                    Assert.AreEqual(initial.Id, actual);
                    break;
                default:
                    Assert.AreEqual(expected, actual, property.Name);
                    break;
            }
        }
    }

    [TestMethod]
    public void Copy_EveryOrderItemPropertyIsCopiedOrDeliberatelyReset()
    {
        var properties = typeof(OrderItem).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true }).ToList();
        var reset = new[] {
            nameof(OrderItem.Id), nameof(OrderItem.OrderItemGuid), nameof(OrderItem.OpenQty),
            nameof(OrderItem.ShipQty), nameof(OrderItem.CancelQty), nameof(OrderItem.CancelAmount),
            nameof(OrderItem.Status), nameof(OrderItem.ReturnQty), nameof(OrderItem.DownloadCount),
            nameof(OrderItem.IsDownloadActivated), nameof(OrderItem.LicenseDownloadId),
            nameof(OrderItem.CreatedOnUtc)
        };
        foreach (var name in reset)
            Assert.IsTrue(properties.Any(p => p.Name == name), $"{name} is not a settable OrderItem property");

        var source = new OrderItem();
        var n = 1;
        foreach (var property in properties)
            property.SetValue(source, Distinct(property.PropertyType, property.Name, n++));
        var initial = new Order();
        initial.OrderItems.Add(source);

        var copy = RecurringCycleOrder.Copy(initial, "c2", 2, "ABC", CreatedOn, 0).OrderItems.Single();

        foreach (var property in properties)
        {
            var actual = property.GetValue(copy);
            switch (property.Name)
            {
                case nameof(OrderItem.Id):
                    Assert.IsFalse(string.IsNullOrEmpty(Assert.IsInstanceOfType<string>(actual)));
                    Assert.AreNotEqual(source.Id, actual);
                    break;
                case nameof(OrderItem.OrderItemGuid):
                    Assert.AreNotEqual(Guid.Empty, actual);
                    Assert.AreNotEqual(source.OrderItemGuid, actual);
                    break;
                case nameof(OrderItem.OpenQty):
                    Assert.AreEqual(source.Quantity, actual);
                    break;
                case nameof(OrderItem.ShipQty):
                case nameof(OrderItem.CancelQty):
                case nameof(OrderItem.ReturnQty):
                case nameof(OrderItem.DownloadCount):
                    Assert.AreEqual(0, actual, property.Name);
                    break;
                case nameof(OrderItem.CancelAmount):
                    Assert.AreEqual(0d, actual);
                    break;
                case nameof(OrderItem.Status):
                    Assert.AreEqual(OrderItemStatus.Open, actual);
                    break;
                case nameof(OrderItem.IsDownloadActivated):
                    Assert.AreEqual(false, actual);
                    break;
                case nameof(OrderItem.LicenseDownloadId):
                    Assert.AreEqual("", actual);
                    break;
                case nameof(OrderItem.CreatedOnUtc):
                    Assert.AreEqual(CreatedOn, actual);
                    break;
                default:
                    AssertSameValue(property.GetValue(source), actual, property.Name);
                    break;
            }
        }
    }

    private static object Distinct(Type type, string name, int n)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string)) return $"{name}-{n}";
        if (type == typeof(int)) return n + 1000;
        if (type == typeof(double)) return n + 0.5;
        if (type == typeof(bool)) return true;
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type == typeof(DateTime)) return new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(n);
        if (type.IsEnum) return Enum.GetValues(type).Cast<object>().Last();
        if (type == typeof(IList<CustomAttribute>))
            return new List<CustomAttribute> { new() { Key = $"{name}-k", Value = $"{name}-v" } };
        if (type == typeof(IList<UserField>))
            return new List<UserField> { new() { Key = $"{name}-k", Value = $"{name}-v" } };
        if (type == typeof(Address) || type == typeof(PickupPoint))
        {
            var value = Activator.CreateInstance(type)!;
            var i = 1;
            foreach (var p in type.GetProperties().Where(p => p.SetMethod is { IsPublic: true }))
                p.SetValue(value, Distinct(p.PropertyType, $"{name}.{p.Name}", n * 100 + i++));
            return value;
        }

        // a new property of another type must teach this test how to fill and compare it
        throw new NotSupportedException($"Order.{name} has a type this test cannot fill: {type}");
    }

    private static void AssertSameValue(object? expected, object? actual, string path)
    {
        if (expected is null || expected is string || expected.GetType().IsValueType)
        {
            Assert.AreEqual(expected, actual, path);
            return;
        }

        Assert.IsNotNull(actual, path);
        Assert.AreNotSame(expected, actual, path);
        if (expected is IEnumerable expectedList)
        {
            var e = expectedList.Cast<object>().ToList();
            var a = ((IEnumerable)actual).Cast<object>().ToList();
            Assert.AreEqual(e.Count, a.Count, path);
            for (var i = 0; i < e.Count; i++) AssertSameValue(e[i], a[i], $"{path}[{i}]");
            return;
        }

        // an entity or address: a new instance holding the same values
        foreach (var p in expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            AssertSameValue(p.GetValue(expected), p.GetValue(actual), $"{path}.{p.Name}");
    }
}
