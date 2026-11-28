using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
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
    private Mock<IResourceLock> _resourceLockMock;

    public TwoFactorAuthenticationServiceTests()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _sMsVerificationService = new Mock<IEnumerable<ISMSVerificationService>>();
        _resourceLockMock = new Mock<IResourceLock>();
        _resourceLockMock.Setup(x => x.Acquire(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        _twoFactorAuthenticationService = new TwoFactorAuthenticationService(
            _customerServiceMock.Object, _sMsVerificationService.Object, _resourceLockMock.Object);
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

    //the service reads the code again from the database, so the stored customer is what the mock returns
    private Customer CustomerWithCode(string code, DateTime validUntil)
    {
        var stored = NewCustomerWithCode(code, validUntil);
        _customerServiceMock.Setup(x => x.GetCustomerById(stored.Id)).ReturnsAsync(stored);
        return stored;
    }

    private static Customer NewCustomerWithCode(string code, DateTime validUntil)
    {
        var customer = new Customer { Id = "c1" };
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

    [TestMethod]
    public async Task EmailCode_UsedUpByAnotherRequest_Refused()
    {
        //the request still holds the customer it loaded before another request used the code
        var loaded = NewCustomerWithCode("123456", DateTime.UtcNow.AddMinutes(10));
        _customerServiceMock.Setup(x => x.GetCustomerById(loaded.Id))
            .ReturnsAsync(NewCustomerWithCode("", DateTime.UtcNow.AddMinutes(10)));

        Assert.IsFalse(await _twoFactorAuthenticationService.AuthenticateTwoFactor(null, "123456", loaded,
            TwoFactorAuthenticationType.EmailVerification));
        _customerServiceMock.Verify(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task EmailCode_CheckedAndUsedUpUnderTheCustomersLock()
    {
        var customer = CustomerWithCode("123456", DateTime.UtcNow.AddMinutes(10));

        await _twoFactorAuthenticationService.AuthenticateTwoFactor(null, "123456", customer,
            TwoFactorAuthenticationType.EmailVerification);

        _resourceLockMock.Verify(x => x.Acquire(It.Is<IEnumerable<string>>(k => k.Single() == "TwoFactorCode:c1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
