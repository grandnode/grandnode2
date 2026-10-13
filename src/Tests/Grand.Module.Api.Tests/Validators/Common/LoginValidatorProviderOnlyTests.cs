using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
using Grand.Module.Api.Models.Common;
using Grand.Module.Api.Validators.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text;

#pragma warning disable CS0618 //API users stay supported until removed

namespace Grand.Module.Api.Tests.Validators.Common;

[TestClass]
public class LoginValidatorProviderOnlyTests
{
    private const string Message = "Sign-in with API users is disabled: use access tokens from the identity provider";

    private static LoginValidator CreateValidator(OidcSignInMode mode)
    {
        var customerService = new Mock<ICustomerService>();
        customerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>()))
            .ReturnsAsync(new Customer { Active = true });
        var userApiService = new Mock<IUserApiService>();
        userApiService.Setup(x => x.GetUserByEmail(It.IsAny<string>()))
            .ReturnsAsync(new UserApi { Email = "user@x.com", Password = "hash", IsActive = true });
        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(x => x.IsHashedPassword(It.IsAny<string>())).Returns(true);
        encryption.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<PasswordFormat>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<HashedPasswordFormat>())).Returns(true);
        return new LoginValidator(new List<IValidatorConsumer<LoginModel>>(), new BackendAPIConfig { Enabled = true },
            customerService.Object, userApiService.Object, encryption.Object,
            new OidcLoginConfig { SignInMode = mode });
    }

    private static LoginModel ValidModel() => new() {
        Email = "user@x.com", Password = Convert.ToBase64String(Encoding.UTF8.GetBytes("secret"))
    };

    [TestMethod]
    public async Task ProviderOnly_ValidApiUser_Refused()
    {
        var result = await CreateValidator(OidcSignInMode.ProviderOnly).ValidateAsync(ValidModel());
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == Message));
    }

    //nothing else runs: a password that is not base64 does not throw
    [TestMethod]
    public async Task ProviderOnly_PasswordNotBase64_RefusedWithoutThrowing()
    {
        var result = await CreateValidator(OidcSignInMode.ProviderOnly)
            .ValidateAsync(new LoginModel { Email = "user@x.com", Password = "not base64!" });
        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == Message));
    }

    [TestMethod]
    public async Task Mixed_ValidApiUser_Valid()
    {
        var result = await CreateValidator(OidcSignInMode.Mixed).ValidateAsync(ValidModel());
        Assert.IsTrue(result.IsValid);
    }
}
