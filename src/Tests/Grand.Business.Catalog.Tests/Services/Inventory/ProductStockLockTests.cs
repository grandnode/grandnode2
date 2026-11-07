using Grand.Business.Core.Utilities.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Services.Inventory;

[TestClass]
public class ProductStockLockTests
{
    [TestMethod]
    public void Key_IsTheKeyCheckoutClaimsUses()
    {
        Assert.AreEqual("Product:abc", ProductStockLock.Key("abc"));
    }
}
