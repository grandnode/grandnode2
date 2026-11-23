using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class TwoFactorAuthenticationServiceTests
{
    private Mock<IEnumerable<ISMSVerificationService>> _sMsVerificationService;
    private TwoFactorAuthenticationService _twoFactorAuthenticationService;
    private Mock<ICustomerService> _customerServiceMock;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _sMsVerificationService = new Mock<IEnumerable<ISMSVerificationService>>();
        _twoFactorAuthenticationService = new TwoFactorAuthenticationService(
            _customerServiceMock.Object, _sMsVerificationService.Object);
    }

    [TestMethod]
    public async Task GenerateCodeSetupTest_AppVerification()
    {
        //Act
        var result = await _twoFactorAuthenticationService.GenerateCodeSetup(Guid.NewGuid().ToString(),
            new Customer { Email = "test@test.com" }, new Store { CompanyName = "test store" },
            new Language { LanguageCulture = "en" }, TwoFactorAuthenticationType.AppVerification);
        //Assert
        Assert.IsTrue(result.CustomValues.ContainsKey("QrCodeImageUrl"));
        Assert.IsTrue(result.CustomValues.ContainsKey("ManualEntryQrCode"));
    }

    [TestMethod]
    public async Task GenerateCodeSetupTest_Email()
    {
        //Act
        var result = await _twoFactorAuthenticationService.GenerateCodeSetup(Guid.NewGuid().ToString(),
            new Customer { Email = "test@test.com" }, new Store { CompanyName = "test store" },
            new Language { LanguageCulture = "en" }, TwoFactorAuthenticationType.EmailVerification);
        //Assert
        Assert.IsTrue(result.CustomValues.ContainsKey("Token"));
    }

    private static Customer CustomerWithCode(string code, DateTime validUntil)
    {
        var customer = new Customer();
        customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.TwoFactorValidCode, Value = code, StoreId = "" });
        customer.UserFields.Add(new UserField {
            Key = SystemCustomerFieldNames.TwoFactorCodeValidUntil, //stored the way UpdateUserField stores it
            Value = CommonHelper.To<string>(validUntil), StoreId = ""
        });
        return customer;
    }

    [TestMethod]
    public async Task EmailCode_Correct_SignsIn_AndIsUsedUp()
    {
        var customer = CustomerWithCode("123456", DateTime.UtcNow.AddMinutes(10));

        Assert.IsTrue(await _twoFactorAuthenticationService.AuthenticateTwoFactor(null, " 123456 ", customer,
            TwoFactorAuthenticationType.EmailVerification));
        _customerServiceMock.Verify(x => x.UpdateUserField(customer, SystemCustomerFieldNames.TwoFactorValidCode,
            string.Empty, It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    [DataRow("654321")]
    [DataRow("")]
    [DataRow(null)]
    public async Task EmailCode_Wrong_Refused(string given)
    {
        var customer = CustomerWithCode("123456", DateTime.UtcNow.AddMinutes(10));

        Assert.IsFalse(await _twoFactorAuthenticationService.AuthenticateTwoFactor(null, given, customer,
            TwoFactorAuthenticationType.EmailVerification));
        _customerServiceMock.Verify(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task EmailCode_UsedUp_Refused()
    {
        var customer = CustomerWithCode("", DateTime.UtcNow.AddMinutes(10));

        Assert.IsFalse(await _twoFactorAuthenticationService.AuthenticateTwoFactor(null, "", customer,
            TwoFactorAuthenticationType.EmailVerification));
    }

    [TestMethod]
    public async Task EmailCode_Expired_Refused()
    {
        var customer = CustomerWithCode("123456", DateTime.UtcNow.AddMinutes(-1));

        Assert.IsFalse(await _twoFactorAuthenticationService.AuthenticateTwoFactor(null, "123456", customer,
            TwoFactorAuthenticationType.EmailVerification));
    }
}
