using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
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
    private readonly Customer _customer = new() { Id = "c1" };

    [TestInitialize]
    public void Init()
    {
        _links = new MongoDBRepositoryTest<ExternalAuthentication>();
        _provider = new Mock<IExternalAuthenticationProvider>();
        _provider.Setup(x => x.SystemName).Returns(OidcCustomerResolver.ProviderSystemName);
        _provider.Setup(x => x.FriendlyName).Returns("OpenID Connect");
        _settings = new ExternalAuthenticationSettings { ActiveAuthenticationMethodSystemNames = [OidcCustomerResolver.ProviderSystemName] };
        _config = new OidcLoginConfig { ClientId = "shop", SsoOnly = true };
        _resourceServer = new OidcResourceServerConfig { Authority = "https://idp" };
    }

    private OidcSsoSignInPolicy Policy(params IExternalAuthenticationProvider[] providers) =>
        new(_links, providers.Length > 0 ? providers : [_provider.Object], _settings, _config, _resourceServer);

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
        Assert.AreSame(_provider.Object, await Policy().RequiredProvider(_customer));
    }

    //break-glass: an account never connected to the provider keeps its password
    [TestMethod]
    public async Task WithoutLink_PasswordAllowed() => Assert.IsNull(await Policy().RequiredProvider(_customer));

    //"connect your account" needs the password
    [TestMethod]
    public async Task LinkedByEmail_PasswordAllowed()
    {
        await Link(byEmail: true);
        Assert.IsNull(await Policy().RequiredProvider(_customer));
    }

    [TestMethod]
    public async Task OptionOff_PasswordAllowed()
    {
        _config.SsoOnly = false;
        await Link(byEmail: false);
        Assert.IsNull(await Policy().RequiredProvider(_customer));
    }

    [TestMethod]
    public async Task MethodInactive_PasswordAllowed()
    {
        _settings.ActiveAuthenticationMethodSystemNames = [];
        await Link(byEmail: false);
        Assert.IsNull(await Policy().RequiredProvider(_customer));
    }

    [TestMethod]
    public async Task PluginNotInstalled_PasswordAllowed()
    {
        await Link(byEmail: false);
        Assert.IsNull(await new OidcSsoSignInPolicy(_links, [], _settings, _config, _resourceServer)
            .RequiredProvider(_customer));
    }

    [TestMethod]
    public async Task NotConfigured_PasswordAllowed()
    {
        _config.ClientId = "";
        await Link(byEmail: false);
        Assert.IsNull(await Policy().RequiredProvider(_customer));
    }
}
