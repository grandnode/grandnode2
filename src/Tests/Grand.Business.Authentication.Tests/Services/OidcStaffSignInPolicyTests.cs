using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class OidcStaffSignInPolicyTests
{
    private IRepository<ExternalAuthentication> _links;
    private Mock<IPermissionService> _permissions;
    private Mock<IExternalAuthenticationProvider> _provider;
    private ExternalAuthenticationSettings _settings;
    private OidcLoginConfig _config;
    private OidcResourceServerConfig _resourceServer;
    private bool _staff;
    private readonly Customer _customer = new() { Id = "c1" };

    [TestInitialize]
    public void Init()
    {
        _links = new MongoDBRepositoryTest<ExternalAuthentication>();
        _permissions = new Mock<IPermissionService>();
        _permissions.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, It.IsAny<Customer>())).ReturnsAsync(() => _staff);
        _provider = new Mock<IExternalAuthenticationProvider>();
        _provider.Setup(x => x.SystemName).Returns(OidcCustomerResolver.ProviderSystemName);
        _provider.Setup(x => x.FriendlyName).Returns("OpenID Connect");
        _settings = new ExternalAuthenticationSettings { ActiveAuthenticationMethodSystemNames = [OidcCustomerResolver.ProviderSystemName] };
        _config = new OidcLoginConfig { ClientId = "shop", StaffSsoOnly = true };
        _resourceServer = new OidcResourceServerConfig { Authority = "https://idp" };
        _staff = true;
    }

    private OidcStaffSignInPolicy Policy(params IExternalAuthenticationProvider[] providers) =>
        new(_links, _permissions.Object, providers.Length > 0 ? providers : [_provider.Object], _settings, _config, _resourceServer);

    private Task Link(bool byEmail) => _links.InsertAsync(new ExternalAuthentication {
        CustomerId = "c1", ProviderSystemName = OidcCustomerResolver.ProviderSystemName, ExternalIdentifier = "sub-1",
        LinkedByEmail = byEmail
    });

    [TestMethod]
    public async Task StaffWithConfirmedLink_ProviderRequired()
    {
        await Link(byEmail: false);
        Assert.AreSame(_provider.Object, await Policy().RequiredProvider(_customer));
    }

    //break-glass: a back-office account never connected to the provider keeps its password
    [TestMethod]
    public async Task StaffWithoutLink_PasswordAllowed() => Assert.IsNull(await Policy().RequiredProvider(_customer));

    //"connect your account" needs the password
    [TestMethod]
    public async Task StaffLinkedByEmail_PasswordAllowed()
    {
        await Link(byEmail: true);
        Assert.IsNull(await Policy().RequiredProvider(_customer));
    }

    [TestMethod]
    public async Task Customer_PasswordAllowed()
    {
        _staff = false;
        await Link(byEmail: false);
        Assert.IsNull(await Policy().RequiredProvider(_customer));
    }

    [TestMethod]
    public async Task OptionOff_PasswordAllowed()
    {
        _config.StaffSsoOnly = false;
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
        Assert.IsNull(await new OidcStaffSignInPolicy(_links, _permissions.Object, [], _settings, _config, _resourceServer)
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
