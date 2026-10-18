using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Grand.Web.Common.Localization;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class LoginControllerExternalSignInTests
{
    private Mock<IExternalAuthenticationService> _externalAuthenticationService = null!;
    private Mock<IGroupService> _groupService = null!;
    private LoginController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _externalAuthenticationService = new Mock<IExternalAuthenticationService>();
        _groupService = new Mock<IGroupService>();
        _controller = new LoginController(new CustomerSettings(), new CaptchaSettings(),
            new Mock<IRequestTranslationService>().Object, new Mock<ICustomerManagerService>().Object,
            new Mock<ICustomerService>().Object, new Mock<IGrandAuthenticationService>().Object,
            new Mock<IMessageProviderService>().Object, new Mock<IContextAccessor> { DefaultValue = DefaultValue.Mock }.Object,
            new Mock<IMediator>().Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = "/shop";
        _controller.ControllerContext = new ControllerContext {
            HttpContext = httpContext,
            RouteData = new RouteData { Values = { ["area"] = "Admin" } }
        };
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.RouteUrl(It.Is<UrlRouteContext>(c => c.RouteName == "AdminIndex"))).Returns("/shop/admin");
        _controller.Url = url.Object;
    }

    [TestMethod]
    public async Task Index_ListsPanelProvidersOnly()
    {
        var panel = new Mock<IExternalSignInPathProvider>();
        panel.Setup(x => x.FriendlyName).Returns("OpenID Connect");
        panel.Setup(x => x.GetSignInPath("/shop/admin")).ReturnsAsync("/oidc/login?returnUrl=%2Fshop%2Fadmin");
        var storefrontOnly = new Mock<IExternalAuthenticationProvider>();
        _externalAuthenticationService.Setup(x => x.LoadActiveAuthenticationProviders(It.IsAny<Grand.Domain.Customers.Customer>(), It.IsAny<Grand.Domain.Stores.Store>()))
            .Returns([panel.Object, storefrontOnly.Object]);

        var model = (LoginModel)((ViewResult)await _controller.Index(_externalAuthenticationService.Object, new OidcLoginConfig(), _groupService.Object)).Model!;

        Assert.AreEqual(1, model.ExternalSignIn.Count);
        Assert.AreEqual("OpenID Connect", model.ExternalSignIn[0].Name);
        Assert.AreEqual("/shop/oidc/login?returnUrl=%2Fshop%2Fadmin", model.ExternalSignIn[0].Url);
    }

    [TestMethod]
    public async Task Index_ProviderWithoutPath_Hidden()
    {
        var panel = new Mock<IExternalSignInPathProvider>();
        panel.Setup(x => x.GetSignInPath(It.IsAny<string>())).ReturnsAsync((string)null!);
        _externalAuthenticationService.Setup(x => x.LoadActiveAuthenticationProviders(It.IsAny<Grand.Domain.Customers.Customer>(), It.IsAny<Grand.Domain.Stores.Store>())).Returns([panel.Object]);
        var model = (LoginModel)((ViewResult)await _controller.Index(_externalAuthenticationService.Object, new OidcLoginConfig(), _groupService.Object)).Model!;
        Assert.AreEqual(0, model.ExternalSignIn.Count);
    }
}
