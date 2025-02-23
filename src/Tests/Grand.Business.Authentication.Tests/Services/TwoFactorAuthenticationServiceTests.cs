using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class TwoFactorAuthenticationServiceTests
{
    private Mock<IEnumerable<ISMSVerificationService>> _sMsVerificationService;
    private TwoFactorAuthenticationService _twoFactorAuthenticationService;
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IContextAccessor> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _workContextMock = new Mock<IContextAccessor>();
        _customerServiceMock = new Mock<ICustomerService>();
        _sMsVerificationService = new Mock<IEnumerable<ISMSVerificationService>>();
        _twoFactorAuthenticationService = new TwoFactorAuthenticationService(_workContextMock.Object,
            _customerServiceMock.Object, _sMsVerificationService.Object);
    }

    [TestMethod]
    public async Task GenerateCodeSetupTest_AppVerification()
    {
        //Arrange
        _workContextMock.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "", Name = "test store" });
        _workContextMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => new Customer());
        //Act
        var result = await _twoFactorAuthenticationService.GenerateCodeSetup(Guid.NewGuid().ToString(),
            new Customer { Email = "test@test.com" },
            new Language { LanguageCulture = "en" }, TwoFactorAuthenticationType.AppVerification);
        //Assert
        Assert.IsTrue(result.CustomValues.ContainsKey("QrCodeImageUrl"));
        Assert.IsTrue(result.CustomValues.ContainsKey("ManualEntryQrCode"));
    }

    [TestMethod]
    public async Task GenerateCodeSetupTest_Email()
    {
        //Arrange
        _workContextMock.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "", Name = "test store" });
        _workContextMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => new Customer());
        //Act
        var result = await _twoFactorAuthenticationService.GenerateCodeSetup(Guid.NewGuid().ToString(),
            new Customer { Email = "test@test.com" },
            new Language { LanguageCulture = "en" }, TwoFactorAuthenticationType.EmailVerification);
        //Assert
        Assert.IsTrue(result.CustomValues.ContainsKey("Token"));
    }
}