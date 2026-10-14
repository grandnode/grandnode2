using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Mediator;
using Grand.SharedKernel.Captcha;
using Grand.Web.Models.Customer;
using Grand.Web.Validators.Customer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Validators.Customers;

[TestClass]
public class LoginValidatorExternalSignInTests
{
    private readonly Customer _customer = new() { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "pw" };
    private Mock<ICustomerService> _customerService;
    private Mock<IEncryptionService> _encryption;
    private Mock<IGroupService> _groups;
    private Mock<IExternalSignInPolicy> _policy;
    private Mock<IExternalAuthenticationProvider> _provider;
    private Mock<ITranslationService> _translation;

    [TestInitialize]
    public void Init()
    {
        _policy = new Mock<IExternalSignInPolicy>();
        _policy.Setup(x => x.PasswordSignIn(It.IsAny<Customer>(), SignInSurface.Storefront))
            .ReturnsAsync(PasswordSignInDecision.Password);
        _provider = new Mock<IExternalAuthenticationProvider>();
        _provider.Setup(x => x.FriendlyName).Returns("OpenID Connect");
        _customerService = new Mock<ICustomerService>();
        _customerService.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(_customer);
        _groups = new Mock<IGroupService>();
        _groups.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);
        _encryption = new Mock<IEncryptionService>();
        _encryption.Setup(e => e.VerifyPassword(It.IsAny<string>(), It.IsAny<PasswordFormat>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<HashedPasswordFormat>()))
            .Returns((string entered, PasswordFormat _, string stored, string _, HashedPasswordFormat _) =>
                entered == stored);
        _translation = new Mock<ITranslationService>();
        _translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);
        _translation.Setup(t => t.GetResource("Account.Login.WrongCredentials.ExternalSignInRequired"))
            .Returns("Sign in with {0}");
    }

    private LoginValidator Validator()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<IMediator>().Object);
        var http = new Mock<IHttpContextAccessor>();
        http.Setup(a => a.HttpContext).Returns(new DefaultHttpContext { RequestServices = services.BuildServiceProvider() });
        var store = new Mock<IStoreContext>();
        store.Setup(s => s.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "store-1" });
        var ctx = new Mock<IContextAccessor>();
        ctx.Setup(c => c.StoreContext).Returns(store.Object);

        return new LoginValidator(
            new List<IValidatorConsumer<LoginModel>>(),
            new List<IValidatorConsumer<ICaptchaValidModel>>(),
            _customerService.Object, _groups.Object, _encryption.Object, _translation.Object, new CustomerSettings(),
            new CaptchaSettings(), http.Object, new Mock<IGoogleReCaptchaValidator>().Object,
            ctx.Object, new CustomerConfig(), _policy.Object);
    }

    [TestMethod]
    public async Task CorrectPassword_ExternalSignInRequired_Failure()
    {
        _policy.Setup(x => x.PasswordSignIn(_customer, SignInSurface.Storefront)).ReturnsAsync(new PasswordSignInDecision(false, _provider.Object));
        var result = await Validator().ValidateAsync(new LoginModel { Email = "a@b.c", Password = "pw" });
        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == "Sign in with OpenID Connect"));
    }

    [TestMethod]
    public async Task CorrectPassword_NoPolicy_Valid()
    {
        var result = await Validator().ValidateAsync(new LoginModel { Email = "a@b.c", Password = "pw" });
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task WrongPassword_PolicyNotAsked()
    {
        var result = await Validator().ValidateAsync(new LoginModel { Email = "a@b.c", Password = "wrong" });
        Assert.IsFalse(result.IsValid);
        _policy.Verify(x => x.PasswordSignIn(It.IsAny<Customer>(), SignInSurface.Storefront), Times.Never);
    }
}
