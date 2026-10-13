using Grand.Business.Authentication.Services;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class OidcCustomerResolverLoginTests
{
    private IRepository<ExternalAuthentication> _links;
    private Mock<ICustomerService> _customerService;
    private Mock<ICustomerManagerService> _customerManagerService;
    private Mock<ISettingService> _settingService;
    private Mock<IGroupService> _groupService;
    private Mock<IPermissionService> _permissionService;
    private Mock<IMediator> _mediator;
    private OidcResourceServerConfig _config;
    private OidcLoginConfig _loginConfig;
    private CustomerConfig _customerConfig;
    private CustomerSettings _customerSettings;

    [TestInitialize]
    public void Init()
    {
        _links = new MongoDBRepositoryTest<ExternalAuthentication>();
        _customerService = new Mock<ICustomerService>();
        _customerManagerService = new Mock<ICustomerManagerService>();
        _settingService = new Mock<ISettingService>();
        _groupService = new Mock<IGroupService>();
        _groupService.Setup(x => x.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);
        _permissionService = new Mock<IPermissionService>();
        _mediator = new Mock<IMediator>();
        _config = new OidcResourceServerConfig { Enabled = true };
        _customerConfig = new CustomerConfig();
        _loginConfig = new OidcLoginConfig();
        _customerSettings = new CustomerSettings { UserRegistrationType = UserRegistrationType.Standard };
        _settingService.Setup(x => x.LoadSetting<CustomerSettings>(It.IsAny<string>())).ReturnsAsync(_customerSettings);
        _customerService.Setup(x => x.InsertGuestCustomer(It.IsAny<Customer>()))
            .ReturnsAsync((Customer c) => { c.Id = "new1"; return c; });
    }

    private OidcCustomerResolver Resolver(IRepository<ExternalAuthentication> links = null) =>
        new(links ?? _links, _customerService.Object, _customerManagerService.Object, _settingService.Object,
            _groupService.Object, _permissionService.Object, _mediator.Object, _config, _loginConfig, _customerConfig, NullLogger<OidcCustomerResolver>.Instance);

    private Task Link(string customerId, string sub = "sub-1", string provider = OidcCustomerResolver.ProviderSystemName,
        string storeId = null, bool byEmail = false) =>
        _links.InsertAsync(new ExternalAuthentication {
            CustomerId = customerId, ProviderSystemName = provider, ExternalIdentifier = sub, StoreId = storeId,
            LinkedByEmail = byEmail
        });

    private List<ExternalAuthentication> LinksOf(string sub) =>
        _links.Table.Where(x => x.ExternalIdentifier == sub).ToList();


    private const string Store = "store1";

    [TestMethod]
    public async Task NoSubject_Refused_InvalidIdentity() =>
        Assert.AreEqual(OidcLoginRefusal.InvalidIdentity, (await Resolver().ResolveForLogin(null, "a@b.c", true, Store)).Refusal);

    [TestMethod]
    public async Task Linked_SignIn_WithoutEmail()
    {
        var customer = new Customer { Id = "c1", Active = true };
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(customer);
        await Link("c1");
        var result = await Resolver().ResolveForLogin("sub-1", null, false, Store);
        Assert.AreEqual(OidcLoginOutcome.SignIn, result.Outcome);
        Assert.AreSame(customer, result.Customer);
    }

    [TestMethod]
    public async Task LinkedInactive_Refused_AccountInactive()
    {
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = false });
        await Link("c1");
        Assert.AreEqual(OidcLoginRefusal.AccountInactive, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Refusal);
    }

    [TestMethod]
    public async Task NoEmail_Refused_InvalidIdentity() =>
        Assert.AreEqual(OidcLoginRefusal.InvalidIdentity, (await Resolver().ResolveForLogin("sub-1", " ", true, Store)).Refusal);

    [TestMethod]
    public async Task ExistingCustomer_Verified_LinkedAndSignIn()
    {
        var customer = new Customer { Id = "c1", Active = true, Email = "john@example.com" };
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(customer);
        var result = await Resolver().ResolveForLogin("sub-1", "John@Example.com", true, Store);
        Assert.AreEqual(OidcLoginOutcome.SignIn, result.Outcome);
        var link = await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1");
        Assert.AreEqual("c1", link.CustomerId);
        Assert.AreEqual(OidcCustomerResolver.ProviderSystemName, link.ProviderSystemName);
        Assert.IsTrue(link.LinkedByEmail);
    }

    [TestMethod]
    public async Task ExistingCustomer_Unverified_LinkRequired_NotLinked()
    {
        var customer = new Customer { Id = "c1", Active = true };
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(customer);
        var result = await Resolver().ResolveForLogin("sub-1", "a@b.c", false, Store);
        Assert.AreEqual(OidcLoginOutcome.LinkRequired, result.Outcome);
        Assert.AreSame(customer, result.Customer);
        Assert.IsNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    [TestMethod]
    public async Task ExistingAdmin_Verified_LinkRequired_NotLinked()
    {
        var customer = new Customer { Id = "c1", Active = true };
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(customer);
        _permissionService.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, customer)).ReturnsAsync(true);
        Assert.AreEqual(OidcLoginOutcome.LinkRequired, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Outcome);
        Assert.IsNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    [TestMethod]
    [DataRow("v1", null)]
    [DataRow(null, "s1")]
    public async Task ExistingVendorOrStoreStaff_Verified_LinkRequired(string vendorId, string staffStoreId)
    {
        var customer = new Customer { Id = "c1", Active = true, VendorId = vendorId, StaffStoreId = staffStoreId };
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(customer);
        Assert.AreEqual(OidcLoginOutcome.LinkRequired, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Outcome);
        Assert.IsNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    private static Customer TwoFactorCustomer()
    {
        var customer = new Customer { Id = "c1", Active = true };
        customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.TwoFactorEnabled, Value = "True", StoreId = "" });
        return customer;
    }

    [TestMethod]
    public async Task ExistingTwoFactorCustomer_Verified_LinkRequired_NotLinked()
    {
        _customerSettings.TwoFactorAuthenticationEnabled = true;
        var customer = TwoFactorCustomer();
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(customer);
        var result = await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store);
        Assert.AreEqual(OidcLoginOutcome.LinkRequired, result.Outcome);
        Assert.AreSame(customer, result.Customer);
        Assert.IsNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    [TestMethod]
    public async Task ExistingTwoFactorCustomer_StoreTwoFactorOff_Verified_SignIn()
    {
        _customerSettings.TwoFactorAuthenticationEnabled = false;
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(TwoFactorCustomer());
        Assert.AreEqual(OidcLoginOutcome.SignIn, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Outcome);
        Assert.IsNotNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    [TestMethod]
    public async Task LinkedTwoFactorCustomer_SignIn()
    {
        _customerSettings.TwoFactorAuthenticationEnabled = true;
        var customer = TwoFactorCustomer();
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(customer);
        await Link("c1");
        Assert.AreEqual(OidcLoginOutcome.SignIn, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Outcome);
    }

    [TestMethod]
    public async Task LinkMadeByEmail_PlainCustomer_Verified_SignIn()
    {
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true });
        await Link("c1", byEmail: true);
        Assert.AreEqual(OidcLoginOutcome.SignIn, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Outcome);
    }

    [TestMethod]
    public async Task LinkMadeByEmail_Unverified_LinkRequired()
    {
        //an access token linked the account by an unverified e-mail (RequireEmailVerified off)
        var customer = new Customer { Id = "c1", Active = true };
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(customer);
        await Link("c1", byEmail: true);
        var result = await Resolver().ResolveForLogin("sub-1", "a@b.c", false, Store);
        Assert.AreEqual(OidcLoginOutcome.LinkRequired, result.Outcome);
        Assert.AreSame(customer, result.Customer);
    }

    [TestMethod]
    public async Task LinkMadeByEmail_TwoFactorCustomer_LinkRequired()
    {
        _customerSettings.TwoFactorAuthenticationEnabled = true;
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(TwoFactorCustomer());
        await Link("c1", byEmail: true);
        Assert.AreEqual(OidcLoginOutcome.LinkRequired, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Outcome);
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("vendor1", null)]
    [DataRow(null, "store1")]
    public async Task LinkMadeByEmail_BackOfficeAccount_LinkRequired(string vendorId, string staffStoreId)
    {
        //linked while it was an ordinary customer, given back-office access later
        var customer = new Customer { Id = "c1", Active = true, VendorId = vendorId, StaffStoreId = staffStoreId };
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(customer);
        if (vendorId == null && staffStoreId == null)
            _permissionService.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, customer)).ReturnsAsync(true);
        await Link("c1", byEmail: true);
        Assert.AreEqual(OidcLoginOutcome.LinkRequired, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Outcome);
    }

    [TestMethod]
    public async Task LinkedAdmin_SignIn()
    {
        var customer = new Customer { Id = "c1", Active = true };
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(customer);
        _permissionService.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, customer)).ReturnsAsync(true);
        await Link("c1");
        Assert.AreEqual(OidcLoginOutcome.SignIn, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Outcome);
    }

    [TestMethod]
    public async Task ProviderOnly_Staff_LinkedByVerifiedEmail()
    {
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        var staff = new Customer { Id = "c1", Active = true, StaffStoreId = "s1" };
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(staff);
        var result = await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store);
        Assert.AreEqual(OidcLoginOutcome.SignIn, result.Outcome);
        Assert.AreEqual("c1", result.Customer.Id);
        Assert.IsTrue((await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1")).LinkedByEmail);
    }

    [TestMethod]
    public async Task ProviderOnly_TwoFactor_LinkedByVerifiedEmail()
    {
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        _customerSettings.TwoFactorAuthenticationEnabled = true;
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(TwoFactorCustomer());
        var result = await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store);
        Assert.AreEqual(OidcLoginOutcome.SignIn, result.Outcome);
        Assert.IsNotNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    [TestMethod]
    public async Task ProviderOnly_UnverifiedEmail_ExistingAccount_Refused()
    {
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Customer { Id = "c1", Active = true });
        var result = await Resolver().ResolveForLogin("sub-1", "a@b.c", false, Store);
        Assert.AreEqual(OidcLoginRefusal.EmailNotVerified, result.Refusal);
        Assert.IsNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    [TestMethod]
    public async Task ProviderOnly_UnverifiedEmail_Unknown_RefusedNotRegistered()
    {
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        var result = await Resolver().ResolveForLogin("sub-new", "new@shop.com", false, Store);
        Assert.AreEqual(OidcLoginOutcome.Refused, result.Outcome);
        Assert.AreEqual(OidcLoginRefusal.EmailNotVerified, result.Refusal);
    }

    [TestMethod]
    public async Task ProviderOnly_VerifiedEmail_Unknown_RegistrationRequired()
    {
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        Assert.AreEqual(OidcLoginOutcome.RegistrationRequired,
            (await Resolver().ResolveForLogin("sub-new", "new@shop.com", true, Store)).Outcome);
    }

    [TestMethod]
    public async Task ProviderOnly_LinkMadeByEmail_StaffSignsIn()
    {
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        _customerService.Setup(x => x.GetCustomerById("c1"))
            .ReturnsAsync(new Customer { Id = "c1", Active = true, StaffStoreId = "s1" });
        await Link("c1", byEmail: true);
        Assert.AreEqual(OidcLoginOutcome.SignIn, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Outcome);
    }

    [TestMethod]
    public async Task ProviderOnly_LinkMadeByEmail_Unverified_SignsIn()
    {
        //the link exists; the provider's later claims do not re-open the first-link rules
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true });
        await Link("c1", byEmail: true);
        Assert.AreEqual(OidcLoginOutcome.SignIn, (await Resolver().ResolveForLogin("sub-1", "a@b.c", false, Store)).Outcome);
    }

    [TestMethod]
    public async Task ProviderOnly_ExistingAccount_LinkedToOtherSubject_Refused()
    {
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Customer { Id = "c1", Active = true });
        await Link("c1", "other-sub");
        Assert.AreEqual(OidcLoginRefusal.LinkedToOtherIdentity, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Refusal);
    }

    [TestMethod]
    public async Task Mixed_Staff_LinkRequired()
    {
        var staff = new Customer { Id = "c1", Active = true, StaffStoreId = "s1" };
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(staff);
        Assert.AreEqual(OidcLoginOutcome.LinkRequired, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Outcome);
    }

    [TestMethod]
    public async Task ExistingCustomerLinkedToOtherSubject_Refused()
    {
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Customer { Id = "c1", Active = true });
        await Link("c1", "other-sub");
        Assert.AreEqual(OidcLoginRefusal.LinkedToOtherIdentity, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Refusal);
    }

    [TestMethod]
    public async Task ExistingGuestOrSystem_Refused_AccountUnusable()
    {
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Customer { Id = "c1", Active = true });
        _groupService.Setup(x => x.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(false);
        Assert.AreEqual(OidcLoginRefusal.AccountUnusable, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Refusal);
    }

    [TestMethod]
    public async Task PerStore_EmailLookupScopedToStore()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store);
        _customerService.Verify(x => x.GetCustomerByEmail("a@b.c", Store), Times.Once);
    }

    [TestMethod]
    public async Task NoCustomer_RegistrationRequired_NothingCreated_EvenWithAutoRegister()
    {
        _config.AutoRegister = true;
        Assert.AreEqual(OidcLoginOutcome.RegistrationRequired, (await Resolver().ResolveForLogin("sub-1", "a@b.c", false, Store)).Outcome);
        _customerService.Verify(x => x.InsertGuestCustomer(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task NoCustomer_RegistrationDisabled_Refused()
    {
        _customerSettings.UserRegistrationType = UserRegistrationType.Disabled;
        Assert.AreEqual(OidcLoginRefusal.RegistrationDisabled, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Refusal);
    }

    [TestMethod]
    public async Task LinkIdentity_StoresLink()
    {
        Assert.AreEqual(OidcLinkResult.Linked, await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "A@B.c", Store));
        var link = await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1");
        Assert.AreEqual("c1", link.CustomerId);
        Assert.AreEqual("a@b.c", link.Email);
    }

    [TestMethod]
    public async Task LinkIdentity_NotMadeByEmail()
    {
        await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c", Store);
        Assert.IsFalse((await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1")).LinkedByEmail);
    }

    [TestMethod]
    public async Task LinkIdentity_LinkMadeByEmail_NowMadeWithPassword()
    {
        //the password sign-in that LinkRequired asked for stands behind the existing link
        await Link("c1", byEmail: true);
        Assert.AreEqual(OidcLinkResult.Linked, await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c", Store));
        Assert.IsFalse((await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1")).LinkedByEmail);
    }

    [TestMethod]
    public async Task LinkIdentity_SameCustomerTwice_Linked()
    {
        await Link("c1");
        Assert.AreEqual(OidcLinkResult.Linked, await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c", Store));
    }

    [TestMethod]
    public async Task LinkIdentity_SubjectOfOtherCustomer_Refused()
    {
        await Link("c2");
        Assert.AreEqual(OidcLinkResult.SubjectLinkedToOtherCustomer,
            await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c", Store));
    }

    [TestMethod]
    public async Task LinkIdentity_CustomerHasOtherSubject_Refused()
    {
        await Link("c1", "other-sub");
        Assert.AreEqual(OidcLinkResult.CustomerLinkedToOtherSubject,
            await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c", Store));
    }

    [TestMethod]
    public async Task LinkIdentity_LostRace_ReturnsSubjectLinkedToOtherCustomer()
    {
        var links = new Mock<IRepository<ExternalAuthentication>>();
        links.SetupSequence(x => x.GetOneAsync(It.IsAny<Expression<Func<ExternalAuthentication, bool>>>()))
            .ReturnsAsync((ExternalAuthentication)null) //subject not linked
            .ReturnsAsync((ExternalAuthentication)null) //customer has no other link
            .ReturnsAsync(new ExternalAuthentication { CustomerId = "winner", ExternalIdentifier = "sub-1" });
        links.Setup(x => x.InsertAsync(It.IsAny<ExternalAuthentication>())).ThrowsAsync(new InvalidOperationException("duplicate"));
        Assert.AreEqual(OidcLinkResult.SubjectLinkedToOtherCustomer,
            await Resolver(links.Object).LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c", Store));
    }

    [TestMethod]
    public async Task ResolveForLogin_PerStore_SubjectLinkedInAnotherStore_SignsInThisStoresCustomer()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c9", storeId: "store2");
        var customer = new Customer { Id = "c1", Active = true, StoreId = Store };
        _customerService.Setup(x => x.GetCustomerByEmail("a@b.c", Store)).ReturnsAsync(customer);

        var result = await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store);

        Assert.AreEqual(OidcLoginOutcome.SignIn, result.Outcome);
        Assert.AreEqual("c1", result.Customer.Id);
        Assert.AreEqual(Store, LinksOf("sub-1").Single(x => x.CustomerId == "c1").StoreId);
        _customerService.Verify(x => x.GetCustomerById("c9"), Times.Never);
    }

    [TestMethod]
    public async Task ResolveForLogin_PerStore_GlobalCustomerLink_SignsInInEveryStore()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c1", storeId: "");
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "" });

        Assert.AreEqual(OidcLoginOutcome.SignIn, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store)).Outcome);
        Assert.AreEqual(OidcLoginOutcome.SignIn, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, "store2")).Outcome);
    }

    [TestMethod]
    public async Task LinkIdentity_PerStore_SubjectLinkedInAnotherStore_LinksThisStore()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c9", storeId: "store2");

        Assert.AreEqual(OidcLinkResult.Linked,
            await Resolver().LinkIdentity(new Customer { Id = "c1", StoreId = Store }, "sub-1", "a@b.c", Store));
        Assert.AreEqual(Store, LinksOf("sub-1").Single(x => x.CustomerId == "c1").StoreId);
    }

    [TestMethod]
    public async Task LinkIdentity_PerStore_GlobalCustomer_SubjectLinkedInThisStore_Refused()
    {
        //the store's link takes precedence in this store, so a link of the global account would never be used here
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c9", storeId: Store);

        Assert.AreEqual(OidcLinkResult.SubjectLinkedToOtherCustomer,
            await Resolver().LinkIdentity(new Customer { Id = "c1", StoreId = "" }, "sub-1", "a@b.c", Store));
        Assert.HasCount(1, LinksOf("sub-1"));
    }

    [TestMethod]
    public async Task LinkIdentity_NotPerStore_LinkWithoutStore()
    {
        Assert.AreEqual(OidcLinkResult.Linked,
            await Resolver().LinkIdentity(new Customer { Id = "c1", StoreId = Store }, "sub-1", "a@b.c", Store));
        Assert.AreEqual("", LinksOf("sub-1").Single().StoreId);
    }

    [TestMethod]
    public async Task ResolveForLogin_PerStore_LegacyGlobalLinkToOtherStoreCustomer_LinksThisStoresCustomer()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c9", storeId: "");
        _customerService.Setup(x => x.GetCustomerById("c9")).ReturnsAsync(new Customer { Id = "c9", Active = true, StoreId = "store2" });
        _customerService.Setup(x => x.GetCustomerByEmail("a@b.c", Store)).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = Store });

        var result = await Resolver().ResolveForLogin("sub-1", "a@b.c", true, Store);

        Assert.AreEqual(OidcLoginOutcome.SignIn, result.Outcome);
        Assert.AreEqual("c1", result.Customer.Id);
        Assert.AreEqual(Store, LinksOf("sub-1").Single(x => x.CustomerId == "c1").StoreId);
        Assert.AreEqual(OidcLoginOutcome.SignIn, (await Resolver().ResolveForLogin("sub-1", "a@b.c", true, "store2")).Outcome);
    }

    [TestMethod]
    public async Task LinkIdentity_PerStore_LegacyGlobalLinkToOtherStoreCustomer_LinksThisStore()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c9", storeId: null);
        _customerService.Setup(x => x.GetCustomerById("c9")).ReturnsAsync(new Customer { Id = "c9", Active = true, StoreId = "store2" });

        Assert.AreEqual(OidcLinkResult.Linked,
            await Resolver().LinkIdentity(new Customer { Id = "c1", StoreId = Store }, "sub-1", "a@b.c", Store));
        Assert.HasCount(2, LinksOf("sub-1"));
    }
    /// <summary>
    ///     Repository that lets a concurrent request store its record right before this one's insert
    /// </summary>
    private sealed class RacingLinks : MongoDBRepositoryTest<ExternalAuthentication>
    {
        public Func<Task> BeforeInsert { get; set; }

        public override async Task<ExternalAuthentication> InsertAsync(ExternalAuthentication entity)
        {
            var before = BeforeInsert;
            BeforeInsert = null;
            if (before != null)
                await before();
            return await base.InsertAsync(entity);
        }
    }

    [TestMethod]
    public async Task LinkIdentity_StoresLinkWithCustomerDerivedId()
    {
        Assert.AreEqual(OidcLinkResult.Linked, await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c", Store));
        Assert.AreEqual(OidcCustomerResolver.OidcLinkId("c1"), LinksOf("sub-1").Single().Id);
    }

    [TestMethod]
    public async Task LinkIdentity_LostRaceToOtherSubjectOfTheCustomer_CustomerLinkedToOtherSubject()
    {
        var links = new Mock<IRepository<ExternalAuthentication>>();
        links.Setup(x => x.GetOneAsync(It.IsAny<Expression<Func<ExternalAuthentication, bool>>>()))
            .ReturnsAsync((ExternalAuthentication)null);
        links.Setup(x => x.InsertAsync(It.IsAny<ExternalAuthentication>())).ThrowsAsync(new InvalidOperationException("duplicate"));
        links.Setup(x => x.GetByIdAsync(OidcCustomerResolver.OidcLinkId("c1"))).ReturnsAsync(new ExternalAuthentication {
            Id = OidcCustomerResolver.OidcLinkId("c1"), CustomerId = "c1",
            ProviderSystemName = OidcCustomerResolver.ProviderSystemName, ExternalIdentifier = "other-sub"
        });

        Assert.AreEqual(OidcLinkResult.CustomerLinkedToOtherSubject,
            await Resolver(links.Object).LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c", Store));
    }

    [TestMethod]
    public async Task ResolveForLogin_LostRaceToOtherSubjectOfTheCustomer_RefusedLinkedToOtherIdentity()
    {
        var links = new RacingLinks();
        links.BeforeInsert = () => links.InsertAsync(new ExternalAuthentication {
            Id = OidcCustomerResolver.OidcLinkId("c1"), CustomerId = "c1",
            ProviderSystemName = OidcCustomerResolver.ProviderSystemName, ExternalIdentifier = "other-sub", StoreId = ""
        });
        _customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Customer { Id = "c1", Active = true });

        var result = await Resolver(links).ResolveForLogin("sub-1", "a@b.c", true, Store);

        Assert.AreEqual(OidcLoginRefusal.LinkedToOtherIdentity, result.Refusal);
        var stored = links.Table.Where(x => x.CustomerId == "c1").ToList();
        Assert.HasCount(1, stored);
        Assert.AreEqual("other-sub", stored.Single().ExternalIdentifier);
    }

    [TestMethod]
    public async Task LinkIdentity_AfterUnlink_RelinksWithTheSameId()
    {
        Assert.AreEqual(OidcLinkResult.Linked, await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c", Store));
        var first = LinksOf("sub-1").Single();
        await _links.DeleteAsync(first);

        Assert.AreEqual(OidcLinkResult.Linked, await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-2", "a@b.c", Store));
        Assert.AreEqual(first.Id, LinksOf("sub-2").Single().Id);
    }
}
