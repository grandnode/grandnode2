using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Common.Security.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Security.Authentication;

[TestClass]
public class ExternalSignInServiceTests
{
    private readonly Customer _customer = new() { Id = "customer" };
    private Mock<IGrandAuthenticationService> _authenticationServiceMock;
    private Mock<IExternalAuthenticationService> _externalAuthenticationServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private ExternalSignInService _service;

    [TestInitialize]
    public void Init()
    {
        _externalAuthenticationServiceMock = new Mock<IExternalAuthenticationService>();
        _authenticationServiceMock = new Mock<IGrandAuthenticationService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>(), "lang1", It.IsAny<string>(),
            It.IsAny<bool>())).Returns<string, string, string, bool>((key, _, _, _) => $"{key}@lang1");
        _service = new ExternalSignInService(_externalAuthenticationServiceMock.Object,
            _authenticationServiceMock.Object, _translationServiceMock.Object);
    }

    private Task<IActionResult> Authenticate(ExternalAuthResult result, string returnUrl = null)
    {
        _externalAuthenticationServiceMock
            .Setup(s => s.Authenticate(It.IsAny<ExternalAuthParam>(), It.IsAny<Customer>(), It.IsAny<Store>(),
                It.IsAny<Language>()))
            .ReturnsAsync(result);
        return _service.Authenticate(new ExternalAuthParam(), new Customer(), new Store(), new Language { Id = "lang1" },
            returnUrl);
    }

    [TestMethod]
    public async Task LoggedIn_SignsInAndRedirectsToReturnUrl()
    {
        var result = await Authenticate(ExternalAuthResult.LoggedIn(_customer), "/cart");

        _authenticationServiceMock.Verify(a => a.SignIn(_customer, false), Times.Once);
        Assert.AreEqual("/cart", ((RedirectResult)result).Url);
    }

    [TestMethod]
    public async Task LoggedIn_WithoutReturnUrl_RedirectsToHomePage()
    {
        var result = await Authenticate(ExternalAuthResult.LoggedIn(_customer));

        Assert.AreEqual("HomePage", ((RedirectToRouteResult)result).RouteName);
    }

    [TestMethod]
    public async Task Registered_SignsInAndShowsStandardRegisterResult()
    {
        var result = await Authenticate(ExternalAuthResult.Registered(_customer), "/cart");

        _authenticationServiceMock.Verify(a => a.SignIn(_customer, false), Times.Once);
        var redirect = (RedirectToRouteResult)result;
        Assert.AreEqual("RegisterResult", redirect.RouteName);
        Assert.AreEqual((int)UserRegistrationType.Standard, redirect.RouteValues!["resultId"]);
    }

    [TestMethod]
    public async Task Associated_RedirectsToReturnUrlWithoutSignIn()
    {
        var result = await Authenticate(ExternalAuthResult.Associated(_customer), "/account");

        _authenticationServiceMock.Verify(a => a.SignIn(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
        Assert.AreEqual("/account", ((RedirectResult)result).Url);
    }

    [TestMethod]
    public async Task AwaitingApproval_ShowsAdminApprovalRegisterResultWithoutSignIn()
    {
        var result = await Authenticate(ExternalAuthResult.AwaitingApproval(_customer));

        _authenticationServiceMock.Verify(a => a.SignIn(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
        var redirect = (RedirectToRouteResult)result;
        Assert.AreEqual("RegisterResult", redirect.RouteName);
        Assert.AreEqual((int)UserRegistrationType.AdminApproval, redirect.RouteValues!["resultId"]);
    }

    [TestMethod]
    [DataRow("https://evil.example")]
    [DataRow("//evil.example")]
    [DataRow("/\evil.example")]
    [DataRow("javascript:alert(1)")]
    public async Task LoggedIn_WithExternalReturnUrl_RedirectsToHomePage(string returnUrl)
    {
        var result = await Authenticate(ExternalAuthResult.LoggedIn(_customer), returnUrl);

        _authenticationServiceMock.Verify(a => a.SignIn(_customer, false), Times.Once);
        Assert.AreEqual("HomePage", ((RedirectToRouteResult)result).RouteName);
    }

    [TestMethod]
    public async Task Associated_WithExternalReturnUrl_RedirectsToHomePage()
    {
        var result = await Authenticate(ExternalAuthResult.Associated(_customer), "https://evil.example");

        Assert.AreEqual("HomePage", ((RedirectToRouteResult)result).RouteName);
    }

    [TestMethod]
    public async Task AlreadyAssigned_ShowsTranslatedErrorWithoutSignIn()
    {
        var result = (RedirectToActionResult)await Authenticate(ExternalAuthResult.AlreadyAssigned());

        Assert.AreEqual("ExternalAuthenticationError", result.ActionName);
        Assert.AreEqual("Common", result.ControllerName);
        CollectionAssert.AreEqual(new[] { "Account.AssociatedExternalAuth.AlreadyAssigned@lang1" },
            ((IEnumerable<string>)result.RouteValues!["Errors"]).ToArray());
        _authenticationServiceMock.Verify(a => a.SignIn(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task RegistrationDisabled_ShowsTranslatedErrorWithoutSignIn()
    {
        var result = (RedirectToActionResult)await Authenticate(ExternalAuthResult.RegistrationDisabled());

        Assert.AreEqual("ExternalAuthenticationError", result.ActionName);
        CollectionAssert.AreEqual(new[] { "Account.AssociatedExternalAuth.RegistrationDisabled@lang1" },
            ((IEnumerable<string>)result.RouteValues!["Errors"]).ToArray());
        _authenticationServiceMock.Verify(a => a.SignIn(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Error_ShowsErrorsOfTheResultWithoutSignIn()
    {
        var error = ExternalAuthResult.Error("failed");

        var result = (RedirectToActionResult)await Authenticate(error);

        Assert.AreEqual("ExternalAuthenticationError", result.ActionName);
        Assert.AreSame(error.Errors, result.RouteValues!["Errors"]);
        _authenticationServiceMock.Verify(a => a.SignIn(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
    }
}
