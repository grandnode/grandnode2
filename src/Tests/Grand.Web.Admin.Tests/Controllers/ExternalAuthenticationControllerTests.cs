using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Models.ExternalAuthentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class ExternalAuthenticationControllerTests
{
    private const string Authority = "https://idp.test";

    private readonly Mock<IOidcLinkService> _linkService = new();

    private async Task<IActionResult> Methods(bool resourceServerEnabled, string clientSecret, string clientId = "shop")
    {
        _linkService.Setup(x => x.GetState()).ReturnsAsync(new OidcLinkState(Authority, Authority, 3));
        var controller = new ExternalAuthenticationController(new Mock<IExternalAuthenticationService>().Object,
            new ExternalAuthenticationSettings(), new Mock<ISettingService>().Object, new Mock<IServiceProvider>().Object);

        return await controller.Methods(
            new OidcResourceServerConfig { Enabled = resourceServerEnabled, Authority = Authority },
            new OidcLoginConfig { ClientId = clientId, ClientSecret = clientSecret },
            _linkService.Object);
    }

    [TestMethod]
    public async Task Methods_Configured_ShowsClientIdAndWhetherASecretIsSet()
    {
        var model = (OidcLinksModel)((ViewResult)await Methods(true, "s3cr3t")).Model!;

        Assert.AreEqual(Authority, model.ConfiguredAuthority);
        Assert.AreEqual("shop", model.ClientId);
        Assert.IsTrue(model.ClientSecretSet);
        //the secret itself never reaches the page
        foreach (var property in typeof(OidcLinksModel).GetProperties().Where(x => x.PropertyType == typeof(string)))
            Assert.IsFalse(((string)property.GetValue(model))?.Contains("s3cr3t") ?? false, property.Name);
    }

    [TestMethod]
    public async Task Methods_SecretEmpty_ReportsNoSecret()
    {
        var model = (OidcLinksModel)((ViewResult)await Methods(true, "")).Model!;

        Assert.IsFalse(model.ClientSecretSet);
    }

    [TestMethod]
    public async Task Methods_Configured_ShowsBothCards()
    {
        var model = (OidcLinksModel)((ViewResult)await Methods(true, "s3cr3t")).Model!;

        Assert.IsTrue(model.SignInConfigured);
        Assert.IsTrue(model.LinksConfigured);
        Assert.AreEqual(3, model.LinkCount);
    }

    //browser sign-in needs only Authority and ClientId: the grid lists it and its configure link needs the card
    [TestMethod]
    public async Task Methods_ResourceServerDisabled_ShowsSignInCardOnly()
    {
        var model = (OidcLinksModel)((ViewResult)await Methods(false, "s3cr3t")).Model!;

        Assert.IsTrue(model.SignInConfigured);
        Assert.IsFalse(model.LinksConfigured);
        Assert.AreEqual(Authority, model.ConfiguredAuthority);
        Assert.AreEqual("shop", model.ClientId);
        _linkService.Verify(x => x.GetState(), Times.Never);
    }

    //the resource server alone: the links card, no sign-in card with an empty client
    [TestMethod]
    public async Task Methods_ClientIdEmpty_ShowsLinksCardOnly()
    {
        var model = (OidcLinksModel)((ViewResult)await Methods(true, "", clientId: "")).Model!;

        Assert.IsFalse(model.SignInConfigured);
        Assert.IsTrue(model.LinksConfigured);
    }

    [TestMethod]
    public async Task Methods_NothingConfigured_ReturnsViewWithoutModel()
    {
        Assert.IsNull(((ViewResult)await Methods(false, "", clientId: "")).Model);
    }
}
