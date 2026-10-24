using Grand.Business.Authentication.Services;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services.Scim;

[TestClass]
public class ScimUserServiceTests
{
    private ScimFixture _f;

    [TestInitialize]
    public void Init() => _f = new ScimFixture();

    private async Task MapGroup(string customerGroupId, params string[] members) =>
        await _f.Groups.InsertAsync(new ScimGroup {
            DisplayName = "IdP " + customerGroupId, CustomerGroupId = customerGroupId, Members = members.ToList()
        });

    [TestMethod]
    public async Task Create_NewUser_StorelessRegisteredActiveAndLinked()
    {
        var result = await _f.UserService().Create(ScimFixture.Data(userName: "Jan@Example.com"));

        Assert.AreEqual(ScimOutcome.Created, result.Outcome);
        var customer = await _f.Reload(result.User.Id);
        Assert.AreEqual("jan@example.com", customer.Email);
        Assert.AreEqual("", customer.StoreId);
        Assert.IsTrue(customer.Active);
        Assert.IsTrue(customer.Groups.Contains(ScimFixture.Registered.Id));
        Assert.IsNotNull(await _f.Users.GetByIdAsync(customer.Id));
        _f.CustomerManager.Verify(x => x.RegisterCustomer(It.Is<RegistrationRequest>(r => r.StoreId == "")), Times.Once);
        _f.Resolver.Verify(x => x.LinkIdentity(It.Is<Customer>(c => c.Id == customer.Id), "oid-1", "jan@example.com", ""),
            Times.Once);
        Assert.AreEqual("oid-1", result.User.ExternalId);
        Assert.AreEqual("Jan", result.User.GivenName);
    }

    [TestMethod]
    public async Task Create_UserNameNotAnEmail_UsesWorkEmail()
    {
        var result = await _f.UserService().Create(ScimFixture.Data(userName: "jnowak", email: "jan@example.com"));
        Assert.AreEqual(ScimOutcome.Created, result.Outcome);
        Assert.AreEqual("jan@example.com", (await _f.Reload(result.User.Id)).Email);
    }

    [TestMethod]
    public async Task Create_NoEmailAnywhere_Invalid()
    {
        var result = await _f.UserService().Create(ScimFixture.Data(userName: "jnowak"));
        Assert.AreEqual(ScimOutcome.Invalid, result.Outcome);
        Assert.AreEqual(0, _f.Customers.Table.Count());
    }

