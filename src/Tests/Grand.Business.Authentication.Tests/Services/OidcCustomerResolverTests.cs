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
public class OidcCustomerResolverTests
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
        _config = new OidcResourceServerConfig { Enabled = true, AutoRegister = false };
        _customerConfig = new CustomerConfig();
        _customerSettings = new CustomerSettings { UserRegistrationType = UserRegistrationType.Standard };
        _settingService.Setup(x => x.LoadSetting<CustomerSettings>(It.IsAny<string>())).ReturnsAsync(_customerSettings);
        _customerService.Setup(x => x.InsertGuestCustomer(It.IsAny<Customer>()))
            .ReturnsAsync((Customer c) => { c.Id = "new1"; return c; });
    }

    private OidcCustomerResolver Resolver(IRepository<ExternalAuthentication> links = null) =>
        new(links ?? _links, _customerService.Object, _customerManagerService.Object, _settingService.Object,
            _groupService.Object, _permissionService.Object, _mediator.Object, _config, _customerConfig, NullLogger<OidcCustomerResolver>.Instance);

    private static ClaimsPrincipal Principal(string sub = "sub-1", string email = "John@Example.com",
        string emailVerified = "true")
    {
        var claims = new List<Claim>();
        if (sub != null) claims.Add(new Claim("sub", sub));
        if (email != null) claims.Add(new Claim("email", email));
        if (emailVerified != null) claims.Add(new Claim("email_verified", emailVerified));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Oidc"));
    }

    private Task Link(string customerId, string sub = "sub-1", string provider = OidcCustomerResolver.ProviderSystemName,
        string storeId = null) =>
        _links.InsertAsync(new ExternalAuthentication {
            CustomerId = customerId, ProviderSystemName = provider, ExternalIdentifier = sub, StoreId = storeId
        });

    private List<ExternalAuthentication> LinksOf(string sub) =>
        _links.Table.Where(x => x.ExternalIdentifier == sub).ToList();

    [TestMethod]
    public async Task NoSubject_InvalidToken() =>
        Assert.AreEqual(OidcFailureKind.InvalidToken, (await Resolver().Resolve(Principal(sub: null), "store1")).Failure);

    [TestMethod]
    public async Task CustomSubjectClaim_IsUsed()
    {
        _config.Claims.Subject = "oid";
        await Link("c1", sub: "abc");
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true });
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "abc")], "Oidc"));
        Assert.AreEqual("c1", (await Resolver().Resolve(principal, "store1")).Customer.Id);
    }

    [TestMethod]
    public async Task Linked_ReturnsCustomer_WithoutEmail()
    {
        await Link("c1");
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true });

        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(email: null), "store1")).Customer.Id);
        _customerService.Verify(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task LinkOfAnotherProvider_Ignored()
    {
        await Link("c9", provider: "ExternalAuth.Google");
        Assert.IsFalse((await Resolver().Resolve(Principal(), "store1")).Succeeded);
        _customerService.Verify(x => x.GetCustomerById("c9"), Times.Never);
    }

    [TestMethod]
    public async Task SubjectIsCaseSensitive()
    {
        await Link("c1", sub: "ABC");
        Assert.IsFalse((await Resolver().Resolve(Principal(sub: "abc"), "store1")).Succeeded);
        _customerService.Verify(x => x.GetCustomerById("c1"), Times.Never);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, true)]
    public async Task LinkedInactiveOrDeleted_Forbidden(bool active, bool deleted)
    {
        await Link("c1");
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = active, Deleted = deleted });
        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(), "store1")).Failure);
    }

    [TestMethod]
    public async Task LinkedMissingCustomer_Forbidden()
    {
        await Link("gone");
        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(), "store1")).Failure);
    }

    [TestMethod]
    public async Task NoEmail_InvalidToken() =>
        Assert.AreEqual(OidcFailureKind.InvalidToken, (await Resolver().Resolve(Principal(email: null), "store1")).Failure);

    [TestMethod]
    [DataRow(null)]
    [DataRow("false")]
    public async Task EmailNotVerified_InvalidToken_NoLookup(string verified)
    {
        Assert.AreEqual(OidcFailureKind.InvalidToken, (await Resolver().Resolve(Principal(emailVerified: verified), "store1")).Failure);
        _customerService.Verify(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [DataRow("true")]
    [DataRow("True")]
    public async Task EmailVerified_AnyCase(string verified)
    {
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true });
        Assert.IsTrue((await Resolver().Resolve(Principal(emailVerified: verified), "store1")).Succeeded);
    }

    [TestMethod]
    public async Task VerificationNotRequired()
    {
        _config.RequireEmailVerified = false;
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true });
        Assert.IsTrue((await Resolver().Resolve(Principal(emailVerified: null), "store1")).Succeeded);
    }

    [TestMethod]
    public void AutoRegister_DefaultsToFalse() => Assert.IsFalse(new OidcResourceServerConfig().AutoRegister);

    [TestMethod]
    public async Task ExistingCustomer_NotPerStore_GlobalLookup_Linked()
    {
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store2" });

        Assert.IsTrue((await Resolver().Resolve(Principal(), "store1")).Succeeded);

        var link = await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1");
        Assert.AreEqual("c1", link.CustomerId);
        Assert.AreEqual("ExternalAuth.OpenIdConnect", link.ProviderSystemName);
        Assert.AreEqual("john@example.com", link.Email);
    }

    [TestMethod]
    public async Task ExistingCustomer_PerStore_StoreLookup()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "store1")).ReturnsAsync(new Customer { Id = "c1", Active = true });
        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store1")).Customer.Id);
    }

    [TestMethod]
    public async Task ExistingCustomerInactive_Forbidden_NotLinked()
    {
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = false });
        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(), "store1")).Failure);
        Assert.IsNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    [TestMethod]
    public async Task NoCustomer_AutoRegisterOff_Forbidden()
    {
        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(), "store1")).Failure);
        _customerService.Verify(x => x.InsertGuestCustomer(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task NoCustomer_Registers_Links_Publishes()
    {
        _config.AutoRegister = true;
        RegistrationRequest captured = null;
        _customerManagerService.Setup(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>()))
            .Callback<RegistrationRequest>(r => captured = r).Returns(Task.CompletedTask);

        var result = await Resolver().Resolve(Principal(), "store1");

        Assert.AreEqual("new1", result.Customer.Id);
        Assert.AreEqual("john@example.com", captured.Email);
        Assert.AreEqual("john@example.com", captured.Username);
        Assert.AreEqual(PasswordFormat.Hashed, captured.PasswordFormat);
        Assert.AreEqual("store1", captured.StoreId);
        Assert.IsTrue(captured.IsApproved);
        Assert.AreEqual(20, captured.Password.Length);
        Assert.AreEqual("store1", captured.Customer.StoreId);
        Assert.AreEqual("new1", (await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1")).CustomerId);
        _mediator.Verify(x => x.Publish(It.IsAny<CustomerRegisteredEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RegistrationDisabledInStore_Forbidden()
    {
        _config.AutoRegister = true;
        _customerSettings.UserRegistrationType = UserRegistrationType.Disabled;

        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(), "store1")).Failure);
        _settingService.Verify(x => x.LoadSetting<CustomerSettings>("store1"), Times.Once);
        _customerManagerService.Verify(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>()), Times.Never);
    }

    [TestMethod]
    [DataRow(UserRegistrationType.EmailValidation)]
    [DataRow(UserRegistrationType.AdminApproval)]
    public async Task OtherRegistrationTypes_RegisterApproved(UserRegistrationType type)
    {
        _config.AutoRegister = true;
        _customerSettings.UserRegistrationType = type;
        RegistrationRequest captured = null;
        _customerManagerService.Setup(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>()))
            .Callback<RegistrationRequest>(r => captured = r).Returns(Task.CompletedTask);

        Assert.IsTrue((await Resolver().Resolve(Principal(), "store1")).Succeeded);
        Assert.IsTrue(captured.IsApproved);
    }

    [TestMethod]
    public async Task UnverifiedEmail_NeverRegisters()
    {
        _config.AutoRegister = true;
        Assert.AreEqual(OidcFailureKind.InvalidToken, (await Resolver().Resolve(Principal(emailVerified: "false"), "store1")).Failure);
        _customerService.Verify(x => x.InsertGuestCustomer(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task Race_Registration_LoserDeletesOwnAccount_ReturnsWinner_NoEvent()
    {
        _config.AutoRegister = true;
        var links = new Mock<IRepository<ExternalAuthentication>>();
        links.SetupSequence(x => x.GetOneAsync(It.IsAny<Expression<Func<ExternalAuthentication, bool>>>()))
            .ReturnsAsync((ExternalAuthentication)null)
            .ReturnsAsync((ExternalAuthentication)null)
            .ReturnsAsync(new ExternalAuthentication { CustomerId = "winner", ProviderSystemName = "Oidc", ExternalIdentifier = "sub-1" });
        links.Setup(x => x.InsertAsync(It.IsAny<ExternalAuthentication>())).ThrowsAsync(new Exception("duplicate key"));
        _customerService.Setup(x => x.GetCustomerById("winner")).ReturnsAsync(new Customer { Id = "winner", Active = true });

        var result = await Resolver(links.Object).Resolve(Principal(), "store1");

        Assert.AreEqual("winner", result.Customer.Id);
        _customerService.Verify(x => x.DeleteCustomer(It.Is<Customer>(c => c.Id == "new1"), true), Times.Once);
        _mediator.Verify(x => x.Publish(It.IsAny<CustomerRegisteredEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Race_LinkingExisting_ReturnsLinkedCustomer()
    {
        var links = new Mock<IRepository<ExternalAuthentication>>();
        links.SetupSequence(x => x.GetOneAsync(It.IsAny<Expression<Func<ExternalAuthentication, bool>>>()))
            .ReturnsAsync((ExternalAuthentication)null)
            .ReturnsAsync((ExternalAuthentication)null) //no other identity linked to the matched customer
            .ReturnsAsync(new ExternalAuthentication { CustomerId = "c1", ProviderSystemName = "Oidc", ExternalIdentifier = "sub-1" });
        links.Setup(x => x.InsertAsync(It.IsAny<ExternalAuthentication>())).ThrowsAsync(new Exception("duplicate key"));
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true });
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true });

        Assert.AreEqual("c1", (await Resolver(links.Object).Resolve(Principal(), "store1")).Customer.Id);
    }

    [TestMethod]
    public async Task InsertFailsWithoutWinner_Rethrows()
    {
        _config.AutoRegister = true;
        var links = new Mock<IRepository<ExternalAuthentication>>();
        links.Setup(x => x.GetOneAsync(It.IsAny<Expression<Func<ExternalAuthentication, bool>>>())).ReturnsAsync((ExternalAuthentication)null);
        links.Setup(x => x.InsertAsync(It.IsAny<ExternalAuthentication>())).ThrowsAsync(new InvalidOperationException("db down"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Resolver(links.Object).Resolve(Principal(), "store1"));
    }

    [TestMethod]
    public async Task LinkedSystemAccount_Forbidden()
    {
        await Link("c1");
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer {
            Id = "c1", Active = true, IsSystemAccount = true, SystemName = SystemCustomerNames.BackgroundTask
        });
        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(), "store1")).Failure);
    }

    [TestMethod]
    public async Task EmailMatchesGuest_Forbidden_NotLinked()
    {
        var guest = new Customer { Id = "c1", Active = true };
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(guest);
        _groupService.Setup(x => x.IsRegistered(guest)).ReturnsAsync(false);

        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(), "store1")).Failure);
        Assert.IsNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    [TestMethod]
    public async Task EmailMatchesCustomerLinkedToAnotherSubject_Forbidden_NotLinked()
    {
        await Link("c1", sub: "other-sub");
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true });

        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(), "store1")).Failure);
        Assert.IsNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    [TestMethod]
    public async Task UnverifiedEmail_AdminPanelAccount_Forbidden_NotLinked()
    {
        _config.RequireEmailVerified = false;
        var admin = new Customer { Id = "c1", Active = true };
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(admin);
        _permissionService.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, admin)).ReturnsAsync(true);

        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(emailVerified: null), "store1")).Failure);
        Assert.IsNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    [TestMethod]
    [DataRow("vendor1", null)]
    [DataRow(null, "store1")]
    public async Task UnverifiedEmail_VendorOrStaffAccount_Forbidden_NotLinked(string vendorId, string staffStoreId)
    {
        _config.RequireEmailVerified = false;
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer {
            Id = "c1", Active = true, VendorId = vendorId, StaffStoreId = staffStoreId
        });

        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(emailVerified: null), "store1")).Failure);
        Assert.IsNull(await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1"));
    }

    [TestMethod]
    public async Task UnverifiedEmail_PlainRegisteredCustomer_Linked()
    {
        _config.RequireEmailVerified = false;
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true });

        Assert.IsTrue((await Resolver().Resolve(Principal(emailVerified: null), "store1")).Succeeded);
        Assert.AreEqual("c1", (await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1")).CustomerId);
    }

    [TestMethod]
    public async Task VerifiedEmail_BackOfficeAccount_Linked()
    {
        var admin = new Customer { Id = "c1", Active = true, StaffStoreId = "store1" };
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(admin);
        _permissionService.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, admin)).ReturnsAsync(true);

        Assert.IsTrue((await Resolver().Resolve(Principal(), "store1")).Succeeded);
    }

    [TestMethod]
    public async Task PerStore_LinkedCustomerOfAnotherStore_Forbidden()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c1");
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store2" });

        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(), "store1")).Failure);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("store1")]
    public async Task PerStore_LinkedStorelessOrSameStoreCustomer_Allowed(string customerStoreId)
    {
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c1");
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = customerStoreId });

        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store1")).Customer.Id);
    }

    [TestMethod]
    public async Task NotPerStore_LinkedCustomerOfAnotherStore_Allowed()
    {
        await Link("c1");
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store2" });

        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store1")).Customer.Id);
    }

    [TestMethod]
    public async Task UsernameTaken_Forbidden_NoRegistration()
    {
        _config.AutoRegister = true;
        _customerService.Setup(x => x.GetCustomerByUsername("john@example.com", "")).ReturnsAsync(new Customer { Id = "other" });

        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(), "store1")).Failure);
        _customerService.Verify(x => x.InsertGuestCustomer(It.IsAny<Customer>()), Times.Never);
        _customerManagerService.Verify(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>()), Times.Never);
    }

    [TestMethod]
    public async Task UsernameTaken_PerStore_StoreLookup()
    {
        _config.AutoRegister = true;
        _customerConfig.RegisterCustomersPerStore = true;
        _customerService.Setup(x => x.GetCustomerByUsername("john@example.com", "store1")).ReturnsAsync(new Customer { Id = "other" });

        Assert.AreEqual(OidcFailureKind.Forbidden, (await Resolver().Resolve(Principal(), "store1")).Failure);
        _customerService.Verify(x => x.InsertGuestCustomer(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task PerStore_SameSubject_EachStoreLinksAndResolvesItsOwnCustomer()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c1", storeId: "store1");
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store1" });
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "store2")).ReturnsAsync(new Customer { Id = "c2", Active = true, StoreId = "store2" });
        _customerService.Setup(x => x.GetCustomerById("c2")).ReturnsAsync(new Customer { Id = "c2", Active = true, StoreId = "store2" });

        Assert.AreEqual("c2", (await Resolver().Resolve(Principal(), "store2")).Customer.Id);
        Assert.AreEqual("c2", (await Resolver().Resolve(Principal(), "store2")).Customer.Id);
        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store1")).Customer.Id);

        var links = LinksOf("sub-1");
        Assert.HasCount(2, links);
        Assert.AreEqual("store2", links.Single(x => x.CustomerId == "c2").StoreId);
    }

    [TestMethod]
    public async Task PerStore_Registration_LinkCarriesTheStore()
    {
        _config.AutoRegister = true;
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c1", storeId: "store1");
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store1" });

        Assert.AreEqual("new1", (await Resolver().Resolve(Principal(), "store2")).Customer.Id);

        Assert.AreEqual("store2", LinksOf("sub-1").Single(x => x.CustomerId == "new1").StoreId);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task PerStore_GlobalCustomer_ResolvesInEveryStore(string linkStoreId)
    {
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c1", storeId: linkStoreId);
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "" });

        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store1")).Customer.Id);
        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store2")).Customer.Id);
        Assert.HasCount(1, LinksOf("sub-1"));
    }

    [TestMethod]
    public async Task PerStore_GlobalCustomerLinked_WithEmptyStore()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "store1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "" });

        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store1")).Customer.Id);

        Assert.AreEqual("", LinksOf("sub-1").Single().StoreId);
    }

    [TestMethod]
    public async Task PerStore_StoreLinkPreferredOverGlobalLink()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("global", storeId: "");
        await Link("c1", storeId: "store1");
        _customerService.Setup(x => x.GetCustomerById("global")).ReturnsAsync(new Customer { Id = "global", Active = true });
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store1" });

        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store1")).Customer.Id);
        Assert.AreEqual("global", (await Resolver().Resolve(Principal(), "store2")).Customer.Id);
    }

    [TestMethod]
    public async Task NotPerStore_LinkStoredWithoutStore_AndFoundInAnyStore()
    {
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store2" });
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store2" });

        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store1")).Customer.Id);
        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store3")).Customer.Id);

        Assert.AreEqual("", LinksOf("sub-1").Single().StoreId);
    }

    [TestMethod]
    public async Task NotPerStore_LinkWrittenPerStore_StillFoundInAnyStore()
    {
        await Link("c1", storeId: "store2");
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store2" });

        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(email: null), "store1")).Customer.Id);
    }

    [TestMethod]
    public async Task PerStore_Race_RereadsTheWinnerOfTheSameStore()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        //another store's link exists; a concurrent request of this store stores its link first
        var stored = new List<ExternalAuthentication> {
            new() { CustomerId = "c1", ProviderSystemName = OidcCustomerResolver.ProviderSystemName, ExternalIdentifier = "sub-1", StoreId = "store1" }
        };
        var links = new Mock<IRepository<ExternalAuthentication>>();
        links.Setup(x => x.GetOneAsync(It.IsAny<Expression<Func<ExternalAuthentication, bool>>>()))
            .ReturnsAsync((Expression<Func<ExternalAuthentication, bool>> p) => stored.FirstOrDefault(p.Compile()));
        links.Setup(x => x.InsertAsync(It.IsAny<ExternalAuthentication>()))
            .Callback(() => stored.Add(new ExternalAuthentication {
                CustomerId = "winner", ProviderSystemName = OidcCustomerResolver.ProviderSystemName, ExternalIdentifier = "sub-1", StoreId = "store2"
            }))
            .ThrowsAsync(new Exception("duplicate key"));
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "store2")).ReturnsAsync(new Customer { Id = "c2", Active = true, StoreId = "store2" });
        _customerService.Setup(x => x.GetCustomerById("winner")).ReturnsAsync(new Customer { Id = "winner", Active = true, StoreId = "store2" });

        Assert.AreEqual("winner", (await Resolver(links.Object).Resolve(Principal(), "store2")).Customer.Id);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    public async Task PerStore_LegacyGlobalLinkToStoreCustomer_OtherStoreLinksItsOwnCustomer(string legacyStoreId)
    {
        //a link written before customers were registered per store, to a customer of store1
        _customerConfig.RegisterCustomersPerStore = true;
        await Link("c1", storeId: legacyStoreId);
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store1" });
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "store2")).ReturnsAsync(new Customer { Id = "c2", Active = true, StoreId = "store2" });
        _customerService.Setup(x => x.GetCustomerById("c2")).ReturnsAsync(new Customer { Id = "c2", Active = true, StoreId = "store2" });

        Assert.AreEqual("c2", (await Resolver().Resolve(Principal(), "store2")).Customer.Id);
        Assert.AreEqual("c2", (await Resolver().Resolve(Principal(), "store2")).Customer.Id);
        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store1")).Customer.Id);

        var links = LinksOf("sub-1");
        Assert.HasCount(2, links);
        Assert.AreEqual("store2", links.Single(x => x.CustomerId == "c2").StoreId);
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
    public void OidcLinkId_Stable_24LowerHex_DerivedFromCustomer()
    {
        var id = OidcCustomerResolver.OidcLinkId("c1");

        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("ExternalAuth.OpenIdConnect:c1"));
        Assert.AreEqual(Convert.ToHexStringLower(hash, 0, 12), id);
        Assert.AreEqual(id, OidcCustomerResolver.OidcLinkId("c1"));
        Assert.MatchesRegex("^[0-9a-f]{24}$", id);
        Assert.AreNotEqual(id, OidcCustomerResolver.OidcLinkId("c2"));
    }

    [TestMethod]
    public async Task LinkingExisting_StoresLinkWithCustomerDerivedId()
    {
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true });

        Assert.IsTrue((await Resolver().Resolve(Principal(), "store1")).Succeeded);

        Assert.AreEqual(OidcCustomerResolver.OidcLinkId("c1"), LinksOf("sub-1").Single().Id);
    }

    [TestMethod]
    public async Task Registration_StoresLinkWithCustomerDerivedId()
    {
        _config.AutoRegister = true;

        Assert.AreEqual("new1", (await Resolver().Resolve(Principal(), "store1")).Customer.Id);

        Assert.AreEqual(OidcCustomerResolver.OidcLinkId("new1"), LinksOf("sub-1").Single().Id);
    }

    [TestMethod]
    public async Task Race_SameCustomer_SecondSubject_LoserRefused_NoException()
    {
        //both requests pass the "no other identity" check; the other subject stores its link first
        var links = new RacingLinks();
        links.BeforeInsert = () => links.InsertAsync(new ExternalAuthentication {
            Id = OidcCustomerResolver.OidcLinkId("c1"), CustomerId = "c1",
            ProviderSystemName = OidcCustomerResolver.ProviderSystemName, ExternalIdentifier = "other-sub", StoreId = ""
        });
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true });

        var result = await Resolver(links).Resolve(Principal(), "store1");

        Assert.AreEqual(OidcFailureKind.Forbidden, result.Failure);
        Assert.AreEqual("The customer account is linked to another identity", result.Description);
        var stored = links.Table.Where(x => x.CustomerId == "c1").ToList();
        Assert.HasCount(1, stored);
        Assert.AreEqual("other-sub", stored.Single().ExternalIdentifier);
    }

    [TestMethod]
    public async Task Race_SameCustomer_SameSubject_ReturnsCustomer()
    {
        var links = new RacingLinks();
        links.BeforeInsert = () => links.InsertAsync(new ExternalAuthentication {
            Id = OidcCustomerResolver.OidcLinkId("c1"), CustomerId = "c1",
            ProviderSystemName = OidcCustomerResolver.ProviderSystemName, ExternalIdentifier = "sub-1", StoreId = ""
        });
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true });
        _customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Active = true });

        Assert.AreEqual("c1", (await Resolver(links).Resolve(Principal(), "store1")).Customer.Id);
        Assert.HasCount(1, LinksOf("sub-1"));
    }

    [TestMethod]
    public async Task Unlink_ThenOtherSubject_RelinksWithTheSameId()
    {
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true });
        Assert.IsTrue((await Resolver().Resolve(Principal(), "store1")).Succeeded);
        var first = LinksOf("sub-1").Single();

        //My account - remove the external association by its record id
        await _links.DeleteAsync(first);

        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(sub: "sub-2"), "store1")).Customer.Id);
        var relinked = LinksOf("sub-2").Single();
        Assert.AreEqual(first.Id, relinked.Id);
        Assert.AreEqual("c1", relinked.CustomerId);
        Assert.IsEmpty(LinksOf("sub-1"));
    }

    [TestMethod]
    public async Task PerStore_CustomersOfEachStore_GetTheirOwnLinkIds()
    {
        _customerConfig.RegisterCustomersPerStore = true;
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "store1")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store1" });
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "store2")).ReturnsAsync(new Customer { Id = "c2", Active = true, StoreId = "store2" });

        Assert.AreEqual("c1", (await Resolver().Resolve(Principal(), "store1")).Customer.Id);
        Assert.AreEqual("c2", (await Resolver().Resolve(Principal(), "store2")).Customer.Id);

        var links = LinksOf("sub-1");
        Assert.HasCount(2, links);
        Assert.AreEqual(OidcCustomerResolver.OidcLinkId("c1"), links.Single(x => x.StoreId == "store1").Id);
        Assert.AreEqual(OidcCustomerResolver.OidcLinkId("c2"), links.Single(x => x.StoreId == "store2").Id);
    }
}
