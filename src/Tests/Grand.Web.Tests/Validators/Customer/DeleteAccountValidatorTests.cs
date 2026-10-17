using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Customer;
using Grand.Web.Validators.Customer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Grand.Web.Common.Localization;

namespace Grand.Web.Tests.Validators.Customers;

[TestClass]
public class DeleteAccountValidatorTests
{
    private static DeleteAccountValidator Validator(OidcLoginConfig config, bool passwordMatches)
    {
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.VerifyPassword(It.IsAny<string>(), It.IsAny<PasswordFormat>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<HashedPasswordFormat>()))
            .Returns(passwordMatches);
        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(new Customer { Password = "pw" });
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns<string>(k => k);
        return new DeleteAccountValidator(new List<IValidatorConsumer<DeleteAccountModel>>(), encryption.Object,
            new CustomerSettings(), context.Object, translation.Object, config);
    }

    [TestMethod]
    public async Task DeleteAccountValidator_ProviderOnly_NoPasswordNeeded()
    {
        var result = await Validator(new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly }, false)
            .ValidateAsync(new DeleteAccountModel());
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task DeleteAccountValidator_Mixed_PasswordRequired()
    {
        var result = await Validator(new OidcLoginConfig(), true).ValidateAsync(new DeleteAccountModel());
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task DeleteAccountValidator_Mixed_WrongPasswordRejected()
    {
        var result = await Validator(new OidcLoginConfig(), false)
            .ValidateAsync(new DeleteAccountModel { Password = "x" });
        Assert.IsFalse(result.IsValid);
    }
}
