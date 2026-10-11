using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Common.Security.ExternalRegistration;
using Grand.Web.Controllers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class AccountControllerExternalRegisterTests
{
    private static readonly ExternalRegistrationIdentity Pending =
        new("ExternalAuth.OpenIdConnect", "sub-1", "john@example.com", true, "John", "Doe");

    private Mock<ICustomerManagerService> _manager;
    private Mock<IMessageProviderService> _messages;
    private Mock<IExternalRegistrationContext> _external;
    private Mock<IExternalAuthenticationService> _externalAuth;
    private CustomerSettings _settings;
    private AccountController _controller;
    private readonly Customer _guest = new() { Id = "g1" };

    [TestInitialize]
    public void Init()
    {
        _manager = new Mock<ICustomerManagerService>();
        _messages = new Mock<IMessageProviderService>();
        _external = new Mock<IExternalRegistrationContext>();
        _externalAuth = new Mock<IExternalAuthenticationService>();
        _settings = new CustomerSettings { UserRegistrationType = UserRegistrationType.Standard };

        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(_guest);
        work.Setup(w => w.WorkingLanguage).Returns(new Language { Id = "l1" });
        var store = new Mock<IStoreContext>();
        store.Setup(s => s.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        context.Setup(c => c.StoreContext).Returns(store.Object);

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetRegister>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterModel());
        var groups = new Mock<IGroupService>();
        groups.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(false);

        _controller = new AccountController(new Mock<IGrandAuthenticationService>().Object,
            new Mock<ITranslationService>().Object, context.Object, new Mock<ICustomerService>().Object,
            groups.Object, _manager.Object, new Mock<ICountryService>().Object, mediator.Object, _messages.Object,
            new CaptchaSettings(), _settings, new CustomerConfig());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("/registerresult/1");
        _controller.Url = url.Object;
    }

    [TestMethod]
    public async Task Get_Pending_PrefillsAndMarksExternal()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        var view = (ViewResult)(await _controller.Register(_external.Object, _externalAuth.Object)).Result;
        var model = (RegisterModel)view.Model;
        Assert.AreEqual("john@example.com", model.Email);
        Assert.AreEqual("John", model.FirstName);
        Assert.IsTrue(model.ExternalRegistration);
    }

    [TestMethod]
    public async Task Post_Pending_UsesPendingEmail_RandomPassword_Links()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        RegistrationRequest request = null;
        _manager.Setup(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>())).Callback<RegistrationRequest>(r => request = r);
        await _controller.Register(new RegisterModel { Email = "evil@b.c", ExternalRegistration = false }, null,
            _external.Object, _externalAuth.Object);
        Assert.AreEqual("john@example.com", request.Email);
        Assert.IsFalse(string.IsNullOrEmpty(request.Password));
        _external.Verify(x => x.Complete(_guest, "s1"), Times.Once);
    }

    [TestMethod]
    [DataRow(UserRegistrationType.Standard, true, true)]
    [DataRow(UserRegistrationType.EmailValidation, true, true)]
    [DataRow(UserRegistrationType.Standard, false, false)]
    [DataRow(UserRegistrationType.AdminApproval, true, false)]
    public async Task Post_Pending_ApprovalFollowsPolicy(UserRegistrationType type, bool verified, bool approved)
    {
        _settings.UserRegistrationType = type;
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending with { EmailVerified = verified });
        RegistrationRequest request = null;
        _manager.Setup(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>())).Callback<RegistrationRequest>(r => request = r);
        await _controller.Register(new RegisterModel(), null, _external.Object, _externalAuth.Object);
        Assert.AreEqual(approved, request.IsApproved);
    }

    [TestMethod]
    public async Task Post_Pending_Unverified_SendsActivation()
    {
        _settings.UserRegistrationType = UserRegistrationType.Standard;
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending with { EmailVerified = false });
        var result = await _controller.Register(new RegisterModel(), null, _external.Object, _externalAuth.Object);
        _messages.Verify(x => x.SendCustomerEmailValidationMessage(_guest, It.IsAny<Grand.Domain.Stores.Store>(), "l1"), Times.Once);
        Assert.AreEqual((int)UserRegistrationType.EmailValidation, ((RedirectToRouteResult)result).RouteValues["resultId"]);
    }

    [TestMethod]
    public async Task Post_NoPending_RegularPath_NoLink()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync((ExternalRegistrationIdentity)null);
        RegistrationRequest request = null;
        _manager.Setup(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>())).Callback<RegistrationRequest>(r => request = r);
        await _controller.Register(new RegisterModel { Email = "a@b.c", Password = "Secret1!" }, null, _external.Object, _externalAuth.Object);
        Assert.AreEqual("Secret1!", request.Password);
        _external.Verify(x => x.Complete(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
    }
}