    [TestMethod]
    public async Task Create_ExternalIdNotSubject_NoLink()
    {
        _f.Config.ExternalIdIsSubject = false;
        var result = await _f.UserService().Create(ScimFixture.Data());
        Assert.AreEqual(ScimOutcome.Created, result.Outcome);
        _f.Resolver.Verify(x => x.LinkIdentity(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Create_SameExternalIdTwice_Conflict()
    {
        await _f.UserService().Create(ScimFixture.Data());
        var second = await _f.UserService().Create(ScimFixture.Data(userName: "other@example.com"));
        Assert.AreEqual(ScimOutcome.Conflict, second.Outcome);
    }

    //the user ruled: SCIM takes over an existing account and manages it from then on
    [TestMethod]
    public async Task Create_ExistingAdminInNoMappedGroup_TakenOverLosesAdministratorsKeepsHandGroup()
    {
        var existing = await _f.AddCustomer("jan@example.com", ScimFixture.Administrators.Id, ScimFixture.OtherCustom.Id);

        var result = await _f.UserService().Create(ScimFixture.Data());

        Assert.AreEqual(ScimOutcome.Created, result.Outcome);
        Assert.AreEqual(existing.Id, result.User.Id);
        var customer = await _f.Reload(existing.Id);
        Assert.IsFalse(customer.Groups.Contains(ScimFixture.Administrators.Id));
        Assert.IsTrue(customer.Groups.Contains(ScimFixture.OtherCustom.Id));
        Assert.IsTrue(customer.Groups.Contains(ScimFixture.Registered.Id));
        _f.CustomerManager.Verify(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>()), Times.Never);
    }

    [TestMethod]
    public async Task Create_ExistingStoreManager_StaffStoreTakenOverAndCleared()
    {
        var existing = await _f.AddCustomer("jan@example.com", ScimFixture.StoreManager.Id);
        await _f.CustomerService.Object.UpdateCustomerField(existing.Id, x => x.StaffStoreId, "storeA");

        await _f.UserService().Create(ScimFixture.Data());

        var customer = await _f.Reload(existing.Id);
        Assert.IsFalse(customer.Groups.Contains(ScimFixture.StoreManager.Id));
        Assert.IsTrue(string.IsNullOrEmpty(customer.StaffStoreId));
    }

    [TestMethod]
    public async Task Create_BreakGlassAccount_ConflictNothingWritten()
    {
        _f.LoginConfig.BreakGlassAccounts = ["jan@example.com"];
        var existing = await _f.AddCustomer("jan@example.com", ScimFixture.Administrators.Id);

        var result = await _f.UserService().Create(ScimFixture.Data());

        Assert.AreEqual(ScimOutcome.Conflict, result.Outcome);
        Assert.IsNull(await _f.Users.GetByIdAsync(existing.Id));
        Assert.IsTrue((await _f.Reload(existing.Id)).Groups.Contains(ScimFixture.Administrators.Id));
    }

    [TestMethod]
    public async Task Create_SystemAccount_Conflict()
    {
        var existing = await _f.AddCustomer("jan@example.com");
        await _f.Customers.UpdateField(existing.Id, x => x.IsSystemAccount, true);
        Assert.AreEqual(ScimOutcome.Conflict, (await _f.UserService().Create(ScimFixture.Data())).Outcome);
    }

    [TestMethod]
    public async Task Create_AccountLinkedToOtherSubject_Conflict()
    {
        var existing = await _f.AddCustomer("jan@example.com", ScimFixture.Administrators.Id);
        await _f.Links.InsertAsync(new ExternalAuthentication {
            CustomerId = existing.Id, ExternalIdentifier = "other-sub",
            ProviderSystemName = OidcCustomerResolver.ProviderSystemName
        });
        Assert.AreEqual(ScimOutcome.Conflict, (await _f.UserService().Create(ScimFixture.Data())).Outcome);
        Assert.IsNull(await _f.Users.GetByIdAsync(existing.Id));
    }

    [TestMethod]
    public async Task Create_InGroupMappedToAdministrators_GetsAdministrators()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await MapGroup(ScimFixture.Administrators.Id, created.User.Id);

        await _f.UserService().Replace(created.User.Id, ScimFixture.Data());

        Assert.IsTrue((await _f.Reload(created.User.Id)).Groups.Contains(ScimFixture.Administrators.Id));
    }

    [TestMethod]
    public async Task Replace_Inactive_LocksOut()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());

        var result = await _f.UserService().Replace(created.User.Id, ScimFixture.Data(active: false));

