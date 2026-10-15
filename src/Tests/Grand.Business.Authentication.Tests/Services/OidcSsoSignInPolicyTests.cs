using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class OidcSsoSignInPolicyTests
{
    private IRepository<ExternalAuthentication> _links;
    private Mock<IExternalAuthenticationProvider> _provider;
    private ExternalAuthenticationSettings _settings;
    private OidcLoginConfig _config;
    private OidcResourceServerConfig _resourceServer;
    private IMemoryCache _cache;
    private Mock<IOidcLinkService> _linkService;
    private readonly Customer _customer = new() { Id = "c1" };

    [TestInitialize]
    public void Init()
    {
        _links = new MongoDBRepositoryTest<ExternalAuthentication>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _linkService = new Mock<IOidcLinkService>();
        _linkService.Setup(x => x.LinksMatchProvider()).ReturnsAsync(true);
        _provider = new Mock<IExternalAuthenticationProvider>();
        _provider.Setup(x => x.SystemName).Returns(OidcCustomerResolver.ProviderSystemName);
        _provider.Setup(x => x.FriendlyName).Returns("OpenID Connect");
        _settings = new ExternalAuthenticationSettings { ActiveAuthenticationMethodSystemNames = [OidcCustomerResolver.ProviderSystemName] };
        _config = new OidcLoginConfig { ClientId = "shop", SsoOnly = true };
        _resourceServer = new OidcResourceServerConfig { Authority = "https://idp" };
    }

    private OidcSsoSignInPolicy Policy(params IExternalAuthenticationProvider[] providers) =>
        new(_links, providers.Length > 0 ? providers : [_provider.Object], _settings, _config, _resourceServer,
            _cache, _linkService.Object, NullLogger<OidcSsoSignInPolicy>.Instance);

    private Task Link(bool byEmail) => _links.InsertAsync(new ExternalAuthentication {
        CustomerId = "c1", ProviderSystemName = OidcCustomerResolver.ProviderSystemName, ExternalIdentifier = "sub-1",
        LinkedByEmail = byEmail
    });

    //every account with a confirmed link, not only back-office ones
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ConfirmedLink_ProviderRequired_WhateverTheAccount(bool staff)
    {
        await Link(byEmail: false);
        var decision = await Policy().PasswordSignIn(_customer, SignInSurface.Storefront);
        Assert.IsFalse(decision.Allowed);
        Assert.AreSame(_provider.Object, decision.Provider);
    }

    //the provider sign-in is refused until the links are reviewed: requiring it would lock the account out
    [TestMethod]
    public async Task ConfirmedLink_ProviderChanged_PasswordAllowed()
    {
        await Link(byEmail: false);
        _linkService.Setup(x => x.LinksMatchProvider()).ReturnsAsync(false);
        Assert.IsTrue((await Policy().PasswordSignIn(_customer, SignInSurface.Storefront)).Allowed);
    }

    //break-glass: an account never connected to the provider keeps its password
    [TestMethod]
    public async Task WithoutLink_PasswordAllowed() => Assert.IsTrue((await Policy().PasswordSignIn(_customer, SignInSurface.Storefront)).Allowed);

    //"connect your account" needs the password
    [TestMethod]
    public async Task LinkedByEmail_PasswordAllowed()
    {
        await Link(byEmail: true);
        Assert.IsTrue((await Policy().PasswordSignIn(_customer, SignInSurface.Storefront)).Allowed);
    }

    [TestMethod]
    public async Task OptionOff_PasswordAllowed()
    {
        _config.SsoOnly = false;
        await Link(byEmail: false);
        Assert.IsTrue((await Policy().PasswordSignIn(_customer, SignInSurface.Storefront)).Allowed);
    }

    [TestMethod]
    public async Task MethodInactive_PasswordAllowed()
    {
        _settings.ActiveAuthenticationMethodSystemNames = [];
        await Link(byEmail: false);
        Assert.IsTrue((await Policy().PasswordSignIn(_customer, SignInSurface.Storefront)).Allowed);
    }

    [TestMethod]
    public async Task PluginNotInstalled_PasswordAllowed()
    {
        await Link(byEmail: false);
        Assert.IsTrue((await new OidcSsoSignInPolicy(_links, [], _settings, _config, _resourceServer,
            _cache, _linkService.Object, NullLogger<OidcSsoSignInPolicy>.Instance).PasswordSignIn(_customer, SignInSurface.Storefront)).Allowed);
    }

    [TestMethod]
    public async Task NotConfigured_PasswordAllowed()
    {
        _config.ClientId = "";
        await Link(byEmail: false);
        Assert.IsTrue((await Policy().PasswordSignIn(_customer, SignInSurface.Storefront)).Allowed);
    }

    private void ProviderOnly(params string[] breakGlass)
    {
        _config.SignInMode = OidcSignInMode.ProviderOnly;
        _config.BreakGlassAccounts = breakGlass;
    }

    //Mixed keeps the stage B answer whatever the surface
    [TestMethod]
    [DataRow(SignInSurface.Storefront)]
    [DataRow(SignInSurface.AdminPanel)]
    public async Task Mixed_PasswordSignIn_MatchesStageB(SignInSurface surface)
    {
        Assert.IsTrue((await Policy().PasswordSignIn(_customer, surface)).Allowed);
        await Link(byEmail: false);
        var decision = await Policy().PasswordSignIn(_customer, surface);
        Assert.IsFalse(decision.Allowed);
        Assert.AreSame(_provider.Object, decision.Provider);
    }

    //every account, connected or not
    [TestMethod]
    [DataRow(SignInSurface.Storefront)]
    [DataRow(SignInSurface.AdminPanel)]
    [DataRow(SignInSurface.StorePanel)]
    [DataRow(SignInSurface.VendorPanel)]
    [DataRow(SignInSurface.FrontendApi)]
    public async Task ProviderOnly_NoLink_ProviderRequired(SignInSurface surface)
    {
        ProviderOnly();
        _customer.Email = "user@shop.com";
        var decision = await Policy().PasswordSignIn(_customer, surface);
        Assert.IsFalse(decision.Allowed);
        Assert.AreSame(_provider.Object, decision.Provider);
    }

    [TestMethod]
    public async Task ProviderOnly_BreakGlass_AdminPanel_PasswordAllowed()
    {
        ProviderOnly("admin@shop.com");
        _customer.Email = "Admin@Shop.com";
        var decision = await Policy().PasswordSignIn(_customer, SignInSurface.AdminPanel);
        Assert.IsTrue(decision.Allowed);
        Assert.IsTrue(decision.BreakGlass);
    }

    [TestMethod]
    [DataRow(SignInSurface.Storefront)]
    [DataRow(SignInSurface.StorePanel)]
    [DataRow(SignInSurface.VendorPanel)]
    [DataRow(SignInSurface.FrontendApi)]
    public async Task ProviderOnly_BreakGlass_OtherSurface_ProviderRequired(SignInSurface surface)
    {
        ProviderOnly("admin@shop.com");
        _customer.Email = "admin@shop.com";
        var decision = await Policy().PasswordSignIn(_customer, surface);
        Assert.IsFalse(decision.Allowed);
        Assert.AreSame(_provider.Object, decision.Provider);
    }

    //fail closed: no working provider never re-opens the password
    [TestMethod]
    public async Task ProviderOnly_PluginMissing_RefusedWithoutProvider()
    {
        ProviderOnly();
        var decision = await new OidcSsoSignInPolicy(_links, [], _settings, _config, _resourceServer,
            _cache, _linkService.Object, NullLogger<OidcSsoSignInPolicy>.Instance).PasswordSignIn(_customer, SignInSurface.Storefront);
        Assert.IsFalse(decision.Allowed);
        Assert.IsNull(decision.Provider);
    }

    //every refused request would log it otherwise
    [TestMethod]
    public async Task ProviderOnly_PluginMissing_LoggedOncePerWindow()
    {
        ProviderOnly();
        var logger = new Mock<ILogger<OidcSsoSignInPolicy>>();
        var policy = new OidcSsoSignInPolicy(_links, [], _settings, _config, _resourceServer, _cache, _linkService.Object, logger.Object);
        await policy.PasswordSignIn(_customer, SignInSurface.Storefront);
        await policy.PasswordSignIn(_customer, SignInSurface.Storefront);
        logger.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task ProviderOnly_MethodInactive_RefusedWithoutProvider()
    {
        ProviderOnly();
        _settings.ActiveAuthenticationMethodSystemNames = [];
        var decision = await Policy().PasswordSignIn(_customer, SignInSurface.Storefront);
        Assert.IsFalse(decision.Allowed);
        Assert.IsNull(decision.Provider);
    }

    [TestMethod]
    public async Task ProviderOnly_NotConfigured_BreakGlassStillAllowedOnAdmin()
    {
        ProviderOnly("admin@shop.com");
        _config.ClientId = "";
        _customer.Email = "admin@shop.com";
        Assert.IsTrue((await Policy().PasswordSignIn(_customer, SignInSurface.AdminPanel)).Allowed);
        _customer.Email = "user@shop.com";
        Assert.IsFalse((await Policy().PasswordSignIn(_customer, SignInSurface.AdminPanel)).Allowed);
    }

    [TestMethod]
    public async Task NullCustomer_PasswordAllowed() =>
        Assert.IsTrue((await Policy().PasswordSignIn(null, SignInSurface.Storefront)).Allowed);
}
