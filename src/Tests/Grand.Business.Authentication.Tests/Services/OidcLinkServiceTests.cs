using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class OidcLinkServiceTests
{
    private const string Oidc = OidcCustomerResolver.ProviderSystemName;

    private IRepository<ExternalAuthentication> _links;
    private Mock<ISettingService> _settings;
    private Mock<IOidcSessionService> _sessions;
    private OidcLinkSettings _stored;
    private OidcResourceServerConfig _config;

    [TestInitialize]
    public void Init()
    {
        _links = new MongoDBRepositoryTest<ExternalAuthentication>();
        _stored = new OidcLinkSettings();
        _settings = new Mock<ISettingService>();
        _settings.Setup(x => x.LoadSetting<OidcLinkSettings>(It.IsAny<string>())).ReturnsAsync(() => _stored);
        _settings.Setup(x => x.SaveSetting(It.IsAny<OidcLinkSettings>(), It.IsAny<string>()))
            .Callback((OidcLinkSettings s, string _) => _stored = s).Returns(Task.CompletedTask);
        _sessions = new Mock<IOidcSessionService>();
        _sessions.Setup(x => x.EndAllOpen()).ReturnsAsync(3);
        _sessions.Setup(x => x.EndByCustomer(It.IsAny<string>())).ReturnsAsync(new List<OidcSession>());
        _config = new OidcResourceServerConfig { Enabled = true, Authority = "https://idp.example/realms/shop/" };
    }

    private OidcLinkService Service() =>
        new(_settings.Object, _config, _links, _sessions.Object, NullLogger<OidcLinkService>.Instance);

    private async Task<ExternalAuthentication> Link(string customerId, string provider = Oidc, string sub = "sub-1")
    {
        var link = new ExternalAuthentication { CustomerId = customerId, ProviderSystemName = provider, ExternalIdentifier = sub };
        await _links.InsertAsync(link);
        return link;
    }

    [TestMethod]
    public async Task FirstUse_AdoptsTheConfiguredProvider()
    {
        Assert.IsTrue(await Service().LinksMatchProvider());
        Assert.AreEqual("https://idp.example/realms/shop", _stored.Authority);
    }

    [TestMethod]
    [DataRow("https://idp.example/realms/shop")]
    [DataRow("https://IDP.example/realms/shop/")]
    public async Task SameProvider_TrailingSlashAndCase_Match(string stored)
    {
        _stored.Authority = stored;
        Assert.IsTrue(await Service().LinksMatchProvider());
    }

    [TestMethod]
    public async Task OtherProvider_DoesNotMatch_NothingAdopted()
    {
        _stored.Authority = "https://old-idp.example";
        await Link("c1");
        Assert.IsFalse(await Service().LinksMatchProvider());
        Assert.AreEqual("https://old-idp.example", _stored.Authority);
    }

    //nobody connected yet: no subject could select another person's account
    [TestMethod]
    public async Task OtherProvider_NoLinks_AdoptsTheConfiguredProvider()
    {
        _stored.Authority = "https://old-idp.example";
        await Link("c1", "ExternalAuth.Google", "g-1");
        Assert.IsTrue(await Service().LinksMatchProvider());
        Assert.AreEqual("https://idp.example/realms/shop", _stored.Authority);
    }

    //links made before the provider was recorded: the provider may have changed with the same upgrade
    [TestMethod]
    public async Task LinksOfAnUnrecordedProvider_DoNotMatch_NothingAdopted()
    {
        await Link("c1");
        Assert.IsFalse(await Service().LinksMatchProvider());
        Assert.IsTrue(string.IsNullOrEmpty(_stored.Authority));

        var state = await Service().GetState();
        Assert.IsTrue(state.ProviderUnknown);
        Assert.IsTrue(state.ProviderChanged);
    }

    //concurrent first requests record the provider once (each save clears the whole cache)
    [TestMethod]
    public async Task FirstUse_ConcurrentRequests_RecordTheProviderOnce()
    {
        _settings.Setup(x => x.SaveSetting(It.IsAny<OidcLinkSettings>(), It.IsAny<string>()))
            .Callback((OidcLinkSettings s, string _) => _stored = s).Returns(() => Task.Delay(50));

        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Service().LinksMatchProvider()));

        Assert.IsTrue(results.All(x => x));
        _settings.Verify(x => x.SaveSetting(It.IsAny<OidcLinkSettings>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task NoConfiguredProvider_NothingRecorded()
    {
        _config.Authority = "";
        await Service().KeepAll();
        _settings.Verify(x => x.SaveSetting(It.IsAny<OidcLinkSettings>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task GetState_CountsOpenIdConnectLinks_ReportsTheChange()
    {
        _stored.Authority = "https://old-idp.example";
        await Link("c1");
        await Link("c2", sub: "sub-2");
        await Link("c1", "ExternalAuth.Google", "g-1");

        var state = await Service().GetState();

        Assert.AreEqual(2, state.LinkCount);
        Assert.IsTrue(state.ProviderChanged);
        Assert.AreEqual("https://old-idp.example", state.LinkedAuthority);
    }

    [TestMethod]
    public async Task GetState_BeforeFirstUse_NotChanged()
    {
        Assert.IsFalse((await Service().GetState()).ProviderChanged);
    }

    [TestMethod]
    public async Task GetState_OtherProviderWithoutLinks_NotChanged()
    {
        _stored.Authority = "https://old-idp.example";
        Assert.IsFalse((await Service().GetState()).ProviderChanged);
    }

    [TestMethod]
    public async Task RemoveAll_OnlyOpenIdConnectLinks_EndsSessions_AdoptsTheProvider()
    {
        _stored.Authority = "https://old-idp.example";
        await Link("c1");
        await Link("c2", sub: "sub-2");
        var google = await Link("c1", "ExternalAuth.Google", "g-1");

        Assert.AreEqual(2, await Service().RemoveAll());

        Assert.AreEqual(google.Id, _links.Table.Single().Id);
        _sessions.Verify(x => x.EndAllOpen(), Times.Once);
        Assert.IsTrue(await Service().LinksMatchProvider());
    }

    [TestMethod]
    public async Task KeepAll_LinksStay_ProviderAdopted()
    {
        _stored.Authority = "https://old-idp.example";
        await Link("c1");

        await Service().KeepAll();

        Assert.AreEqual(1, _links.Table.Count());
        Assert.IsTrue(await Service().LinksMatchProvider());
        _sessions.Verify(x => x.EndAllOpen(), Times.Never);
    }

    [TestMethod]
    public async Task Remove_OpenIdConnectLinkOfTheCustomer_EndsItsSessions()
    {
        var link = await Link("c1");

        Assert.IsTrue(await Service().Remove(new Customer { Id = "c1" }, link.Id));

        Assert.AreEqual(0, _links.Table.Count());
        _sessions.Verify(x => x.EndByCustomer("c1"), Times.Once);
    }

    [TestMethod]
    public async Task Remove_LinkOfAnotherCustomer_Refused()
    {
        var link = await Link("c2");

        Assert.IsFalse(await Service().Remove(new Customer { Id = "c1" }, link.Id));

        Assert.AreEqual(1, _links.Table.Count());
        _sessions.Verify(x => x.EndByCustomer(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Remove_OtherProviderLink_NoProviderSessionsEnded()
    {
        var link = await Link("c1", "ExternalAuth.Google", "g-1");

        Assert.IsTrue(await Service().Remove(new Customer { Id = "c1" }, link.Id));

        _sessions.Verify(x => x.EndByCustomer(It.IsAny<string>()), Times.Never);
    }
}
