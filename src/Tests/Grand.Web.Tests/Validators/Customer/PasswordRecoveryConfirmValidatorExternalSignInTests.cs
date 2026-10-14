using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Customer;
using Grand.Web.Validators.Customer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Validators.Customers;

[TestClass]
public class PasswordRecoveryConfirmValidatorExternalSignInTests
{
    private const string Token = "token-1";

    private Customer _customer;
    private Mock<IExternalSignInPolicy> _signInPolicy;
    private PasswordRecoveryConfirmValidator _validator;

    [TestInitialize]
    public void Init()
    {
        _customer = new Customer { Id = "c1", Email = "u@x.com", Active = true };
        _customer.UserFields.Add(new UserField
            { Key = SystemCustomerFieldNames.PasswordRecoveryToken, Value = Token, StoreId = "" });
        _customer.UserFields.Add(new UserField {
            Key = SystemCustomerFieldNames.PasswordRecoveryTokenDateGenerated,
            Value = DateTime.UtcNow.ToString("o"), StoreId = ""
        });

        var customerService = new Mock<ICustomerService>();
        customerService.Setup(x => x.GetCustomerByEmail("u@x.com", It.IsAny<string>())).ReturnsAsync(_customer);
        var groupService = new Mock<IGroupService>();
        groupService.Setup(x => x.IsRegistered(_customer)).ReturnsAsync(true);
        var translation = new Mock<ITranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);
        _signInPolicy = new Mock<IExternalSignInPolicy>();
        _signInPolicy.Setup(x => x.PasswordSignIn(It.IsAny<Customer>(), SignInSurface.Storefront))
            .ReturnsAsync(PasswordSignInDecision.Password);

        _validator = new PasswordRecoveryConfirmValidator(
            new List<IValidatorConsumer<PasswordRecoveryConfirmModel>>(),
            customerService.Object, groupService.Object,
            new Mock<ICustomerManagerService>().Object, new Mock<ICustomerHistoryPasswordService>().Object,
            translation.Object, new CustomerSettings(), new Mock<IContextAccessor>().Object, new CustomerConfig(),
            _signInPolicy.Object);
    }

    private Task<FluentValidation.Results.ValidationResult> Validate() =>
        _validator.ValidateAsync(new PasswordRecoveryConfirmModel {
            Email = "u@x.com", Token = Token, NewPassword = "N3w-password", ConfirmNewPassword = "N3w-password"
        });

    [TestMethod]
    public async Task ValidLink_Valid()
    {
        Assert.IsTrue((await Validate()).IsValid);
    }

    //a link sent before the account had to sign in through the provider sets no password any more
    [TestMethod]
    public async Task ExternalSignInRequired_WrongToken()
    {
        _signInPolicy.Setup(x => x.PasswordSignIn(_customer, SignInSurface.Storefront)).ReturnsAsync(new PasswordSignInDecision(false, new Mock<IExternalAuthenticationProvider>().Object));

        var result = await Validate();

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == "Account.PasswordRecovery.WrongToken"));
    }
}
