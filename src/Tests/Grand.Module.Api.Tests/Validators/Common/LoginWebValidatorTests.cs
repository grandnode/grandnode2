using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Validators;
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

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _customerManagerServiceMock = new Mock<ICustomerManagerService>();

        var storeContextMock = new Mock<IStoreContext>();
        storeContextMock.Setup(s => s.CurrentStore).Returns(new Store { Id = StoreId });
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(c => c.StoreContext).Returns(storeContextMock.Object);
    }

    private LoginWebValidator CreateValidator(bool apiEnabled, bool perStore) =>
        new(new List<IValidatorConsumer<LoginWebModel>>(),
            new FrontendAPIConfig { Enabled = apiEnabled },
            _customerServiceMock.Object,
            _customerManagerServiceMock.Object,
            _contextAccessorMock.Object,
            new CustomerConfig { RegisterCustomersPerStore = perStore });

    private static LoginWebModel ValidModel() => new() {
        Email = "user@x.com",
        Password = Convert.ToBase64String(Encoding.UTF8.GetBytes("secret"))
    };

    [TestMethod]
    public async Task ApiDisabled_Fails()
    {
        var validator = CreateValidator(apiEnabled: false, perStore: true);
        var result = await validator.ValidateAsync(ValidModel());
        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == "API is disabled"));
    }

    [TestMethod]
    public async Task PerStoreOn_ValidCredentials_ScopesLookupAndLoginToCurrentStore()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId))
            .ReturnsAsync(new Customer { Active = true });
        _customerManagerServiceMock.Setup(m => m.LoginCustomer(It.IsAny<string>(), It.IsAny<string>(), StoreId))
            .ReturnsAsync(CustomerLoginResults.Successful);

        var validator = CreateValidator(apiEnabled: true, perStore: true);
        var result = await validator.ValidateAsync(ValidModel());

        Assert.IsTrue(result.IsValid);
        _customerServiceMock.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId), Times.Once);
        _customerManagerServiceMock.Verify(m => m.LoginCustomer(It.IsAny<string>(), It.IsAny<string>(), StoreId), Times.Once);
    }

    [TestMethod]
    public async Task PerStoreOff_UsesGlobalLookup()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), ""))
            .ReturnsAsync(new Customer { Active = true });
        _customerManagerServiceMock.Setup(m => m.LoginCustomer(It.IsAny<string>(), It.IsAny<string>(), ""))
            .ReturnsAsync(CustomerLoginResults.Successful);

        var validator = CreateValidator(apiEnabled: true, perStore: false);
        var result = await validator.ValidateAsync(ValidModel());

        Assert.IsTrue(result.IsValid);
        _customerManagerServiceMock.Verify(m => m.LoginCustomer(It.IsAny<string>(), It.IsAny<string>(), ""), Times.Once);
    }

    [TestMethod]
    public async Task WrongCredentials_Fails()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((Customer)null);

        var validator = CreateValidator(apiEnabled: true, perStore: true);
        var result = await validator.ValidateAsync(ValidModel());

        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == "Customer not exist or password is wrong"));
    }
}
