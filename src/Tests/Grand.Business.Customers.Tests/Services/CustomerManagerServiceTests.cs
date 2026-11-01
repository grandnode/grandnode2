using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Business.Customers.Services;
using Grand.Domain.Customers;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services;

[TestClass]
public class CustomerManagerServiceTests
{
    private Mock<ICustomerHistoryPasswordService> _customerHistoryPasswordServiceMock;
    private CustomerManagerService _customerManagerService;

    private Mock<ICustomerService> _customerServiceMock;
    private CustomerSettings _customerSettings;
    private Mock<IEncryptionService> _encryptionServiceMock;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IExternalSignInPolicy> _signInPolicy;
    private Mock<ILogger<CustomerManagerService>> _logger;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _groupServiceMock = new Mock<IGroupService>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        //emulate the real format-aware verification for the Clear-format credentials used by these tests
        _encryptionServiceMock.Setup(e => e.VerifyPassword(It.IsAny<string>(), It.IsAny<PasswordFormat>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HashedPasswordFormat>()))
            .Returns((string entered, PasswordFormat _, string stored, string _, HashedPasswordFormat _) =>
                entered == stored);
        _mediatorMock = new Mock<IMediator>();
        _customerHistoryPasswordServiceMock = new Mock<ICustomerHistoryPasswordService>();
        _logger = new Mock<ILogger<CustomerManagerService>>();
        _signInPolicy = new Mock<IExternalSignInPolicy>();
        _signInPolicy.Setup(x => x.PasswordSignIn(It.IsAny<Customer>(), It.IsAny<SignInSurface>()))
            .ReturnsAsync(PasswordSignInDecision.Password);
        _customerSettings = new CustomerSettings {
            AllowUsersToChangeUsernames = true
        };

        _customerManagerService = new CustomerManagerService(_customerServiceMock.Object, _groupServiceMock.Object,
            _encryptionServiceMock.Object, _mediatorMock.Object, _customerHistoryPasswordServiceMock.Object, _customerSettings,
            _signInPolicy.Object, _logger.Object);
    }

    [TestMethod]
    public async Task LoginCustomer_ExternalSignInRequired_AfterCorrectPassword()
    {
        var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(customer);
        _signInPolicy.Setup(x => x.PasswordSignIn(customer, It.IsAny<SignInSurface>())).ReturnsAsync(new PasswordSignInDecision(false, new Mock<IExternalAuthenticationProvider>().Object));
        Assert.AreEqual(CustomerLoginResults.ExternalSignInRequired,
            await _customerManagerService.LoginCustomer("admin@admin.com", "123456"));
    }

    [TestMethod]
    public async Task LoginCustomer_PassesSurfaceToPolicy()
    {
        var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(customer);
        _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);
        await _customerManagerService.LoginCustomer("admin@admin.com", "123456", "", SignInSurface.AdminPanel);
        _signInPolicy.Verify(x => x.PasswordSignIn(It.IsAny<Customer>(), SignInSurface.AdminPanel), Times.Once);
    }

    [TestMethod]
    public async Task LoginCustomer_BreakGlass_SuccessfulAndWarningLogged()
    {
        var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(customer);
        _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);
        _signInPolicy.Setup(x => x.PasswordSignIn(It.IsAny<Customer>(), It.IsAny<SignInSurface>()))
            .ReturnsAsync(new PasswordSignInDecision(true, null, true));

        var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456", "", SignInSurface.AdminPanel);

        Assert.AreEqual(CustomerLoginResults.Successful, result);
        _logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [TestMethod]
    public async Task LoginCustomer_RefusedByPolicy_ExternalSignInRequired()
    {
        var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(customer);
        _signInPolicy.Setup(x => x.PasswordSignIn(It.IsAny<Customer>(), It.IsAny<SignInSurface>()))
            .ReturnsAsync(new PasswordSignInDecision(false, null));
        Assert.AreEqual(CustomerLoginResults.ExternalSignInRequired,
            await _customerManagerService.LoginCustomer("admin@admin.com", "123456"));
    }

    //a wrong password says nothing about how the account signs in
    [TestMethod]
    public async Task LoginCustomer_WrongPassword_PolicyNotAsked()
    {
        var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(customer);
        Assert.AreEqual(CustomerLoginResults.WrongPassword, await _customerManagerService.LoginCustomer("a@b.c", "bad"));
        _signInPolicy.Verify(x => x.PasswordSignIn(It.IsAny<Customer>(), It.IsAny<SignInSurface>()), Times.Never);
    }

    [TestMethod]
    public async Task LoginCustomerTest_WrongPassword()
    {
        //Arrange
        var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "12345" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(customer));
        _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(true));
        //Act
        var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456");
        //Assert
        Assert.AreEqual(CustomerLoginResults.WrongPassword, result);
    }

    [TestMethod]
    public async Task LoginCustomerTest_Successful()
    {
        //Arrange
        var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(customer));
        _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(true));
        //Act
        var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456");
        //Assert
        Assert.AreEqual(CustomerLoginResults.Successful, result);
    }

    [TestMethod]
    public async Task LoginCustomer_PassesStoreIdToLookup()
    {
        //Arrange
        var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail("admin@admin.com", "store-1"))
            .ReturnsAsync(customer);
        _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);
        //Act
        var result = await _customerManagerService.LoginCustomer("admin@admin.com", "123456", "store-1");
        //Assert
        Assert.AreEqual(CustomerLoginResults.Successful, result);
        _customerServiceMock.Verify(c => c.GetCustomerByEmail("admin@admin.com", "store-1"), Times.Once);
    }

    [TestMethod]
    public async Task ChangePassword_PassesStoreIdToLookup()
    {
        //Arrange
        var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail("admin@admin.com", "store-1"))
            .ReturnsAsync(customer);
        var changepassword = new ChangePasswordRequest("admin@admin.com", PasswordFormat.Clear, "zxcvbn", "123456");
        //Act
        await _customerManagerService.ChangePassword(changepassword, "store-1");
        //Assert
        _customerServiceMock.Verify(c => c.GetCustomerByEmail("admin@admin.com", "store-1"), Times.Once);
    }

    [TestMethod]
    public async Task ChangePassword_ForCustomer_ChangesThatCustomerWithoutLookup()
    {
        //Arrange
        var customer = new Customer { Id = "c1", Email = "a@x.com", PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
        //Act
        await _customerManagerService.ChangePassword(customer, PasswordFormat.Clear, "zxcvbn");
        //Assert
        Assert.AreEqual("zxcvbn", customer.Password);
        _customerServiceMock.Verify(c => c.UpdateCustomer(customer), Times.Once);
        _customerServiceMock.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangePassword_UnknownEmail_Throws()
    {
        //Arrange
        var changepassword = new ChangePasswordRequest("nobody@x.com", PasswordFormat.Clear, "zxcvbn");
        //Act + Assert
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() =>
            _customerManagerService.ChangePassword(changepassword, "store-1"));
    }

    [TestMethod]
    public async Task ChangePasswordTest_Success()
    {
        //Arrange
        var customer = new Customer { Active = true, PasswordFormatId = PasswordFormat.Clear, Password = "123456" };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>()))
            .Returns(() => Task.FromResult(customer));

        var changepassword = new ChangePasswordRequest("admin@admin.com",
            PasswordFormat.Clear,
            "zxcvbn", "123456");
        //Act
        await _customerManagerService.ChangePassword(changepassword);
        //Assert
        var passwordMatch = _customerManagerService.PasswordMatch(PasswordFormat.Clear, "zxcvb", "zxcvb", string.Empty);
        Assert.IsTrue(passwordMatch);
    }
}