using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Controllers;
using Grand.Web.Models.Customer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Grand.Web.Common.Localization;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class AccountControllerTwoFactorReturnUrlTests
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
        public bool TryGetValue(string key, out byte[] value) => _data.TryGetValue(key, out value);
    }

    private Mock<ICustomerManagerService> _manager;
    private Mock<ICustomerService> _customers;
    private AccountController _controller;
    private readonly Customer _customer = new() { Id = "c1", Email = "a@b.c" };

    [TestInitialize]
    public void Init()
    {
        _manager = new Mock<ICustomerManagerService>();
        _customers = new Mock<ICustomerService>();
        _customers.Setup(x => x.GetCustomerByEmail("a@b.c", It.IsAny<string>())).ReturnsAsync(_customer);
        var context = new Mock<IContextAccessor>();
        context.Setup(x => x.StoreContext.CurrentStore).Returns(new Domain.Stores.Store { Id = "store" });
        context.Setup(x => x.WorkContext.CurrentCustomer).Returns(new Customer());
        var settings = new CustomerSettings { TwoFactorAuthenticationEnabled = true };
        _controller = new AccountController(new Mock<IGrandAuthenticationService>().Object, new Mock<IRequestTranslationService>().Object,
            context.Object, _customers.Object, new Mock<IGroupService>().Object, _manager.Object, new Mock<ICountryService>().Object,
            new Mock<IMediator>().Object, new Mock<ICustomerMessageProviderService>().Object, new CaptchaSettings(), settings,
            new CustomerConfig());
        var http = new DefaultHttpContext { Session = new TestSession() };
        _controller.ControllerContext = new ControllerContext { HttpContext = http };
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns<string>(u => u.StartsWith('/') && !u.StartsWith("//"));
        _controller.Url = url.Object;
    }

    private async Task<IActionResult> PassTwoFactor(string returnUrl)
    {
        _manager.Setup(x => x.LoginCustomer("a@b.c", "p", It.IsAny<string>(), SignInSurface.Storefront)).ReturnsAsync(CustomerLoginResults.RequiresTwoFactor);
        await _controller.Login(new LoginModel { Email = "a@b.c", Password = "p" }, returnUrl);
        var twoFactor = new Mock<ITwoFactorAuthenticationService>();
        twoFactor.Setup(x => x.AuthenticateTwoFactor(It.IsAny<string>(), "123", _customer, It.IsAny<TwoFactorAuthenticationType>()))
            .ReturnsAsync(true);
        return await _controller.TwoFactorAuthorization("123", twoFactor.Object);
    }

    [TestMethod]
    public async Task LocalReturnUrl_SurvivesTwoFactor()
    {
        var result = await PassTwoFactor("/oidc/link/complete");
        Assert.AreEqual("/oidc/link/complete", ((RedirectResult)result).Url);
    }

    [TestMethod]
    public async Task ExternalReturnUrl_NotKept()
    {
        var result = await PassTwoFactor("https://evil.example/");
        Assert.IsInstanceOfType<RedirectToRouteResult>(result);
        Assert.AreEqual("HomePage", ((RedirectToRouteResult)result).RouteName);
    }
}
