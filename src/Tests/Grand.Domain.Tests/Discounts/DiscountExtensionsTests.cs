using Grand.Domain.Discounts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Discounts;

[TestClass]
public class DiscountExtensionsTests
{
    private static readonly DateTime Now = new(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);

    // --- IsEnabled = false overrides everything ---

    [TestMethod]
    public void IsDiscountActive_Disabled_ReturnsFalse()
    {
        var discount = new Discount { IsEnabled = false };

        Assert.IsFalse(discount.IsDiscountActive(Now));
    }

    [TestMethod]
    public void IsDiscountActive_Disabled_WithValidDateRange_ReturnsFalse()
    {
        var discount = new Discount {
            IsEnabled = false,
            StartDateUtc = Now.AddDays(-1),
            EndDateUtc = Now.AddDays(1)
        };

        Assert.IsFalse(discount.IsDiscountActive(Now));
    }

    // --- IsEnabled = true, no date constraints ---

    [TestMethod]
    public void IsDiscountActive_Enabled_NoDates_ReturnsTrue()
    {
        var discount = new Discount { IsEnabled = true };

        Assert.IsTrue(discount.IsDiscountActive(Now));
    }

    // --- Start date boundary cases ---

    [TestMethod]
    public void IsDiscountActive_Enabled_StartDateInFuture_ReturnsFalse()
    {
        var discount = new Discount {
            IsEnabled = true,
            StartDateUtc = Now.AddSeconds(1)
        };

        Assert.IsFalse(discount.IsDiscountActive(Now));
    }

    [TestMethod]
    public void IsDiscountActive_Enabled_StartDateExactlyNow_ReturnsTrue()
    {
        var discount = new Discount {
            IsEnabled = true,
            StartDateUtc = Now
        };

        Assert.IsTrue(discount.IsDiscountActive(Now));
    }

    [TestMethod]
    public void IsDiscountActive_Enabled_StartDateInPast_ReturnsTrue()
    {
        var discount = new Discount {
            IsEnabled = true,
            StartDateUtc = Now.AddDays(-7)
        };

        Assert.IsTrue(discount.IsDiscountActive(Now));
    }

    // --- End date boundary cases ---

    [TestMethod]
    public void IsDiscountActive_Enabled_EndDateInPast_ReturnsFalse()
    {
        var discount = new Discount {
            IsEnabled = true,
            EndDateUtc = Now.AddSeconds(-1)
        };

        Assert.IsFalse(discount.IsDiscountActive(Now));
    }

    [TestMethod]
    public void IsDiscountActive_Enabled_EndDateExactlyNow_ReturnsTrue()
    {
        var discount = new Discount {
            IsEnabled = true,
            EndDateUtc = Now
        };

        Assert.IsTrue(discount.IsDiscountActive(Now));
    }

    [TestMethod]
    public void IsDiscountActive_Enabled_EndDateInFuture_ReturnsTrue()
    {
        var discount = new Discount {
            IsEnabled = true,
            EndDateUtc = Now.AddDays(7)
        };

        Assert.IsTrue(discount.IsDiscountActive(Now));
    }

    // --- Full date range ---

    [TestMethod]
    public void IsDiscountActive_Enabled_WithinValidRange_ReturnsTrue()
    {
        var discount = new Discount {
            IsEnabled = true,
            StartDateUtc = Now.AddDays(-1),
            EndDateUtc = Now.AddDays(1)
        };

        Assert.IsTrue(discount.IsDiscountActive(Now));
    }

    [TestMethod]
    public void IsDiscountActive_Enabled_ExpiredRange_ReturnsFalse()
    {
        var discount = new Discount {
            IsEnabled = true,
            StartDateUtc = Now.AddDays(-10),
            EndDateUtc = Now.AddDays(-1)
        };

        Assert.IsFalse(discount.IsDiscountActive(Now));
    }

    [TestMethod]
    public void IsDiscountActive_Enabled_FutureRange_ReturnsFalse()
    {
        var discount = new Discount {
            IsEnabled = true,
            StartDateUtc = Now.AddDays(1),
            EndDateUtc = Now.AddDays(10)
        };

        Assert.IsFalse(discount.IsDiscountActive(Now));
    }
}
