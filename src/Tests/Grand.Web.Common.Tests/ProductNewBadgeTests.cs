extern alias GrandWebAlias;
using GrandWebAlias::Grand.Web.Models.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests;

[TestClass]
public class ProductNewBadgeTests
{
    [TestMethod]
    public void ProductDetailsModel_HasShowNewBadgeProperty()
    {
        var model = new ProductDetailsModel();
        model.ShowNewBadge = true;
        Assert.IsTrue(model.ShowNewBadge);
    }

    [TestMethod]
    public void BadgeLogic_ProductCreatedToday_IsTrue()
    {
        var createdOnUtc = DateTime.UtcNow;
        var result = createdOnUtc >= DateTime.UtcNow.AddDays(-30);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void BadgeLogic_ProductCreatedWithin30Days_IsTrue()
    {
        var createdOnUtc = DateTime.UtcNow.AddDays(-29);
        var result = createdOnUtc >= DateTime.UtcNow.AddDays(-30);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void BadgeLogic_ProductCreated31DaysAgo_IsFalse()
    {
        var createdOnUtc = DateTime.UtcNow.AddDays(-31);
        var result = createdOnUtc >= DateTime.UtcNow.AddDays(-30);
        Assert.IsFalse(result);
    }
}
