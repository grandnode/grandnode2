using Grand.Business.Core.Extensions;
using Grand.Domain.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Cms.Tests.Extensions;

[TestClass]
public class PageExtensionsTests
{
    private static Page Shared(string systemName, string id)
    {
        return new Page { Id = id, SystemName = systemName, LimitedToStores = false };
    }

    private static Page OwnedBy(string systemName, string id, string storeId)
    {
        return new Page { Id = id, SystemName = systemName, LimitedToStores = true, Stores = { storeId } };
    }

    [TestMethod]
    public void PreferStoreOverrides_StoreCopiedTheSharedPage_KeepsOnlyTheCopy()
    {
        var pages = new List<Page> { Shared("about", "1"), OwnedBy("about", "2", "store-1") };

        var result = pages.PreferStoreOverrides("store-1");

        Assert.HasCount(1, result);
        Assert.AreEqual("2", result[0].Id);
    }

    [TestMethod]
    public void PreferStoreOverrides_NoCopyForThisStore_KeepsTheSharedPage()
    {
        var pages = new List<Page> { Shared("about", "1") };

        var result = pages.PreferStoreOverrides("store-1");

        Assert.HasCount(1, result);
        Assert.AreEqual("1", result[0].Id);
    }

    /// <summary>
    ///     Only the system name the store overrode is affected; everything else it may see stays.
    /// </summary>
    [TestMethod]
    public void PreferStoreOverrides_OtherSystemNames_AreUntouched()
    {
        var pages = new List<Page> {
            Shared("about", "1"),
            OwnedBy("about", "2", "store-1"),
            Shared("contact", "3"),
            OwnedBy("terms", "4", "store-1")
        };

        var result = pages.PreferStoreOverrides("store-1");

        CollectionAssert.AreEqual(new[] { "2", "3", "4" }, result.Select(p => p.Id).ToArray());
    }

    /// <summary>
    ///     A store panel creates the copy with the system name it was copied from, but nothing stops the
    ///     casing from differing, and the storefront treats one page as one page regardless of casing.
    /// </summary>
    [TestMethod]
    public void PreferStoreOverrides_SystemNameCasingDiffers_StillCountsAsAnOverride()
    {
        var pages = new List<Page> { Shared("About", "1"), OwnedBy("about", "2", "store-1") };

        var result = pages.PreferStoreOverrides("store-1");

        Assert.HasCount(1, result);
        Assert.AreEqual("2", result[0].Id);
    }

    /// <summary>
    ///     The order the caller was given is the order it renders in, so collapsing must not reshuffle.
    /// </summary>
    [TestMethod]
    public void PreferStoreOverrides_PreservesTheIncomingOrder()
    {
        var pages = new List<Page> {
            Shared("c", "1"),
            OwnedBy("a", "2", "store-1"),
            Shared("b", "3")
        };

        var result = pages.PreferStoreOverrides("store-1");

        CollectionAssert.AreEqual(new[] { "1", "2", "3" }, result.Select(p => p.Id).ToArray());
    }

    /// <summary>
    ///     Without a store there is no override to prefer - the admin panel reads pages this way.
    /// </summary>
    [TestMethod]
    public void PreferStoreOverrides_NoStore_KeepsEveryPage()
    {
        var pages = new List<Page> { Shared("about", "1"), OwnedBy("about", "2", "store-1") };

        var result = pages.PreferStoreOverrides("");

        Assert.HasCount(2, result);
    }

    /// <summary>
    ///     Another store's copy is not this store's override, and a page limited to another store should
    ///     not have reached this method at all.
    /// </summary>
    [TestMethod]
    public void PreferStoreOverrides_CopyBelongsToAnotherStore_KeepsTheSharedPage()
    {
        var pages = new List<Page> { Shared("about", "1"), OwnedBy("about", "2", "store-2") };

        var result = pages.PreferStoreOverrides("store-1");

        CollectionAssert.AreEqual(new[] { "1", "2" }, result.Select(p => p.Id).ToArray());
    }

    [TestMethod]
    public void PreferStoreOverrides_NullPages_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ((IEnumerable<Page>)null).PreferStoreOverrides("store-1"));
    }
}
