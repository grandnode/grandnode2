using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.SharedKernel;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Services.Tests
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
            _customerSettings = new CustomerSettings();
            _customerSettings.AllowUsersToChangeUsernames = true;

            _customerManagerService = new CustomerManagerService(_customerServiceMock.Object, _groupServiceMock.Object,
                _encryptionServiceMock.Object, _translationServiceMock.Object, _mediatorMock.Object, _userFieldServiceMock.Object,
                _customerHistoryPasswordServiceMock.Object, _customerSettings);
        }
        [TestMethod()]
        public async Task LoginCustomerTest_CustomerNotExist()
        {
            //Arrange
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(null));

            //Act
            var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456");
            //Assert
            Assert.AreEqual(CustomerLoginResults.CustomerNotExist, result);
        }
        [TestMethod()]
        public async Task LoginCustomerTest_CustomerDeleted()
        {
            //Arrange
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(new Customer() { Deleted = true }));

            //Act
            var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456");
            //Assert
            Assert.AreEqual(CustomerLoginResults.Deleted, result);
        }
        [TestMethod()]
        public async Task LoginCustomerTest_CustomerNotActive()
        {
            //Arrange
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(new Customer() { Active = false }));
            //Act
            var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456");
            //Assert
            Assert.AreEqual(CustomerLoginResults.NotActive, result);
        }
        [TestMethod()]
        public async Task LoginCustomerTest_NotRegistered()
        {
            //Arrange
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(new Customer() { Active = true }));
            //Act
            var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456");
            //Assert
            Assert.AreEqual(CustomerLoginResults.NotRegistered, result);
        }
        [TestMethod()]
        public async Task LoginCustomerTest_LockedOut()
        {
            //Arrange
            var customer = new Customer() { Active = true, CannotLoginUntilDateUtc = DateTime.UtcNow.AddMinutes(1) };
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(customer));
            _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(true));
            //Act
            var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456");
            //Assert
            Assert.AreEqual(CustomerLoginResults.LockedOut, result);
        }
        [TestMethod()]
        public async Task LoginCustomerTest_WrongPassword()
        {
            //Arrange
            var customer = new Customer() { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "12345" };
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
            var customer = new Customer() { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(customer));
            _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(true));
            //Act
            var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456");
            //Assert
            Assert.AreEqual(CustomerLoginResults.Successful, result);
        }
        [TestMethod()]
        public async Task RegisterCustomerTest_Errors()
        {
            //Arrange
            var request = new Core.Utilities.Customers.RegistrationRequest(new Customer(),
                "admin@admin.com", "admin@admin.com", "123456", PasswordFormat.Clear, ""
                );
            _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(true));
            //Act
            var result = await _customerManagerService.RegisterCustomer(request);
            //Assert
            Assert.AreEqual(result.Success, false);
            Assert.AreEqual(result.Errors.FirstOrDefault(), "Current customer is already registered");
        }

        [TestMethod()]
        public async Task RegisterCustomerTest_Success()
        {
            //Arrange
            var request = new Core.Utilities.Customers.RegistrationRequest(new Customer(),
                "admin@admin.com", "admin@admin.com", "123456", PasswordFormat.Clear, ""
                );
            _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(false));
            _groupServiceMock.Setup(c => c.GetCustomerGroupBySystemName(It.IsAny<string>())).Returns(() => Task.FromResult(new CustomerGroup()));
            //Act
            var result = await _customerManagerService.RegisterCustomer(request);
            //Assert
            Assert.AreEqual(result.Success, true);
        }

        [TestMethod()]
        public async Task ChangePasswordTest_InValidOldPassword_Errors()
        {
            //Arrange
            var customer = new Customer() { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "111111" };
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(customer));

            var changepassword = new Core.Utilities.Customers.ChangePasswordRequest("admin@admin.com",
                true, PasswordFormat.Clear,
                "zxcvbn", "123456");
            //Act
            var result = await _customerManagerService.ChangePassword(changepassword);
            //Assert
            Assert.AreEqual(result.Success, false);
        }
        [TestMethod()]
        public async Task ChangePasswordTest_Success()
        {
            //Arrange
            var customer = new Customer() { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(customer));

            var changepassword = new Core.Utilities.Customers.ChangePasswordRequest("admin@admin.com",
                true, PasswordFormat.Clear,
                "zxcvbn", "123456");
            //Act
            var result = await _customerManagerService.ChangePassword(changepassword);
            //Assert
            Assert.AreEqual(result.Success, true);
        }
        [TestMethod()]
        public async Task SetEmailTest_Success()
        {
            //Arrange
            var customer = new Customer() { Email = "email@email.com", Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(customer));

            //Act
            await _customerManagerService.SetEmail(customer, "admin@admin.pl");
            //Assert
            Assert.AreEqual(customer.Email, "admin@admin.pl");
        }
        [TestMethod()]
        public async Task SetEmailTest_ThrowsException()
        {
            //Arrange
            var customer = new Customer() { Email = "email@email.com", Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
            _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(customer));

            //Act
            await Assert.ThrowsExceptionAsync<GrandException>(async () => await _customerManagerService.SetEmail(customer, "adminadmin.pl"));
        }
        [TestMethod()]
        public async Task SetUsernameTest_Success()
        {
            //Arrange
            _customerSettings.UsernamesEnabled = true;
            var customer = new Customer() { Email = "email@email.com", Username = "emailemail.com", Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
            _customerServiceMock.Setup(c => c.GetCustomerByUsername(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(customer));

            //Act
            await _customerManagerService.SetUsername(customer, "adminadmin.pl");
            //Assert
            Assert.AreEqual(customer.Username, "adminadmin.pl");
        }
        [TestMethod()]
        public async Task SetUsernameTest_ThrowsException()
        {
            //Arrange
            _customerSettings.UsernamesEnabled = true;
            var customer = new Customer() { Email = "email@email.com", Username = "emailemail.com", Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
            _customerServiceMock.Setup(c => c.GetCustomerByUsername(It.IsAny<string>())).Returns(() => Task.FromResult<Customer>(new Customer()));

            //Act
            await Assert.ThrowsExceptionAsync<GrandException>(async () => await _customerManagerService.SetUsername(customer, "adminadmin.pl"));
        }
    }
}