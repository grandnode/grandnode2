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
        _customerSettings = new CustomerSettings { UserRegistrationType = UserRegistrationType.Standard };
        _settingService.Setup(x => x.LoadSetting<CustomerSettings>(It.IsAny<string>())).ReturnsAsync(_customerSettings);
        _customerService.Setup(x => x.InsertGuestCustomer(It.IsAny<Customer>()))
            .ReturnsAsync((Customer c) => { c.Id = "new1"; return c; });
    }

    private OidcCustomerResolver Resolver(IRepository<ExternalAuthentication> links = null) =>
        new(links ?? _links, _customerService.Object, _customerManagerService.Object, _settingService.Object,
            _groupService.Object, _permissionService.Object, _mediator.Object, _config, _customerConfig, NullLogger<OidcCustomerResolver>.Instance);

    private Task Link(string customerId, string sub = "sub-1", string provider = OidcCustomerResolver.ProviderSystemName) =>
        _links.InsertAsync(new ExternalAuthentication { CustomerId = customerId, ProviderSystemName = provider, ExternalIdentifier = sub });


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
        Assert.AreEqual(OidcLinkResult.Linked, await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "A@B.c"));
        var link = await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1");
        Assert.AreEqual("c1", link.CustomerId);
        Assert.AreEqual("a@b.c", link.Email);
    }

    [TestMethod]
    public async Task LinkIdentity_SameCustomerTwice_Linked()
    {
        await Link("c1");
        Assert.AreEqual(OidcLinkResult.Linked, await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c"));
    }

    [TestMethod]
    public async Task LinkIdentity_SubjectOfOtherCustomer_Refused()
    {
        await Link("c2");
        Assert.AreEqual(OidcLinkResult.SubjectLinkedToOtherCustomer,
            await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c"));
    }

    [TestMethod]
    public async Task LinkIdentity_CustomerHasOtherSubject_Refused()
    {
        await Link("c1", "other-sub");
        Assert.AreEqual(OidcLinkResult.CustomerLinkedToOtherSubject,
            await Resolver().LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c"));
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
            await Resolver(links.Object).LinkIdentity(new Customer { Id = "c1" }, "sub-1", "a@b.c"));
    }
}
