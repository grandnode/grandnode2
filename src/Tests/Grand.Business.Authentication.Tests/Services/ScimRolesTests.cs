using Grand.Business.Authentication.Services.Scim;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class ScimRolesTests
{
    private const string Admin = "g-admin";
    private const string StoreManager = "g-sm";
    private const string Vendors = "g-vendors";
    private const string Custom = "g-custom";

    private static string SystemNameOf(string groupId) => groupId switch {
        Admin => SystemCustomerGroupNames.Administrators,
        StoreManager => SystemCustomerGroupNames.StoreManager,
        Vendors => SystemCustomerGroupNames.Vendors,
        _ => ""
    };

    private static ScimGroup Mapped(string customerGroupId, string store = null, string vendor = null) =>
        new() { CustomerGroupId = customerGroupId, StaffStoreId = store, VendorId = vendor };

    private static ScimWanted Wanted(params ScimGroup[] groups) => ScimRoles.Wanted(groups, SystemNameOf);

    [TestMethod]
    public void NoGroups_NothingWanted()
    {
        var wanted = Wanted();
        Assert.AreEqual(0, wanted.GroupIds.Count);
        Assert.IsNull(wanted.StaffStoreId);
        Assert.IsNull(wanted.VendorId);
        Assert.IsFalse(wanted.Conflict);
    }

    [TestMethod]
    public void AdministratorsAlone()
    {
        var wanted = Wanted(Mapped(Admin));
        CollectionAssert.AreEquivalent(new[] { Admin }, wanted.GroupIds.ToList());
        Assert.IsNull(wanted.StaffStoreId);
        Assert.IsFalse(wanted.Conflict);
    }

    [TestMethod]
    public void StoreManager_SameStoreTwice_OneStore()
    {
        var wanted = Wanted(Mapped(StoreManager, "storeA"), Mapped(StoreManager, "storeA"));
        CollectionAssert.AreEquivalent(new[] { StoreManager }, wanted.GroupIds.ToList());
        Assert.AreEqual("storeA", wanted.StaffStoreId);
        Assert.IsFalse(wanted.Conflict);
    }

    //guessing a store would give access to the wrong one
    [TestMethod]
    public void StoreManager_TwoStores_ConflictKeepsCustomGroups()
    {
        var wanted = Wanted(Mapped(StoreManager, "storeA"), Mapped(StoreManager, "storeB"), Mapped(Custom));
        CollectionAssert.AreEquivalent(new[] { Custom }, wanted.GroupIds.ToList());
        Assert.IsNull(wanted.StaffStoreId);
        Assert.IsTrue(wanted.Conflict);
    }

    //Administrators with a staff store is refused by the customer validator and locks the account out of the Admin panel
    [TestMethod]
    public void AdministratorsWithStoreManager_Conflict()
    {
        var wanted = Wanted(Mapped(Admin), Mapped(StoreManager, "storeA"));
        Assert.AreEqual(0, wanted.GroupIds.Count);
        Assert.IsNull(wanted.StaffStoreId);
        Assert.IsTrue(wanted.Conflict);
    }

    [TestMethod]
    public void VendorsAlone()
    {
        var wanted = Wanted(Mapped(Vendors, vendor: "v1"));
        CollectionAssert.AreEquivalent(new[] { Vendors }, wanted.GroupIds.ToList());
        Assert.AreEqual("v1", wanted.VendorId);
        Assert.IsFalse(wanted.Conflict);
    }

    [TestMethod]
    public void VendorsWithStoreManager_Conflict()
    {
        var wanted = Wanted(Mapped(Vendors, vendor: "v1"), Mapped(StoreManager, "storeA"));
        Assert.AreEqual(0, wanted.GroupIds.Count);
        Assert.IsNull(wanted.VendorId);
        Assert.IsNull(wanted.StaffStoreId);
        Assert.IsTrue(wanted.Conflict);
    }

    [TestMethod]
    public void VendorsTwoVendors_Conflict()
    {
        var wanted = Wanted(Mapped(Vendors, vendor: "v1"), Mapped(Vendors, vendor: "v2"));
        Assert.AreEqual(0, wanted.GroupIds.Count);
        Assert.IsTrue(wanted.Conflict);
    }

    [TestMethod]
    public void CustomGroupsOnly()
    {
        var wanted = Wanted(Mapped(Custom), Mapped("g-other"));
        CollectionAssert.AreEquivalent(new[] { Custom, "g-other" }, wanted.GroupIds.ToList());
        Assert.IsFalse(wanted.Conflict);
    }

    [TestMethod]
    public void UnmappedGroup_GivesNothing()
    {
        var wanted = Wanted(Mapped(null), Mapped(""));
        Assert.AreEqual(0, wanted.GroupIds.Count);
        Assert.IsFalse(wanted.Conflict);
    }
}
