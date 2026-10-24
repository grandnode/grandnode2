using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Grand.Business.Authentication.Services.Scim;
using Grand.Infrastructure.Events;
using Grand.Domain.Customers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Grand.Business.Authentication.Tests.Services.Scim;

[TestClass]
public class ScimGroupServiceTests
{
    private ScimFixture _f;

    [TestInitialize]
    public void Init() => _f = new ScimFixture();

    private async Task<string> User(string email = "jan@example.com", string externalId = "oid-1") =>
        (await _f.UserService().Create(ScimFixture.Data(userName: email, externalId: externalId))).User.Id;

    private async Task<string> Group(string name, params string[] members) =>
        (await _f.GroupServiceUnderTest().Create(new ScimGroupData(name, "ext-" + name, members))).Group.Id;

    private async Task<bool> HasGroup(string customerId, string customerGroupId) =>
        (await _f.Reload(customerId)).Groups.Contains(customerGroupId);

    [TestMethod]
    public async Task Create_NoDisplayName_Invalid() =>
        Assert.AreEqual(ScimOutcome.Invalid,
            (await _f.GroupServiceUnderTest().Create(new ScimGroupData(" ", null, []))).Outcome);

    //Entra allows two groups with one name; refusing the second would fail its provisioning for good
    [TestMethod]
    public async Task Create_SameDisplayNameTwice_Allowed()
    {
        await Group("Shop admins");
        Assert.AreEqual(ScimOutcome.Created,
            (await _f.GroupServiceUnderTest().Create(new ScimGroupData("shop ADMINS", null, []))).Outcome);
    }

    [TestMethod]
    public async Task Create_SameExternalIdTwice_Conflict()
    {
        await _f.GroupServiceUnderTest().Create(new ScimGroupData("One", "ext-1", []));
        Assert.AreEqual(ScimOutcome.Conflict,
            (await _f.GroupServiceUnderTest().Create(new ScimGroupData("Two", "ext-1", []))).Outcome);
    }

    //a group pushed by the identity provider grants nothing by itself
    [TestMethod]
    public async Task Create_UnmappedGroup_GrantsNothing()
    {
        var user = await User();
        await Group("Shop admins", user);
        Assert.IsFalse(await HasGroup(user, ScimFixture.Administrators.Id));
    }

    [TestMethod]
    public async Task Create_UnknownMember_Ignored()
    {
        var created = await _f.GroupServiceUnderTest().Create(new ScimGroupData("Shop admins", null, ["nobody"]));
        Assert.AreEqual(ScimOutcome.Created, created.Outcome);
        Assert.AreEqual(0, created.Group.MemberIds.Count);
    }

    [TestMethod]
    public async Task SetMapping_Administrators_MembersGetIt()
    {
        var user = await User();
        var group = await Group("Shop admins", user);

        var outcome = await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.Administrators.Id, null, null);

