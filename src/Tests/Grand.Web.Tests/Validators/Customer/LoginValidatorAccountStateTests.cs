using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Mediator;
using Grand.SharedKernel.Captcha;
using Grand.Web.Common.Localization;
using Grand.Web.Models.Customer;
using Grand.Web.Validators.Customer;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Validators.Customers;

/// <summary>
///     The sign-in form does not tell which addresses have an account; the state of an account is told only to
///     someone who knows its password
/// </summary>
[TestClass]
public class LoginValidatorAccountStateTests
{
    private const string WrongCredentials = "Account.Login.WrongCredentials";

    private readonly Customer _customer = new()
        { Email = "a@b.c", Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "pw" };

    private Mock<ICustomerService> _customerService;
    private Mock<IGroupService> _groups;
    private Mock<IMediator> _mediator;

    public LoginValidatorAccountStateTests()
    {
        _customerService = new Mock<ICustomerService>();
        _customerService.Setup(c => c.GetCustomerByEmail("a@b.c", It.IsAny<string>())).ReturnsAsync(_customer);
        _groups = new Mock<IGroupService>();
        _groups.Setup(g => g.IsRegistered(_customer)).ReturnsAsync(true);
        _mediator = new Mock<IMediator>();
    }

    private async Task<string[]> Errors(string email, string password)
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.VerifyPassword(It.IsAny<string>(), It.IsAny<PasswordFormat>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<HashedPasswordFormat>()))
            .Returns((string entered, PasswordFormat _, string stored, string _, HashedPasswordFormat _) =>
                entered == stored);
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);
        var policy = new Mock<IExternalSignInPolicy>();
        policy.Setup(x => x.PasswordSignIn(It.IsAny<Customer>(), SignInSurface.Storefront))
            .ReturnsAsync(PasswordSignInDecision.Password);
        var store = new Mock<IStoreContext>();
        store.Setup(s => s.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "store-1" });
        var ctx = new Mock<IContextAccessor>();
        ctx.Setup(c => c.StoreContext).Returns(store.Object);

        var validator = new LoginValidator(
            new List<IValidatorConsumer<LoginModel>>(), new List<IValidatorConsumer<ICaptchaValidModel>>(),
            _customerService.Object, _groups.Object, encryption.Object, translation.Object, new CustomerSettings(),
            new CaptchaSettings(), new Mock<IHttpContextAccessor>().Object, new Mock<IGoogleReCaptchaValidator>().Object,
            ctx.Object, policy.Object, _mediator.Object);

        var result = await validator.ValidateAsync(new LoginModel { Email = email, Password = password });
        return result.Errors.Select(e => e.ErrorMessage).ToArray();
    }

    [TestMethod]
    public async Task UnknownEmail_WrongCredentials()
    {
        CollectionAssert.AreEqual(new[] { WrongCredentials }, await Errors("nobody@b.c", "pw"));
    }

    [TestMethod]
    public async Task DeletedAccount_WrongCredentials()
    {
        _customer.Deleted = true;

        CollectionAssert.AreEqual(new[] { WrongCredentials }, await Errors("a@b.c", "pw"));
    }

    [TestMethod]
    public async Task NotRegistered_WrongCredentials()
    {
        _groups.Setup(g => g.IsRegistered(_customer)).ReturnsAsync(false);

        CollectionAssert.AreEqual(new[] { WrongCredentials }, await Errors("a@b.c", "pw"));
    }

    [TestMethod]
    public async Task InactiveAccount_WrongPassword_WrongCredentialsAndCounted()
    {
        _customer.Active = false;

        CollectionAssert.AreEqual(new[] { WrongCredentials }, await Errors("a@b.c", "wrong"));
        _mediator.Verify(m => m.Publish(It.IsAny<CustomerLoginFailedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task InactiveAccount_RightPassword_NotActive()
    {
        _customer.Active = false;

        CollectionAssert.AreEqual(new[] { "Account.Login.WrongCredentials.NotActive" },
            await Errors("a@b.c", "pw"));
    }
}