        Assert.AreEqual(ScimOutcome.Ok, result.Outcome);
        Assert.IsFalse(result.User.Active);
        Assert.IsFalse((await _f.Reload(created.User.Id)).Active);
        Assert.IsTrue((await _f.Users.GetByIdAsync(created.User.Id)).DeactivatedByScim);
        _f.Sessions.Verify(x => x.EndByCustomer(created.User.Id), Times.Once);
        _f.Revocation.Verify(x => x.RevokeTokens("oid-1"), Times.Once);
        _f.CustomerService.Verify(x => x.UpdateUserField(It.Is<Customer>(c => c.Id == created.User.Id),
            SystemCustomerFieldNames.RefreshToken, "", ""), Times.Once);
    }

    [TestMethod]
    public async Task Replace_ActiveAfterScimDeactivation_Reactivates()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await _f.UserService().Replace(created.User.Id, ScimFixture.Data(active: false));

        var result = await _f.UserService().Replace(created.User.Id, ScimFixture.Data(active: true));

        Assert.IsTrue(result.User.Active);
        Assert.IsTrue((await _f.Reload(created.User.Id)).Active);
        Assert.IsFalse((await _f.Users.GetByIdAsync(created.User.Id)).DeactivatedByScim);
    }

    //an administrator who disabled the account by hand decides when it comes back
    [TestMethod]
    public async Task Replace_ActiveOnHandDisabledAccount_StaysInactive()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await _f.Customers.UpdateField(created.User.Id, x => x.Active, false);

        var result = await _f.UserService().Replace(created.User.Id, ScimFixture.Data(active: true));

        Assert.IsFalse(result.User.Active);
        Assert.IsFalse((await _f.Reload(created.User.Id)).Active);
    }

    [TestMethod]
    public async Task Replace_EmailOfAnotherCustomer_Conflict()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await _f.AddCustomer("taken@example.com");

        var result = await _f.UserService().Replace(created.User.Id, ScimFixture.Data(userName: "taken@example.com"));

        Assert.AreEqual(ScimOutcome.Conflict, result.Outcome);
        Assert.AreEqual("jan@example.com", (await _f.Reload(created.User.Id)).Email);
    }

    [TestMethod]
    public async Task Replace_NewEmail_UpdatesEmailAndUsername()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await _f.UserService().Replace(created.User.Id, ScimFixture.Data(userName: "Jan.Nowak@Example.com"));
        var customer = await _f.Reload(created.User.Id);
        Assert.AreEqual("jan.nowak@example.com", customer.Email);
        Assert.AreEqual("jan.nowak@example.com", customer.Username);
    }

    [TestMethod]
    public async Task Replace_Unknown_NotFound() =>
        Assert.AreEqual(ScimOutcome.NotFound, (await _f.UserService().Replace("nope", ScimFixture.Data())).Outcome);

    [TestMethod]
    public async Task Delete_DeactivatesRevokesRolesUnlinksAndKeepsCustomer()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await MapGroup(ScimFixture.Administrators.Id, created.User.Id);
        await _f.UserService().Replace(created.User.Id, ScimFixture.Data());

        var outcome = await _f.UserService().Delete(created.User.Id);

        Assert.AreEqual(ScimOutcome.Ok, outcome);
        var customer = await _f.Reload(created.User.Id);
        Assert.IsNotNull(customer);
        Assert.IsFalse(customer.Active);
        Assert.IsFalse(customer.Groups.Contains(ScimFixture.Administrators.Id));
        Assert.IsTrue((await _f.Users.GetByIdAsync(created.User.Id)).Deprovisioned);
        Assert.AreEqual(0, _f.Links.Table.Count(x => x.CustomerId == created.User.Id));
        Assert.IsFalse(_f.Groups.Table.First().Members.Contains(created.User.Id));
        Assert.AreEqual(ScimOutcome.NotFound, (await _f.UserService().Get(created.User.Id)).Outcome);
    }

    [TestMethod]
    public async Task List_FilterByUserName_CaseInsensitive()
    {
        await _f.UserService().Create(ScimFixture.Data());
        await _f.UserService().Create(ScimFixture.Data(userName: "ola@example.com", externalId: "oid-2"));

        var page = await _f.UserService().List("JAN@example.com", null, 0, 10);

        Assert.AreEqual(1, page.Total);
        Assert.AreEqual("jan@example.com", page.Items.Single().UserName);
    }

    //Entra's connection test filters for a random user name and expects an empty list, not an error
    [TestMethod]
    public async Task List_UnknownUserName_Empty()
    {
        var page = await _f.UserService().List(Guid.NewGuid().ToString(), null, 0, 10);
        Assert.AreEqual(0, page.Total);
        Assert.AreEqual(0, page.Items.Count);
    }

    [TestMethod]
    public async Task List_FilterByExternalId()
    {
        await _f.UserService().Create(ScimFixture.Data());
        await _f.UserService().Create(ScimFixture.Data(userName: "ola@example.com", externalId: "oid-2"));
        var page = await _f.UserService().List(null, "oid-2", 0, 10);
        Assert.AreEqual("ola@example.com", page.Items.Single().UserName);
    }

    //a customer who is not SCIM-managed is never listed, even with a matching e-mail
    [TestMethod]
    public async Task List_UnmanagedCustomer_NotListed()
    {
        await _f.AddCustomer("jan@example.com");
        Assert.AreEqual(0, (await _f.UserService().List("jan@example.com", null, 0, 10)).Total);
    }

    //review H1: a taken-over account keeps the resolver's rules (password and second factor for staff) - SCIM vouches only
    //for accounts it created, or anyone able to edit an e-mail at the provider would sign in as the account's owner
    [TestMethod]
    public async Task Create_TakenOverAccount_NotLinkedAhead()
    {
        await _f.AddCustomer("jan@example.com", ScimFixture.Administrators.Id);

        var result = await _f.UserService().Create(ScimFixture.Data());

        Assert.AreEqual(ScimOutcome.Created, result.Outcome);
        _f.Resolver.Verify(x => x.LinkIdentity(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        Assert.IsFalse((await _f.Users.GetByIdAsync(result.User.Id)).RegisteredByScim);
    }

    //only staff accounts are taken over: a shopper with the e-mail is not the provider's to manage
    [TestMethod]
    public async Task Create_ShopperAccount_Conflict()
    {
        var shopper = await _f.AddCustomer("jan@example.com");

        Assert.AreEqual(ScimOutcome.Conflict, (await _f.UserService().Create(ScimFixture.Data())).Outcome);
        Assert.IsNull(await _f.Users.GetByIdAsync(shopper.Id));
    }

    [TestMethod]
    public async Task Delete_BreakGlassSinceProvisioning_ConflictAccountUntouched()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        _f.LoginConfig.BreakGlassAccounts = ["jan@example.com"];

        Assert.AreEqual(ScimOutcome.Conflict, await _f.UserService().Delete(created.User.Id));
        Assert.IsTrue((await _f.Reload(created.User.Id)).Active);
        Assert.IsNotNull(await _f.Users.GetByIdAsync(created.User.Id));
    }

    //a managed account must not move onto a break-glass address: it would get the Admin password sign-in out of SCIM's reach
    [TestMethod]
    public async Task Replace_ToBreakGlassEmail_Conflict()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        _f.LoginConfig.BreakGlassAccounts = ["rescue@example.com"];

        var result = await _f.UserService().Replace(created.User.Id, ScimFixture.Data(userName: "rescue@example.com"));

        Assert.AreEqual(ScimOutcome.Conflict, result.Outcome);
        Assert.AreEqual("jan@example.com", (await _f.Reload(created.User.Id)).Email);
    }

    //a refused replace writes nothing
    [TestMethod]
    public async Task Replace_NewEmailWithTakenExternalId_ConflictEmailUnchanged()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await _f.UserService().Create(ScimFixture.Data(userName: "ola@example.com", externalId: "oid-2"));

        var result = await _f.UserService().Replace(created.User.Id,
            ScimFixture.Data(userName: "jan.nowak@example.com", externalId: "oid-2"));

        Assert.AreEqual(ScimOutcome.Conflict, result.Outcome);
        Assert.AreEqual("jan@example.com", (await _f.Reload(created.User.Id)).Email);
    }

    //Entra deactivates, deletes, and creates the user again when it is restored or reassigned
    [TestMethod]
    public async Task Create_AfterDeleteOfScimDeactivatedAccount_ActiveAgain()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await _f.UserService().Replace(created.User.Id, ScimFixture.Data(active: false));
        await _f.UserService().Delete(created.User.Id);

        var again = await _f.UserService().Create(ScimFixture.Data());

        Assert.AreEqual(ScimOutcome.Created, again.Outcome);
        Assert.IsTrue((await _f.Reload(created.User.Id)).Active);
    }

    [TestMethod]
    public async Task Create_AfterDeleteOfHandDisabledAccount_StaysInactive()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await _f.Customers.UpdateField(created.User.Id, x => x.Active, false);
        await _f.UserService().Delete(created.User.Id);

        await _f.UserService().Create(ScimFixture.Data());

        Assert.IsFalse((await _f.Reload(created.User.Id)).Active);
    }

    //review I4: a sync holding an older copy of the record must not undo a deactivation written meanwhile
    [TestMethod]
    public async Task Sync_WithStaleRecord_KeepsDeactivatedByScim()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        var stale = await _f.Users.GetByIdAsync(created.User.Id);
        await _f.UserService().Replace(created.User.Id, ScimFixture.Data(active: false));

        await _f.RoleSync().Sync(await _f.Reload(created.User.Id), stale);

        Assert.IsTrue((await _f.Users.GetByIdAsync(created.User.Id)).DeactivatedByScim);
    }

    //an old cookie or API token must not come back to life when the account is activated again
    [TestMethod]
    public async Task Deactivate_RotatesPasswordToken()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());

        await _f.UserService().Replace(created.User.Id, ScimFixture.Data(active: false));

        _f.CustomerService.Verify(x => x.UpdateUserField(It.Is<Customer>(c => c.Id == created.User.Id),
            SystemCustomerFieldNames.PasswordToken, It.Is<string>(v => IsGuid(v)), ""), Times.Once);
    }

    [TestMethod]
    public async Task CreateAndDelete_PublishEntityEvents()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await _f.UserService().Delete(created.User.Id);

        _f.Mediator.Verify(x => x.Publish(It.IsAny<EntityInserted<ScimUser>>(), It.IsAny<CancellationToken>()), Times.Once);
        _f.Mediator.Verify(x => x.Publish(It.IsAny<EntityUpdated<ScimUser>>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    //a deleted user is gone for SCIM but recognised when the provider creates it again
    [TestMethod]
    public async Task Delete_UserNotListedNorCounted()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await _f.UserService().Delete(created.User.Id);

        Assert.AreEqual(0, (await _f.UserService().List(null, "oid-1", 0, 10)).Total);
        Assert.AreEqual(0, await _f.UserService().Count());
        Assert.AreEqual(ScimOutcome.NotFound, (await _f.UserService().Replace(created.User.Id, ScimFixture.Data())).Outcome);
    }

    [TestMethod]
    public async Task Create_AfterDelete_SameAccountAndLinkedAgain()
    {
        var created = await _f.UserService().Create(ScimFixture.Data());
        await _f.UserService().Delete(created.User.Id);

        var again = await _f.UserService().Create(ScimFixture.Data());

        Assert.AreEqual(created.User.Id, again.User.Id);
        Assert.AreEqual(1, _f.Links.Table.Count(x => x.CustomerId == created.User.Id));
    }

    //an employee's shopping account on the company's domain is taken over: it keeps its orders and its password, and connects
    //once with that password - it is not linked ahead
    [TestMethod]
    public async Task Create_ShopperOnTakeOverDomain_TakenOverNotLinkedAhead()
    {
        _f.Config.TakeOverDomains = ["example.com"];
        var shopper = await _f.AddCustomer("jan@example.com");

        var result = await _f.UserService().Create(ScimFixture.Data());

        Assert.AreEqual(ScimOutcome.Created, result.Outcome);
        Assert.AreEqual(shopper.Id, result.User.Id);
        _f.Resolver.Verify(x => x.LinkIdentity(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        _f.CustomerManager.Verify(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>()), Times.Never);
    }

    [TestMethod]
    public async Task Create_ShopperOnOtherDomain_Conflict()
    {
        _f.Config.TakeOverDomains = ["firma.pl"];
        var shopper = await _f.AddCustomer("jan@example.com");

        Assert.AreEqual(ScimOutcome.Conflict, (await _f.UserService().Create(ScimFixture.Data())).Outcome);
        Assert.IsNull(await _f.Users.GetByIdAsync(shopper.Id));
    }

    private static bool IsGuid(string value) => Guid.TryParse(value, out _);
}
