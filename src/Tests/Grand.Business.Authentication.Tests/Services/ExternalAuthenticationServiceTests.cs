using Grand.Business.Authentication.Services;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class ExternalAuthenticationServiceTests
{
    private Mock<IGrandAuthenticationService> _authenticationServiceMock;
    private Mock<ICustomerManagerService> _customerManagerServiceMock;
    private Mock<ICustomerService> _customerServiceMock;
    private IEnumerable<IExternalAuthenticationProvider> _externalAuthenticationProviders;
    private IRepository<ExternalAuthentication> _externalAuthenticationRecordRepository;

    private ExternalAuthenticationService _externalAuthenticationService;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<IMediator> _mediatorMock;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _groupServiceMock = new Mock<IGroupService>();
        _authenticationServiceMock = new Mock<IGrandAuthenticationService>();
        _customerManagerServiceMock = new Mock<ICustomerManagerService>();
        _mediatorMock = new Mock<IMediator>();
        _externalAuthenticationRecordRepository = new MongoDBRepositoryTest<ExternalAuthentication>();
        _externalAuthenticationProviders = new List<IExternalAuthenticationProvider>
            { new ExternalAuthenticationProviderTest() };

        var externalAuthenticationSetting = new ExternalAuthenticationSettings {
            ActiveAuthenticationMethodSystemNames = [
                "ExternalAuthenticationProviderTest"
            ]
        };

        _externalAuthenticationService = new ExternalAuthenticationService(_authenticationServiceMock.Object,
            _customerManagerServiceMock.Object, _customerServiceMock.Object,
            _groupServiceMock.Object, _mediatorMock.Object, _externalAuthenticationRecordRepository,
            _externalAuthenticationProviders,
            new CustomerSettings { UserRegistrationType = UserRegistrationType.Standard }, externalAuthenticationSetting);
    }


    [TestMethod]
    public void LoadActiveAuthenticationProvidersTest()
    {
        //Act
        var result = _externalAuthenticationService.LoadActiveAuthenticationProviders(new Customer(), new Store());
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public void LoadAuthenticationProviderBySystemNameTest()
    {
        //Act
        var result =
            _externalAuthenticationService.LoadAuthenticationProviderBySystemName("ExternalAuthenticationProviderTest");
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void LoadAllAuthenticationProvidersTest()
    {
        //Act
        var result = _externalAuthenticationService.LoadAllAuthenticationProviders();
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public void AuthenticationProviderIsAvailableTest_Available()
    {
        //Act
        var result =
            _externalAuthenticationService.AuthenticationProviderIsAvailable("ExternalAuthenticationProviderTest",
                new Customer(), new Store());
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void AuthenticationProviderIsAvailableTest_NotAvailable()
    {
        //Act
        var result = _externalAuthenticationService.AuthenticationProviderIsAvailable("123", new Customer(), new Store());
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task Authenticate_AssociatedUserFromGuest_PublishesLoggedInEventWithPassedCustomerAndStore()
    {
        var user = new Customer { Id = "user" };
        var guest = new Customer { Id = "guest" };
        _customerServiceMock.Setup(c => c.GetCustomerById("user")).ReturnsAsync(user);
        var parameters = new ExternalAuthParam { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "1" };
        await _externalAuthenticationService.AssociateCustomer(user, parameters);

        await _externalAuthenticationService.Authenticate(parameters, guest, new Store { Id = "store1" });

        _mediatorMock.Verify(m => m.Publish(It.Is<CustomerLoggedInEvent>(e =>
            e.Customer == user && e.PreviousCustomer == guest && e.StoreId == "store1"), default), Times.Once);
        _authenticationServiceMock.Verify(a => a.SignIn(user, false), Times.Once);
    }

    [TestMethod]
    public async Task Authenticate_UnknownIdentity_RegistersPassedCustomerInPassedStore()
    {
        var guest = new Customer { Id = "guest" };
        var parameters = new ExternalAuthParam {
            ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "new", Email = "new@test.com"
        };

        await _externalAuthenticationService.Authenticate(parameters, guest, new Store { Id = "store1" });

        _customerManagerServiceMock.Verify(m => m.RegisterCustomer(It.Is<RegistrationRequest>(r =>
            r.Customer == guest && r.StoreId == "store1" && r.Email == "new@test.com")), Times.Once);
        _mediatorMock.Verify(m => m.Publish(It.Is<CustomerRegisteredEvent>(e => e.Customer == guest), default),
            Times.Once);
        _authenticationServiceMock.Verify(a => a.SignIn(guest, false), Times.Once);
        Assert.AreEqual("guest", _externalAuthenticationRecordRepository.Table.Single().CustomerId);
    }

    [TestMethod]
    public async Task AssociateCustomerTest()
    {
        //Act
        await _externalAuthenticationService.AssociateCustomer(new Customer(),
            new ExternalAuthParam { ProviderSystemName = "ExternalAuthenticationProviderTest" });
        //Assert
        Assert.IsTrue(_externalAuthenticationRecordRepository.Table.Any());
    }

    [TestMethod]
    public async Task GetCustomerTest()
    {
        //Arrange
        var expectedCustomer = new Customer { Username = "John", Active = true };
        _customerServiceMock.Setup(c => c.GetCustomerById(It.IsAny<string>()))
            .Returns(() => Task.FromResult(expectedCustomer));
        await _externalAuthenticationService.AssociateCustomer(expectedCustomer,
            new ExternalAuthParam { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "1" });
        //Act
        var customer = await _externalAuthenticationService.GetCustomer(new ExternalAuthParam
            { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "1" });
        //Assert
        Assert.IsNotNull(customer);
        Assert.AreEqual(customer.Username, expectedCustomer.Username);
    }

    [TestMethod]
    public async Task GetCustomer_IdentifierWithUpperCaseLetters_IsFound()
    {
        var expectedCustomer = new Customer { Username = "John", Active = true };
        _customerServiceMock.Setup(c => c.GetCustomerById(It.IsAny<string>()))
            .Returns(() => Task.FromResult(expectedCustomer));
        await _externalAuthenticationService.AssociateCustomer(expectedCustomer,
            new ExternalAuthParam { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "AbC-123" });

        var customer = await _externalAuthenticationService.GetCustomer(new ExternalAuthParam
            { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "AbC-123" });

        Assert.IsNotNull(customer);
    }

    [TestMethod]
    public async Task GetCustomer_IdentifierInDifferentCase_IsNotFound()
    {
        var expectedCustomer = new Customer { Username = "John", Active = true };
        _customerServiceMock.Setup(c => c.GetCustomerById(It.IsAny<string>()))
            .Returns(() => Task.FromResult(expectedCustomer));
        await _externalAuthenticationService.AssociateCustomer(expectedCustomer,
            new ExternalAuthParam { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "AbC-123" });

        var customer = await _externalAuthenticationService.GetCustomer(new ExternalAuthParam
            { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "abc-123" });

        Assert.IsNull(customer);
    }

    [TestMethod]
    public async Task GetCustomer_ProviderSystemNameInDifferentCase_IsFound()
    {
        var expectedCustomer = new Customer { Username = "John", Active = true };
        _customerServiceMock.Setup(c => c.GetCustomerById(It.IsAny<string>()))
            .Returns(() => Task.FromResult(expectedCustomer));
        await _externalAuthenticationService.AssociateCustomer(expectedCustomer,
            new ExternalAuthParam { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "AbC-123" });

        var customer = await _externalAuthenticationService.GetCustomer(new ExternalAuthParam
            { ProviderSystemName = "externalauthenticationprovidertest", Identifier = "AbC-123" });

        Assert.IsNotNull(customer);
    }

    [TestMethod]
    public async Task GetExternalIdentifiersTest()
    {
        //Arrange
        var customer = new Customer { Username = "John", Active = true };
        await _externalAuthenticationService.AssociateCustomer(customer,
            new ExternalAuthParam { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "1" });
        //Act
        var result = await _externalAuthenticationService.GetExternalIdentifiers(customer);
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task DeleteExternalAuthenticationTest()
    {
        //Arrange
        var customer = new Customer { Username = "John", Active = true };
        var externalAuthParam = new ExternalAuthParam
            { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "1" };
        await _externalAuthenticationService.AssociateCustomer(customer, externalAuthParam);
        //Act
        await _externalAuthenticationService.DeleteExternalAuthentication(
            _externalAuthenticationRecordRepository.Table.FirstOrDefault(x =>
                x.ExternalIdentifier == externalAuthParam.Identifier));
        //Assert
        Assert.IsFalse(_externalAuthenticationRecordRepository.Table.Any());
    }
}