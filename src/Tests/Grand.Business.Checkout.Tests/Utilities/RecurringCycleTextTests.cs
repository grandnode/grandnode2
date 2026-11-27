using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Utilities;

[TestClass]
public class RecurringCycleTextTests
{
    [DataTestMethod]
    [DataRow(1, RecurringCyclePeriod.Days, "RecurringPayment.Cycle.Day")]
    [DataRow(3, RecurringCyclePeriod.Days, "RecurringPayment.Cycle.Days")]
    [DataRow(1, RecurringCyclePeriod.Weeks, "RecurringPayment.Cycle.Week")]
    [DataRow(2, RecurringCyclePeriod.Weeks, "RecurringPayment.Cycle.Weeks")]
    [DataRow(1, RecurringCyclePeriod.Months, "RecurringPayment.Cycle.Month")]
    [DataRow(12, RecurringCyclePeriod.Months, "RecurringPayment.Cycle.Months")]
    [DataRow(1, RecurringCyclePeriod.Years, "RecurringPayment.Cycle.Year")]
    [DataRow(2, RecurringCyclePeriod.Years, "RecurringPayment.Cycle.Years")]
    public void ResourceKey_OneCycleIsSingular_MoreArePlural(int cycleLength, RecurringCyclePeriod period,
        string expected)
    {
        Assert.AreEqual(expected, RecurringCycleText.ResourceKey(cycleLength, period));
    }
}
