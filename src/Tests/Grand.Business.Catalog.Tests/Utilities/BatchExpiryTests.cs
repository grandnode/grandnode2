using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Utilities;

[TestClass]
public class BatchExpiryTests
{
    private static readonly DateTime Today = new(2026, 11, 7);

    private static BatchExpiryStatus? Status(int year, int month, int day) =>
        BatchExpiry.Status(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc), Today, 30);

    [TestMethod]
    public void Status_NoExpiry_Null()
    {
        Assert.IsNull(BatchExpiry.Status(null, Today, 30));
    }

    [TestMethod]
    public void Status_BeforeToday_Expired()
    {
        Assert.AreEqual(BatchExpiryStatus.Expired, Status(2026, 11, 6));
    }

    [TestMethod]
    public void Status_Today_Expiring()
    {
        Assert.AreEqual(BatchExpiryStatus.Expiring, Status(2026, 11, 7));
    }

    [TestMethod]
    public void Status_LastWarningDay_Expiring()
    {
        Assert.AreEqual(BatchExpiryStatus.Expiring, Status(2026, 12, 7));
    }

    [TestMethod]
    public void Status_AfterWarningDays_Valid()
    {
        Assert.AreEqual(BatchExpiryStatus.Valid, Status(2026, 12, 8));
    }

    [TestMethod]
    public void WarningDays_ProductOverridesSetting()
    {
        Assert.AreEqual(5,
            BatchExpiry.WarningDays(new Product { ExpiryWarningDays = 5 },
                new InventorySettings { ExpiryWarningDays = 30 }));
    }

    [TestMethod]
    public void WarningDays_ProductEmpty_Setting()
    {
        Assert.AreEqual(30, BatchExpiry.WarningDays(new Product(), new InventorySettings()));
    }
}
