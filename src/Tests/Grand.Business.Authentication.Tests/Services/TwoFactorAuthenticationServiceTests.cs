using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Authentication.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grand.Infrastructure;
using Moq;
using Grand.Business.Core.Interfaces.Common.Directory;
using Google.Authenticator;
using Grand.Domain.Customers;

namespace Grand.Business.Authentication.Services.Tests
{
    [TestClass()]
    public class TwoFactorAuthenticationServiceTests
    {
        private Mock<IWorkContext> _workContextMock;
        private Mock<IUserFieldService> _userFieldServiceMock;
        private Mock<IServiceProvider> _serviceProviderMock;
        private TwoFactorAuthenticationService _twoFactorAuthenticationService;
        [TestInitialize()]
        public void Init()
        {
            _workContextMock = new Mock<IWorkContext>();
            _userFieldServiceMock = new Mock<IUserFieldService>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _twoFactorAuthenticationService = new TwoFactorAuthenticationService(_workContextMock.Object,
                _userFieldServiceMock.Object, _serviceProviderMock.Object);
        }

        [TestMethod()]
        public async Task GenerateCodeSetupTest_AppVerification()
        {
            //Arrange
            _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Domain.Stores.Store() { Id = "", Name = "test store" });
            _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
            //Act
            var result = await _twoFactorAuthenticationService.GenerateCodeSetup(Guid.NewGuid().ToString(), 
                new Customer() { Email = "test@test.com" }, 
                new Domain.Localization.Language() { LanguageCulture = "en" }, TwoFactorAuthenticationType.AppVerification);
            //Assert
            Assert.IsTrue(result.CustomValues.ContainsKey("QrCodeImageUrl"));
            Assert.IsTrue(result.CustomValues.ContainsKey("ManualEntryQrCode"));
        }
        [TestMethod()]
        public async Task GenerateCodeSetupTest_Email()
        {
            //Arrange
            _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Domain.Stores.Store() { Id = "", Name = "test store" });
            _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
            //Act
            var result = await _twoFactorAuthenticationService.GenerateCodeSetup(Guid.NewGuid().ToString(),
                new Customer() { Email = "test@test.com" },
                new Domain.Localization.Language() { LanguageCulture = "en" }, TwoFactorAuthenticationType.EmailVerification);
            //Assert
            Assert.IsTrue(result.CustomValues.ContainsKey("Token"));
        }
        

    }
}