using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Business.Customers.Services;
using Grand.Domain.Customers;
using Grand.Mediator;
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
        _customerSettings = new CustomerSettings {
            AllowUsersToChangeUsernames = true
        };

        _customerManagerService = new CustomerManagerService(_customerServiceMock.Object, _groupServiceMock.Object,
            _encryptionServiceMock.Object, _mediatorMock.Object, _customerHistoryPasswordServiceMock.Object, _customerSettings);
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
        _customerServiceMock.Verify(c => c.UpdateCustomerPassword(customer), Times.Once);
        //the whole-customer update would write back everything else the caller's copy holds
        _customerServiceMock.Verify(c => c.UpdateCustomer(It.IsAny<Customer>()), Times.Never);
        _customerServiceMock.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangePassword_ImplementationBuiltForAnEarlierRelease_UsesItsExistingMethod()
    {
        ICustomerManagerService manager = new EarlierReleaseManager();

        await manager.ChangePassword(new Customer { Email = "a@x.com", StoreId = "store-1" }, PasswordFormat.Hashed, "new");

        var implementation = (EarlierReleaseManager)manager;
        Assert.AreEqual("a@x.com", implementation.Request.Email);
        Assert.AreEqual(PasswordFormat.Hashed, implementation.Request.PasswordFormat);
        Assert.AreEqual("new", implementation.Request.NewPassword);
        Assert.AreEqual("store-1", implementation.StoreId);
    }

    [TestMethod]
    public void NewInterfaceMembers_HaveDefaultBodies_SoExistingImplementationsKeepLoading()
    {
        Assert.IsFalse(typeof(ICustomerService).GetMethod(nameof(ICustomerService.UpdateCustomerPassword))!.IsAbstract);
        Assert.IsFalse(typeof(ICustomerManagerService).GetMethod(nameof(ICustomerManagerService.ChangePassword),
            [typeof(Customer), typeof(PasswordFormat), typeof(string)])!.IsAbstract);
    }

    private sealed class EarlierReleaseManager : ICustomerManagerService
    {
        public ChangePasswordRequest Request { get; private set; }
        public string StoreId { get; private set; }

        public Task<CustomerLoginResults> LoginCustomer(string usernameOrEmail, string password, string storeId = "") =>
            throw new NotImplementedException();

        public Task RegisterCustomer(RegistrationRequest request) => throw new NotImplementedException();

        public bool PasswordMatch(PasswordFormat passwordFormat, string oldPassword, string newPassword,
            string passwordSalt) => throw new NotImplementedException();

        public Task ChangePassword(ChangePasswordRequest request, string storeId = "")
        {
            Request = request;
            StoreId = storeId;
            return Task.CompletedTask;
        }
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