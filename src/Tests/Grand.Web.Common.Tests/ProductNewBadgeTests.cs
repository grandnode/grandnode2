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
}
