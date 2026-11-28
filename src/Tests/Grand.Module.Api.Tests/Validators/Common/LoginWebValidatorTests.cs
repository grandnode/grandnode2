using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
using Grand.Mediator;
using Grand.Module.Api.Models.Common;
using Grand.Module.Api.Validators.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text;

namespace Grand.Module.Api.Tests.Validators.Common;

[TestClass]
public class LoginWebValidatorTests
{
    private const string StoreId = "store-1";

    private Mock<ICustomerService> _customerServiceMock;
    private Mock<ICustomerManagerService> _customerManagerServiceMock;
    private Mock<IContextAccessor> _contextAccessorMock;
    private Mock<IMediator> _mediatorMock;
    private CustomerSettings _customerSettings;

    public LoginWebValidatorTests()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _customerManagerServiceMock = new Mock<ICustomerManagerService>();
        _mediatorMock = new Mock<IMediator>();
        _customerSettings = new CustomerSettings();

        var storeContextMock = new Mock<IStoreContext>();
        storeContextMock.Setup(s => s.CurrentStore).Returns(new Store { Id = StoreId });
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(c => c.StoreContext).Returns(storeContextMock.Object);
    }

    private LoginWebValidator CreateValidator(bool apiEnabled) =>
        new(new List<IValidatorConsumer<LoginWebModel>>(),
            new FrontendAPIConfig { Enabled = apiEnabled },
            _customerSettings,
            _customerServiceMock.Object,
            _customerManagerServiceMock.Object,
            _contextAccessorMock.Object,
            _mediatorMock.Object);

    private static LoginWebModel ValidModel() => new() {
        Email = "user@x.com",
        Password = Convert.ToBase64String(Encoding.UTF8.GetBytes("secret"))
    };

    [TestMethod]
    public async Task ApiDisabled_Fails()
    {
        var validator = CreateValidator(apiEnabled: false);
        var result = await validator.ValidateAsync(ValidModel());
        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == "API is disabled"));
    }

    [TestMethod]
    public async Task ValidCredentials_ScopesLookupAndLoginToCurrentStore()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId))
            .ReturnsAsync(new Customer { Active = true });
        _customerManagerServiceMock.Setup(m => m.LoginCustomer(It.IsAny<string>(), It.IsAny<string>(), StoreId, It.IsAny<SignInSurface>()))
            .ReturnsAsync(CustomerLoginResults.Successful);

        var validator = CreateValidator(apiEnabled: true);
        var result = await validator.ValidateAsync(ValidModel());

        Assert.IsTrue(result.IsValid);
        _customerServiceMock.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId), Times.Once);
        _customerManagerServiceMock.Verify(m => m.LoginCustomer(It.IsAny<string>(), It.IsAny<string>(), StoreId, SignInSurface.FrontendApi), Times.Once);
    }

    [TestMethod]
    public async Task ExternalSignInRequired_Fails()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Customer { Active = true });
        _customerManagerServiceMock.Setup(m => m.LoginCustomer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SignInSurface>()))
            .ReturnsAsync(CustomerLoginResults.ExternalSignInRequired);

        var validator = CreateValidator(apiEnabled: true);
        var result = await validator.ValidateAsync(ValidModel());

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task WrongCredentials_Fails()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Customer?)null);

        var validator = CreateValidator(apiEnabled: true);
        var result = await validator.ValidateAsync(ValidModel());

        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == "Customer not exist or password is wrong"));
    }

    [TestMethod]
    public async Task LockedOutAccount_Fails_WithoutCheckingThePassword()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Customer { Active = true, CannotLoginUntilDateUtc = DateTime.UtcNow.AddMinutes(5) });

        var result = await CreateValidator(apiEnabled: true).ValidateAsync(ValidModel());

        Assert.IsFalse(result.IsValid);
        _customerManagerServiceMock.Verify(m => m.LoginCustomer(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<SignInSurface>()), Times.Never);
    }

    [TestMethod]
    public async Task WrongPassword_CountsAsAFailedLogin()
    {
        var customer = new Customer { Active = true };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(customer);
        _customerManagerServiceMock.Setup(m => m.LoginCustomer(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<SignInSurface>())).ReturnsAsync(CustomerLoginResults.WrongPassword);

        var result = await CreateValidator(apiEnabled: true).ValidateAsync(ValidModel());

        Assert.IsFalse(result.IsValid);
        _mediatorMock.Verify(m => m.Publish(It.Is<CustomerLoginFailedEvent>(e => e.Customer == customer),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task PasswordNotBase64_Fails()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Customer { Active = true });

        var result = await CreateValidator(apiEnabled: true)
            .ValidateAsync(new LoginWebModel { Email = "user@x.com", Password = "not base64!" });

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task NoPassword_FailsWithoutThrowing()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Customer { Active = true });

        var result = await CreateValidator(apiEnabled: true)
            .ValidateAsync(new LoginWebModel { Email = "user@x.com", Password = null! });

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task ValidCredentials_ClearsTheFailedAttempts()
    {
        var customer = new Customer { Active = true, FailedLoginAttempts = 2 };
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(customer);
        _customerManagerServiceMock.Setup(m => m.LoginCustomer(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<SignInSurface>())).ReturnsAsync(CustomerLoginResults.Successful);

        var result = await CreateValidator(apiEnabled: true).ValidateAsync(ValidModel());

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, customer.FailedLoginAttempts);
        _customerServiceMock.Verify(c => c.UpdateCustomerLastLoginDate(customer), Times.Once);
    }

    [TestMethod]
    public async Task UsernamesEnabled_SignsInWithTheUsernameOfTheCustomerFoundByEmail()
    {
        _customerSettings.UsernamesEnabled = true;
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Customer { Active = true, Username = "user1" });
        _customerManagerServiceMock.Setup(m => m.LoginCustomer("user1", "secret", StoreId,
            SignInSurface.FrontendApi)).ReturnsAsync(CustomerLoginResults.Successful);

        var result = await CreateValidator(apiEnabled: true).ValidateAsync(ValidModel());

        Assert.IsTrue(result.IsValid);
    }
}
