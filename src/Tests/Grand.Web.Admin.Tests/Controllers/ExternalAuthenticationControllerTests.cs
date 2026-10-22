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

    private static async Task<IActionResult> Methods(bool resourceServerEnabled, string clientSecret)
    {
        var linkService = new Mock<IOidcLinkService>();
        linkService.Setup(x => x.GetState()).ReturnsAsync(new OidcLinkState(Authority, Authority, 3));
        var controller = new ExternalAuthenticationController(new Mock<IExternalAuthenticationService>().Object,
            new ExternalAuthenticationSettings(), new Mock<ISettingService>().Object, new Mock<IServiceProvider>().Object);

        return await controller.Methods(
            new OidcResourceServerConfig { Enabled = resourceServerEnabled, Authority = Authority },
            new OidcLoginConfig { ClientId = "shop", ClientSecret = clientSecret },
            linkService.Object);
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
    public async Task Methods_ResourceServerDisabled_ReturnsViewWithoutModel()
    {
        Assert.IsNull(((ViewResult)await Methods(false, "s3cr3t")).Model);
    }
}
