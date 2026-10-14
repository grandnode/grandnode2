using Grand.Business.Core.Interfaces.Authentication;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.ExternalRegistration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Security.ExternalRegistration;

[TestClass]
public class ProviderSignOutTests
{
    private const string ProviderUrl = "https://idp.example/logout?x=1";

    private Mock<IGrandAuthenticationService> _auth;
    private Mock<IExternalSignOutProvider> _provider;
    private Mock<IContextAccessor> _contextAccessor;
    private OidcLoginConfig _config;
    private List<IExternalSignOutProvider> _providers;

    [TestInitialize]
    public void Init()
    {
        _auth = new Mock<IGrandAuthenticationService>();
        _provider = new Mock<IExternalSignOutProvider>();
        _provider.Setup(x => x.GetSignOutUrl(It.IsAny<string>())).ReturnsAsync(ProviderUrl);
        _providers = [_provider.Object];
        _config = new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly };
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Store { Url = "https://shop.example" });
        _contextAccessor = new Mock<IContextAccessor>();
        _contextAccessor.Setup(x => x.StoreContext).Returns(storeContext.Object);
        _auth.Setup(x => x.HasOidcSession()).ReturnsAsync(true);
    }

    private ProviderSignOut Service() =>
        new(_auth.Object, _providers, _config, _contextAccessor.Object, NullLogger<ProviderSignOut>.Instance);

    [TestMethod]
    public async Task ProviderOnly_SessionCookie_ReturnsProviderUrl()
    {
        var url = await Service().SignOut();

        Assert.AreEqual(ProviderUrl, url);
        _provider.Verify(x => x.GetSignOutUrl("https://shop.example/"), Times.Once);
        _auth.Verify(x => x.SignOut(), Times.Once);
    }

    [TestMethod]
    public async Task StoreUrlWithTrailingSlash_TargetEndsWithOneSlash()
    {
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Store { Url = "https://shop.example/" });
        _contextAccessor.Setup(x => x.StoreContext).Returns(storeContext.Object);

        await Service().SignOut();

        _provider.Verify(x => x.GetSignOutUrl("https://shop.example/"), Times.Once);
    }

    [TestMethod]
    public async Task UrlIsFetchedBeforeLocalSignOut()
    {
        var order = new List<string>();
        _auth.Setup(x => x.HasOidcSession()).ReturnsAsync(true).Callback(() => order.Add("check"));
        _provider.Setup(x => x.GetSignOutUrl(It.IsAny<string>())).ReturnsAsync(ProviderUrl).Callback(() => order.Add("url"));
        _auth.Setup(x => x.SignOut()).Returns(Task.CompletedTask).Callback(() => order.Add("signout"));

        await Service().SignOut();

        CollectionAssert.AreEqual(new[] { "check", "url", "signout" }, order);
    }

    [TestMethod]
    public async Task Mixed_ReturnsNull_StillSignsOut()
    {
        _config.SignInMode = OidcSignInMode.Mixed;

        Assert.IsNull(await Service().SignOut());

        _provider.Verify(x => x.GetSignOutUrl(It.IsAny<string>()), Times.Never);
        _auth.Verify(x => x.SignOut(), Times.Once);
    }

    [TestMethod]
    public async Task PlainCookie_ReturnsNull()
    {
        _auth.Setup(x => x.HasOidcSession()).ReturnsAsync(false);

        Assert.IsNull(await Service().SignOut());

        _provider.Verify(x => x.GetSignOutUrl(It.IsAny<string>()), Times.Never);
        _auth.Verify(x => x.SignOut(), Times.Once);
    }

    [TestMethod]
    public async Task NoProvider_ReturnsNull()
    {
        _providers.Clear();

        Assert.IsNull(await Service().SignOut());

        _auth.Verify(x => x.SignOut(), Times.Once);
    }

    [TestMethod]
    public async Task ProviderThrows_ReturnsNull_StillSignsOut()
    {
        _provider.Setup(x => x.GetSignOutUrl(It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("discovery down"));

        Assert.IsNull(await Service().SignOut());

        _auth.Verify(x => x.SignOut(), Times.Once);
    }

    [TestMethod]
    public async Task FirstProviderWithUrlWins()
    {
        var empty = new Mock<IExternalSignOutProvider>();
        empty.Setup(x => x.GetSignOutUrl(It.IsAny<string>())).ReturnsAsync((string)null);
        _providers.Insert(0, empty.Object);

        Assert.AreEqual(ProviderUrl, await Service().SignOut());

        _auth.Verify(x => x.SignOut(), Times.Once);
    }
}
