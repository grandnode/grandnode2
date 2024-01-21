﻿using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Customers.Services;
using Grand.Domain.Customers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services
{
    [TestClass()]
    public class CustomerManagerServiceTests
    {

        private Mock<ICustomerService> _customerServiceMock;
        private Mock<IGroupService> _groupServiceMock;
        private Mock<IEncryptionService> _encryptionServiceMock;
        private Mock<ITranslationService> _translationServiceMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<IUserFieldService> _userFieldServiceMock;
        private Mock<ICustomerHistoryPasswordService> _customerHistoryPasswordServiceMock;
        private CustomerSettings _customerSettings;
        private CustomerManagerService _customerManagerService;
        [TestInitialize()]
        public void Init()
        {
            _customerServiceMock = new Mock<ICustomerService>();
            _groupServiceMock = new Mock<IGroupService>();
            _encryptionServiceMock = new Mock<IEncryptionService>();
            _translationServiceMock = new Mock<ITranslationService>();
            _mediatorMock = new Mock<IMediator>();
            _userFieldServiceMock = new Mock<IUserFieldService>();
            _customerHistoryPasswordServiceMock = new Mock<ICustomerHistoryPasswordService>();
            _customerSettings = new CustomerSettings {
                AllowUsersToChangeUsernames = true
            };

            _customerManagerService = new CustomerManagerService(_customerServiceMock.Object, _groupServiceMock.Object,
                _encryptionServiceMock.Object, _mediatorMock.Object, _userFieldServiceMock.Object,
                _customerHistoryPasswordServiceMock.Object, _customerSettings);
        }
        
        [TestMethod()]
        public async Task LoginCustomerTest_WrongPassword()
        {
            //Arrange
            var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "12345" };
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(customer));
            _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(true));
            //Act
            var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456");
            //Assert
            Assert.AreEqual(CustomerLoginResults.WrongPassword, result);
        }
        [TestMethod()]
        public async Task LoginCustomerTest_Successful()
        {
            //Arrange
            var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(customer));
            _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(true));
            //Act
            var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456");
            //Assert
            Assert.AreEqual(CustomerLoginResults.Successful, result);
        }
        
        [TestMethod()]
        public async Task ChangePasswordTest_Success()
        {
            //Arrange
            var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(customer));

            var changepassword = new Core.Utilities.Customers.ChangePasswordRequest("admin@admin.com",
                PasswordFormat.Clear,
                "zxcvbn", "123456");
            //Act
            await _customerManagerService.ChangePassword(changepassword);
            //Assert
            var passwordMatch = _customerManagerService.PasswordMatch(PasswordFormat.Clear, "zxcvb", "zxcvb", string.Empty);
            Assert.IsTrue(passwordMatch);
        }
        
    }
}