        Assert.AreEqual(ScimOutcome.Ok, outcome);
        Assert.IsTrue(await HasGroup(user, ScimFixture.Administrators.Id));
    }

    [TestMethod]
    public async Task AddMember_ToMappedGroup_GetsRole_RemoveMember_LosesIt()
    {
        var user = await User();
        var group = await Group("Shop admins");
        await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.Administrators.Id, null, null);

        await _f.GroupServiceUnderTest().ModifyMembers(group, [user], [], false);
        Assert.IsTrue(await HasGroup(user, ScimFixture.Administrators.Id));

        await _f.GroupServiceUnderTest().ModifyMembers(group, [], [user], false);
        Assert.IsFalse(await HasGroup(user, ScimFixture.Administrators.Id));
    }

    //removing a mapping must take its role away again, or every member keeps admin rights forever
    [TestMethod]
    public async Task ClearMapping_RevokesRole()
    {
        var user = await User();
        var group = await Group("Shop admins", user);
        await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.Administrators.Id, null, null);

        await _f.GroupServiceUnderTest().SetMapping(group, "", null, null);

        Assert.IsFalse(await HasGroup(user, ScimFixture.Administrators.Id));
    }

    [TestMethod]
    public async Task ChangeMapping_SwapsRoles()
    {
        var user = await User();
        var group = await Group("Shop admins", user);
        await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.Administrators.Id, null, null);

        await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.Custom.Id, null, null);

        Assert.IsFalse(await HasGroup(user, ScimFixture.Administrators.Id));
        Assert.IsTrue(await HasGroup(user, ScimFixture.Custom.Id));
    }

    [TestMethod]
    public async Task DeleteGroup_RevokesRole()
    {
        var user = await User();
        var group = await Group("Shop admins", user);
        await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.Administrators.Id, null, null);

        Assert.AreEqual(ScimOutcome.Ok, await _f.GroupServiceUnderTest().Delete(group));

        Assert.IsFalse(await HasGroup(user, ScimFixture.Administrators.Id));
    }

    [TestMethod]
    public async Task StoreManagerMapping_SetsStaffStore()
    {
        var user = await User();
        var group = await Group("Store A staff", user);

        await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.StoreManager.Id, "storeA", null);

        var customer = await _f.Reload(user);
        Assert.IsTrue(customer.Groups.Contains(ScimFixture.StoreManager.Id));
        Assert.AreEqual("storeA", customer.StaffStoreId);
    }

    [TestMethod]
    public async Task AdministratorsAndStoreManager_ConflictGrantsNeither()
    {
        var user = await User();
        var admins = await Group("Shop admins", user);
        var staff = await Group("Store A staff", user);
        await _f.GroupServiceUnderTest().SetMapping(admins, ScimFixture.Administrators.Id, null, null);

        await _f.GroupServiceUnderTest().SetMapping(staff, ScimFixture.StoreManager.Id, "storeA", null);

        var customer = await _f.Reload(user);
        Assert.IsFalse(customer.Groups.Contains(ScimFixture.Administrators.Id));
        Assert.IsFalse(customer.Groups.Contains(ScimFixture.StoreManager.Id));
        Assert.IsTrue(string.IsNullOrEmpty(customer.StaffStoreId));
        CollectionAssert.Contains((await _f.UserService().GetConflicts()).ToList(), user);
    }

    [TestMethod]
    public async Task SetMapping_SalesManager_Invalid()
    {
        var group = await Group("Sales");
        Assert.AreEqual(ScimOutcome.Invalid,
            await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.SalesManager.Id, null, null));
    }

    [TestMethod]
    [DataRow("g-registered")]
    [DataRow("g-guests")]
    [DataRow("no-such-group")]
    public async Task SetMapping_NotMappable_Invalid(string customerGroupId)
    {
        var group = await Group("Some");
        Assert.AreEqual(ScimOutcome.Invalid,
            await _f.GroupServiceUnderTest().SetMapping(group, customerGroupId, null, null));
    }

    [TestMethod]
    public async Task SetMapping_StoreManagerWithoutStore_Invalid()
    {
        var group = await Group("Store staff");
        Assert.AreEqual(ScimOutcome.Invalid,
            await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.StoreManager.Id, "", null));
    }

    [TestMethod]
    public async Task SetMapping_StoreManagerUnknownStore_Invalid()
    {
        _f.StoreService.Setup(x => x.GetStoreById("missing")).ReturnsAsync((Store)null);
        var group = await Group("Store staff");
        Assert.AreEqual(ScimOutcome.Invalid,
            await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.StoreManager.Id, "missing", null));
    }

    [TestMethod]
    public async Task SetMapping_VendorsWithoutVendor_Invalid()
    {
        var group = await Group("Vendor staff");
        Assert.AreEqual(ScimOutcome.Invalid,
            await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.Vendors.Id, null, null));
    }

    [TestMethod]
    public async Task SetMapping_CustomGroupWithStore_Invalid()
    {
        var group = await Group("Some");
        Assert.AreEqual(ScimOutcome.Invalid,
            await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.Custom.Id, "storeA", null));
    }

    [TestMethod]
    public async Task ReplaceMembers_ReplacesList()
    {
        var jan = await User();
        var ola = await User("ola@example.com", "oid-2");
        var group = await Group("Shop admins", jan);
        await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.Administrators.Id, null, null);

        await _f.GroupServiceUnderTest().ModifyMembers(group, [ola], [], true);

        Assert.IsFalse(await HasGroup(jan, ScimFixture.Administrators.Id));
        Assert.IsTrue(await HasGroup(ola, ScimFixture.Administrators.Id));
    }

    [TestMethod]
    public async Task List_FilterByDisplayName_CaseInsensitive()
    {
        await Group("Shop admins");
        await Group("Store staff");
        var page = await _f.GroupServiceUnderTest().List("SHOP admins", null, 0, 10);
        Assert.AreEqual("Shop admins", page.Items.Single().DisplayName);
    }

    [TestMethod]
    public async Task GetMappings_ListsGroupsWithMemberCount()
    {
        var user = await User();
        var group = await Group("Shop admins", user);
        await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.Administrators.Id, null, null);

        var mapping = (await _f.GroupServiceUnderTest().GetMappings()).Single();

        Assert.AreEqual("Shop admins", mapping.DisplayName);
        Assert.AreEqual(1, mapping.MemberCount);
        Assert.AreEqual(ScimFixture.Administrators.Id, mapping.CustomerGroupId);
    }

    //the mapping screen offers only the targets SetMapping accepts
    [TestMethod]
    public async Task GetMappableGroups_BackOfficeAndCustomOnly()
    {
        var ids = (await _f.GroupServiceUnderTest().GetMappableGroups()).Select(x => x.Id).ToList();
        CollectionAssert.AreEquivalent(new[] {
            ScimFixture.Administrators.Id, ScimFixture.StoreManager.Id, ScimFixture.Vendors.Id, ScimFixture.Custom.Id,
            ScimFixture.OtherCustom.Id
        }, ids);
    }

    //a break-glass account keeps its roles whatever the provider's groups say
    [TestMethod]
    public async Task BreakGlassMember_LeavingGroup_KeepsAdministrators()
    {
        var user = await User();
        var group = await Group("Shop admins", user);
        await _f.GroupServiceUnderTest().SetMapping(group, ScimFixture.Administrators.Id, null, null);
        _f.LoginConfig.BreakGlassAccounts = ["jan@example.com"];

        await _f.GroupServiceUnderTest().ModifyMembers(group, [], [user], false);

        Assert.IsTrue(await HasGroup(user, ScimFixture.Administrators.Id));
    }

    //review I5: one membership change recomputes the members it changed, not the whole group
    [TestMethod]
    public async Task ModifyMembers_SyncsOnlyChangedMembers()
    {
        var jan = await User();
        var ola = await User("ola@example.com", "oid-2");
        var ewa = await User("ewa@example.com", "oid-3");
        var group = await Group("Shop admins", jan, ola);
        var spy = new SyncSpy(_f);

        await _f.GroupServiceUnderTest(spy).ModifyMembers(group, [ewa], [ola], false);

        CollectionAssert.AreEquivalent(new[] { ewa, ola }, spy.Synced);
    }

    [TestMethod]
    public async Task CreateAndDelete_PublishEntityEvents()
    {
        var group = await Group("Shop admins");
        await _f.GroupServiceUnderTest().Delete(group);

        _f.Mediator.Verify(x => x.Publish(It.IsAny<EntityInserted<ScimGroup>>(), It.IsAny<CancellationToken>()), Times.Once);
        _f.Mediator.Verify(x => x.Publish(It.IsAny<EntityDeleted<ScimGroup>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private class SyncSpy(ScimFixture f) : ScimRoleSync(f.Users, f.Groups, f.CustomerService.Object, f.GroupService.Object,
        f.LoginConfig, f.Mediator.Object, NullLogger<ScimRoleSync>.Instance)
    {
        public List<string> Synced { get; } = [];

        public override Task SyncMembers(IEnumerable<string> customerIds)
        {
            var ids = customerIds.ToList();
            Synced.AddRange(ids);
            return base.SyncMembers(ids);
        }
    }
}
