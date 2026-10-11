using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Mediator;
using Grand.SharedKernel.Captcha;
using Grand.Web.Common.Security.ExternalRegistration;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Grand.Web.Validators.Customer;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Validators.Customers;

[TestClass]
public class RegisterValidatorExternalTests
{
    private static readonly ExternalRegistrationIdentity Pending =
        new("ExternalAuth.OpenIdConnect", "sub-1", "john@example.com", true, "John", "Doe");

    private Mock<IExternalRegistrationContext> _external;
    private Mock<ICustomerService> _customerService;

    [TestInitialize]
    public void Init()
    {
        _external = new Mock<IExternalRegistrationContext>();
        _customerService = new Mock<ICustomerService>();
    }

    private RegisterValidator Validator()
    {
        var translation = new Mock<ITranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);
        var groupService = new Mock<IGroupService>();
        groupService.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(false);
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetParseCustomAttributes>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomAttribute>());
        var attrParser = new Mock<ICustomerAttributeParser>();
        attrParser.Setup(p => p.GetAttributeWarnings(It.IsAny<IList<CustomAttribute>>(), It.IsAny<string>()))
            .ReturnsAsync(new List<string>());
        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(new Customer());
        var store = new Mock<IStoreContext>();
        store.Setup(s => s.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "s1" });
        var ctx = new Mock<IContextAccessor>();
        ctx.Setup(c => c.WorkContext).Returns(work.Object);
        ctx.Setup(c => c.StoreContext).Returns(store.Object);

        return new RegisterValidator(
            new List<IValidatorConsumer<RegisterModel>>(),
            new List<IValidatorConsumer<ICaptchaValidModel>>(),
            translation.Object, new Mock<ICountryService>().Object, new CustomerSettings(),
            new CaptchaSettings(), new Mock<IHttpContextAccessor>().Object,
            new Mock<IGoogleReCaptchaValidator>().Object, mediator.Object, attrParser.Object,
            _customerService.Object, groupService.Object, ctx.Object, new CustomerConfig(), _external.Object);
    }

    [TestMethod]
    public async Task Pending_NoPassword_Valid()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        var result = await Validator().ValidateAsync(new RegisterModel { Email = Pending.Email });
        Assert.IsTrue(result.IsValid, string.Join(";", result.Errors));
    }

    [TestMethod]
    public async Task Pending_Unverified_NoPassword_Invalid()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending with { EmailVerified = false });
        var result = await Validator().ValidateAsync(new RegisterModel { Email = Pending.Email });
        Assert.IsTrue(result.Errors.Any(e => e.PropertyName == nameof(RegisterModel.Password)));
    }

    [TestMethod]
    public async Task NoPending_NoPassword_Invalid_EvenWithFormFlag()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync((ExternalRegistrationIdentity)null);
        var result = await Validator().ValidateAsync(new RegisterModel { Email = "a@b.c", ExternalRegistration = true });
        Assert.IsTrue(result.Errors.Any(e => e.PropertyName == nameof(RegisterModel.Password)));
    }

    [TestMethod]
    public async Task Pending_UniquenessCheckedOnPendingEmail()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _customerService.Setup(x => x.GetCustomerByEmail(Pending.Email, It.IsAny<string>())).ReturnsAsync(new Customer());
        var result = await Validator().ValidateAsync(new RegisterModel { Email = "other@b.c" });
        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == "Account.Register.Errors.EmailAlreadyExists"));
    }
}
