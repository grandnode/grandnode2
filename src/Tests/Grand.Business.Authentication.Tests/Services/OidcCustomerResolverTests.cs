using Grand.Business.Authentication.Services;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
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
            _mediator.Object, _config, _customerConfig, NullLogger<OidcCustomerResolver>.Instance);

    private static ClaimsPrincipal Principal(string sub = "sub-1", string email = "John@Example.com",
        string emailVerified = "true")
    {
        var claims = new List<Claim>();
        if (sub != null) claims.Add(new Claim("sub", sub));
        if (email != null) claims.Add(new Claim("email", email));
        if (emailVerified != null) claims.Add(new Claim("email_verified", emailVerified));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Oidc"));
    }

    private Task Link(string customerId, string sub = "sub-1", string provider = "Oidc") =>
        _links.InsertAsync(new ExternalAuthentication { CustomerId = customerId, ProviderSystemName = provider, ExternalIdentifier = sub });

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
    public async Task ExistingCustomer_NotPerStore_GlobalLookup_Linked()
    {
        _customerService.Setup(x => x.GetCustomerByEmail("John@Example.com", "")).ReturnsAsync(new Customer { Id = "c1", Active = true, StoreId = "store2" });

        Assert.IsTrue((await Resolver().Resolve(Principal(), "store1")).Succeeded);

        var link = await _links.GetOneAsync(x => x.ExternalIdentifier == "sub-1");
        Assert.AreEqual("c1", link.CustomerId);
        Assert.AreEqual("Oidc", link.ProviderSystemName);
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
}
