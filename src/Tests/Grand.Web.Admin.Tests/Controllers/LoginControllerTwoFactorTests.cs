using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class LoginControllerTwoFactorTests
{
    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new();
        public bool IsAvailable => true;
        public string Id => "s";
        public IEnumerable<string> Keys => _data.Keys;
        public void Clear() => _data.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _data.Remove(key);
        public void Set(string key, byte[] value) => _data[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _data.TryGetValue(key, out value!);
    }

    private readonly Customer _customer = new() { Id = "c1", Email = "admin@shop.com" };
    private Mock<ICustomerManagerService> _manager = null!;
    private Mock<IGrandAuthenticationService> _authentication = null!;
    private TestSession _session = null!;
    private LoginController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.TwoFactorEnabled, Value = "True", StoreId = "" });
        _manager = new Mock<ICustomerManagerService>();
        _manager.Setup(x => x.LoginCustomer("admin@shop.com", "pwd", It.IsAny<string>()))
            .ReturnsAsync(CustomerLoginResults.RequiresTwoFactor);
        var customers = new Mock<ICustomerService>();
        customers.Setup(x => x.GetCustomerByEmail("admin@shop.com", It.IsAny<string>())).ReturnsAsync(_customer);
        _authentication = new Mock<IGrandAuthenticationService>();
        var settings = new CustomerSettings {
            TwoFactorAuthenticationEnabled = true,
            TwoFactorAuthenticationType = TwoFactorAuthenticationType.AppVerification
        };
        var translations = new Mock<ITranslationService>();
        translations.Setup(x => x.GetResource(It.IsAny<string>())).Returns<string>(key => key);
        _controller = new LoginController(settings, new CaptchaSettings(), translations.Object,
            _manager.Object, customers.Object, _authentication.Object, new Mock<IMessageProviderService>().Object,
            new Mock<IContextAccessor>().Object, new Mock<IMediator>().Object);
        _session = new TestSession();
        _controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { Session = _session },
            RouteData = new RouteData { Values = { ["area"] = "Admin" } }
        };
    }

    private Task<IActionResult> PasswordStep() =>
        _controller.Index(new LoginModel { Email = "admin@shop.com", Password = "pwd" },
            new Mock<IExternalAuthenticationService>().Object);

    [TestMethod]
    public async Task Password_RequiresTwoFactor_GoesToThePanelsOwnSecondStep()
    {
        var result = (RedirectToActionResult)await PasswordStep();

        Assert.AreEqual("TwoFactorAuthorization", result.ActionName);
        Assert.AreEqual("Login", result.ControllerName);
        Assert.AreEqual("Admin", result.RouteValues!["area"]);
        Assert.IsFalse(_session.Keys.Contains("RequiresTwoFactor"), "the storefront's key signs in on the storefront");
    }

    [TestMethod]
    public async Task SecondStep_Get_ShowsTheForm()
    {
        await PasswordStep();

        var result = await _controller.TwoFactorAuthorization(new Mock<ITwoFactorAuthenticationService>().Object);

        Assert.IsInstanceOfType<ViewResult>(result);
    }

    [TestMethod]
    public async Task SecondStep_ValidCode_SignsInToThePanel()
    {
        await PasswordStep();
        var twoFactor = new Mock<ITwoFactorAuthenticationService>();
        twoFactor.Setup(x => x.AuthenticateTwoFactor(It.IsAny<string>(), "123456", _customer, It.IsAny<TwoFactorAuthenticationType>()))
            .ReturnsAsync(true);

        var result = (RedirectToRouteResult)await _controller.TwoFactorAuthorization("123456", twoFactor.Object);

        Assert.AreEqual("AdminIndex", result.RouteName);
        _authentication.Verify(x => x.SignIn(_customer, false), Times.Once);
        Assert.IsFalse(_session.Keys.Any());
    }

    [TestMethod]
    public async Task SecondStep_WrongCode_NotSignedIn()
    {
        await PasswordStep();

        var result = await _controller.TwoFactorAuthorization("000000", new Mock<ITwoFactorAuthenticationService>().Object);

        Assert.IsInstanceOfType<ViewResult>(result);
        _authentication.Verify(x => x.SignIn(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task SecondStep_WithoutPasswordStep_BackToThePanelLogin()
    {
        var result = (RedirectToRouteResult)await _controller.TwoFactorAuthorization("123456",
            new Mock<ITwoFactorAuthenticationService>().Object);

        Assert.AreEqual("AdminLogin", result.RouteName);
        _authentication.Verify(x => x.SignIn(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
    }
}
