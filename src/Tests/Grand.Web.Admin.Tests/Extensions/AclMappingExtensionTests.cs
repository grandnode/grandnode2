using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Extensions;

[TestClass]
public class AclMappingExtensionTests
{
    /// <summary>
    ///     Pins the fail-open: an empty store grants access even to an entity limited to another store.
    /// </summary>
    [TestMethod]
    public void AccessToEntityByStore_WithoutAStore_GrantsAccessToAnEntityLimitedToOtherStores()
    {
        var product = new Product {
            LimitedToStores = true,
            Stores = { "another-store" }
        };

        Assert.IsFalse(product.AccessToEntityByStore("this-store"));
        Assert.IsTrue(product.AccessToEntityByStore(""), "fail-open: no store means no check");
    }

    /// <summary>
    ///     This check is stricter than AclService.Authorize on the same-sounding question: an entity
    ///     shared with a second store is refused here, while AclService grants it. Anyone reaching for
    ///     "does this store own the entity" has to pick deliberately between the two.
    /// </summary>
    [TestMethod]
    public void AccessToEntityByStore_RefusesAnEntitySharedWithASecondStore()
    {
        var product = new Product {
            LimitedToStores = true,
            Stores = { "this-store", "another-store" }
        };

        Assert.IsFalse(product.AccessToEntityByStore("this-store"));
    }

    [TestMethod]
    public void AccessToEntityByStore_AcceptsAnEntityOwnedByThatStoreAlone()
    {
        var product = new Product {
            LimitedToStores = true,
            Stores = { "this-store" }
        };

        Assert.IsTrue(product.AccessToEntityByStore("this-store"));
    }

    [TestMethod]
    public void AccessToEntityByStore_RefusesAMissingEntity()
    {
        Assert.IsFalse(((Product)null).AccessToEntityByStore("this-store"));
    }
}
