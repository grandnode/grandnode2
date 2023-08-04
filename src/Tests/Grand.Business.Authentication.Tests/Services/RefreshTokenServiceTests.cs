using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain;
using Grand.Domain.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services
{
    [TestClass()]
    public class RefreshTokenServiceTests
    {
        private Mock<IUserFieldService> _userFieldServiceMock;
        private RefreshTokenService _service;

        [TestInitialize()]
        public void Init()
        {
            _userFieldServiceMock = new Mock<IUserFieldService>();
            _service = new RefreshTokenService(_userFieldServiceMock.Object, new Infrastructure.Configuration.FrontendAPIConfig() {
                SecretKey = "JWTRefreshTokenHIGHsecuredPasswordVVVp1OH7Xzyr",
                ValidIssuer = "http://localhost:4200",
                ValidAudience = "http://localhost:4200",
                ValidateLifetime = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = false,
                ExpiryInMinutes = 1440,
                RefreshTokenExpiryInMinutes = 1440,
                Enabled = true,
                ValidateIssuer = false
            });
        }

        [TestMethod()]
        public void GenerateRefreshTokenTest()
        {
            //Act
            var result = _service.GenerateRefreshToken();
            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public async Task SaveRefreshTokenToCustomerTest()
        {
            //Arrange
            _userFieldServiceMock.Setup(c => c.SaveField<string>(It.IsAny<BaseEntity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            var token = Guid.NewGuid().ToString();
            //Act
            var result = await _service.SaveRefreshTokenToCustomer(new Domain.Customers.Customer(), token);
            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(token, result.Token);
        }

        [TestMethod()]
        public async Task GetCustomerRefreshTokenTest()
        {
            //Arrange
            _userFieldServiceMock.Setup(c => c.GetFieldsForEntity<RefreshToken>(It.IsAny<BaseEntity>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(new RefreshToken() { Token = "123" }));
            //Act
            var result = await _service.GetCustomerRefreshToken(new Domain.Customers.Customer());
            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("123", result.Token);
        }


    }
}