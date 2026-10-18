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
public class LoginControllerProviderOnlyTests
{
    private Mock<IExternalAuthenticationService> _externalAuthenticationService = null!;
    private Mock<IContextAccessor> _contextAccessor = null!;
    private Mock<IGroupService> _groupService = null!;
    private Mock<ICustomerManagerService> _customerManagerService = null!;

    [TestInitialize]
    public void Setup()
    {
        _externalAuthenticationService = new Mock<IExternalAuthenticationService>();
        _groupService = new Mock<IGroupService>();
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer());
        _contextAccessor = new Mock<IContextAccessor> { DefaultValue = DefaultValue.Mock };
        _contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        _customerManagerService = new Mock<ICustomerManagerService>();
    }

    private LoginController CreateController(string area)
    {
        var controller = new LoginController(new CustomerSettings(), new CaptchaSettings(),
            new Mock<IRequestTranslationService>().Object, _customerManagerService.Object,
            new Mock<ICustomerService>().Object, new Mock<IGrandAuthenticationService>().Object,
            new Mock<IMessageProviderService>().Object, _contextAccessor.Object,
            new Mock<IMediator>().Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = "/shop";
        controller.ControllerContext = new ControllerContext {
            HttpContext = httpContext,
            RouteData = new RouteData { Values = { ["area"] = area } }
        };
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns($"/shop/{area.ToLowerInvariant()}");
        controller.Url = url.Object;
        return controller;
    }

    private void WithSignInPath(string? path)
    {
        var provider = new Mock<IExternalSignInPathProvider>();
        provider.Setup(x => x.FriendlyName).Returns("OpenID Connect");
        provider.Setup(x => x.GetSignInPath(It.IsAny<string>())).ReturnsAsync(path);
        _externalAuthenticationService.Setup(x => x.LoadActiveAuthenticationProviders(It.IsAny<Grand.Domain.Customers.Customer>(), It.IsAny<Grand.Domain.Stores.Store>()))
            .Returns([provider.Object]);
    }

    private static OidcLoginConfig ProviderOnly() => new() { SignInMode = OidcSignInMode.ProviderOnly };

    [TestMethod]
    public async Task Index_ProviderOnly_Store_Redirects()
    {
        WithSignInPath("/oidc/login?returnUrl=%2Fshop%2Fstore");
        var controller = CreateController("Store");

        var result = await controller.Index(_externalAuthenticationService.Object, ProviderOnly(), _groupService.Object);

        var redirect = result as RedirectResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("/shop/oidc/login?returnUrl=%2Fshop%2Fstore", redirect.Url);
    }

    [TestMethod]
    public async Task Index_ProviderOnly_Admin_Redirects()
    {
        WithSignInPath("/oidc/login?returnUrl=%2Fshop%2Fadmin");
        var controller = CreateController("Admin");

        var result = await controller.Index(_externalAuthenticationService.Object, ProviderOnly(), _groupService.Object);

        var redirect = result as RedirectResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("/shop/oidc/login?returnUrl=%2Fshop%2Fadmin", redirect.Url);
    }

    [TestMethod]
    [DataRow("1")]
    [DataRow("true")]
    [DataRow("True")]
    public async Task Index_ProviderOnly_Admin_PasswordParam_ShowsForm(string password)
    {
        WithSignInPath("/oidc/login?returnUrl=%2Fshop%2Fadmin");
        var controller = CreateController("Admin");

        var result = await controller.Index(_externalAuthenticationService.Object, ProviderOnly(), _groupService.Object,
            password);

        var model = (LoginModel)((ViewResult)result).Model!;
        Assert.IsTrue(model.PasswordFormShown);
        Assert.IsFalse(model.ExternalSignInUnavailable);
        Assert.AreEqual(1, model.ExternalSignIn.Count);
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("")]
    [DataRow(null)]
    public async Task Index_ProviderOnly_Admin_PasswordParamNotSet_Redirects(string? password)
    {
        WithSignInPath("/oidc/login?returnUrl=%2Fshop%2Fadmin");
        var controller = CreateController("Admin");

        var result = await controller.Index(_externalAuthenticationService.Object, ProviderOnly(), _groupService.Object,
            password);

        Assert.IsInstanceOfType<RedirectResult>(result);
    }

    [TestMethod]
    public async Task Index_NoSignInPath_Store_NoticeWithoutForm()
    {
        WithSignInPath(null);
        var controller = CreateController("Store");

        var result = await controller.Index(_externalAuthenticationService.Object, ProviderOnly(), _groupService.Object);

        var model = (LoginModel)((ViewResult)result).Model!;
        Assert.IsTrue(model.ExternalSignInUnavailable);
        Assert.IsFalse(model.PasswordFormShown);
    }

    [TestMethod]
    public async Task Index_NoSignInPath_Admin_NoticeWithForm()
    {
        WithSignInPath(null);
        var controller = CreateController("Admin");

        var result = await controller.Index(_externalAuthenticationService.Object, ProviderOnly(), _groupService.Object);

        var model = (LoginModel)((ViewResult)result).Model!;
        Assert.IsTrue(model.ExternalSignInUnavailable);
        Assert.IsTrue(model.PasswordFormShown);
    }

    [TestMethod]
    public async Task Index_Mixed_ShowsFormNoRedirect()
    {
        WithSignInPath("/oidc/login?returnUrl=%2Fshop%2Fadmin");
        var controller = CreateController("Admin");

        var result = await controller.Index(_externalAuthenticationService.Object, new OidcLoginConfig(), _groupService.Object);

        var model = (LoginModel)((ViewResult)result).Model!;
        Assert.IsTrue(model.PasswordFormShown);
        Assert.IsFalse(model.ExternalSignInUnavailable);
    }

    [TestMethod]
    public async Task IndexPost_ProviderOnly_Admin_FailedAttempt_KeepsForm()
    {
        WithSignInPath("/oidc/login?returnUrl=%2Fshop%2Fadmin");
        _customerManagerService.Setup(x => x.LoginCustomer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<SignInSurface>()))
            .ReturnsAsync(CustomerLoginResults.WrongPassword);
        var controller = CreateController("Admin");

        var result = await controller.Index(new LoginModel { Email = "a@b.c", Password = "x" },
            _externalAuthenticationService.Object, ProviderOnly());

        var model = (LoginModel)((ViewResult)result).Model!;
        Assert.IsTrue(model.PasswordFormShown);
    }

    [TestMethod]
    public async Task IndexPost_ProviderOnly_Vendor_NoPasswordForm()
    {
        WithSignInPath(null);
        var controller = CreateController("Vendor");

        var result = await controller.Index(new LoginModel { Email = "a@b.c", Password = "x" },
            _externalAuthenticationService.Object, ProviderOnly());

        var model = (LoginModel)((ViewResult)result).Model!;
        Assert.IsFalse(model.PasswordFormShown);
        Assert.IsTrue(model.ExternalSignInUnavailable);
    }

    [TestMethod]
    [DataRow("Store")]
    [DataRow("Admin")]
    public async Task Index_ProviderOnly_SignedInWithoutAccess_NoRedirect(string area)
    {
        WithSignInPath("/oidc/login?returnUrl=%2Fshop%2Fpanel");
        _groupService.Setup(x => x.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);
        var controller = CreateController(area);

        var result = await controller.Index(_externalAuthenticationService.Object, ProviderOnly(), _groupService.Object);

        var model = (LoginModel)((ViewResult)result).Model!;
        Assert.IsTrue(model.SignedInWithoutAccess);
        Assert.AreEqual(area == "Admin", model.PasswordFormShown);
    }

    [TestMethod]
    public async Task Index_Mixed_SignedIn_ShowsForm()
    {
        WithSignInPath("/oidc/login?returnUrl=%2Fshop%2Fadmin");
        _groupService.Setup(x => x.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);
        var controller = CreateController("Admin");

        var result = await controller.Index(_externalAuthenticationService.Object, new OidcLoginConfig(), _groupService.Object);

        var model = (LoginModel)((ViewResult)result).Model!;
        Assert.IsFalse(model.SignedInWithoutAccess);
        Assert.IsTrue(model.PasswordFormShown);
    }
}
