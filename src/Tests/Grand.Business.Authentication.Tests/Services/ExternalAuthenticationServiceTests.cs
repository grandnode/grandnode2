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
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class ExternalAuthenticationServiceTests
{
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
        _customerManagerServiceMock = new Mock<ICustomerManagerService>();
        _mediatorMock = new Mock<IMediator>();
        _externalAuthenticationRecordRepository = new MongoDBRepositoryTest<ExternalAuthentication>();
        _externalAuthenticationProviders = new List<IExternalAuthenticationProvider>
            { new ExternalAuthenticationProviderTest() };

        _externalAuthenticationService = CreateService(UserRegistrationType.Standard);
    }

    private ExternalAuthenticationService CreateService(UserRegistrationType userRegistrationType)
    {
        var externalAuthenticationSetting = new ExternalAuthenticationSettings {
            ActiveAuthenticationMethodSystemNames = [
                "ExternalAuthenticationProviderTest"
            ]
        };

        return new ExternalAuthenticationService(_customerManagerServiceMock.Object, _customerServiceMock.Object,
            _groupServiceMock.Object, _mediatorMock.Object, _externalAuthenticationRecordRepository,
            _externalAuthenticationProviders,
            new CustomerSettings { UserRegistrationType = userRegistrationType }, externalAuthenticationSetting);
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
    public void LoadActiveAuthenticationProviders_NullCustomerOrStore_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _externalAuthenticationService.LoadActiveAuthenticationProviders(null, new Store()));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _externalAuthenticationService.LoadActiveAuthenticationProviders(new Customer(), null));
    }

    [TestMethod]
    public void AuthenticationProviderIsAvailable_NullCustomerOrStore_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _externalAuthenticationService.AuthenticationProviderIsAvailable("ExternalAuthenticationProviderTest",
                null, new Store()));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _externalAuthenticationService.AuthenticationProviderIsAvailable("ExternalAuthenticationProviderTest",
                new Customer(), null));
    }

    [TestMethod]
    public async Task Authenticate_AssociatedUserFromGuest_PublishesLoggedInEventWithPassedCustomerStoreAndLanguage()
    {
        var user = new Customer { Id = "user" };
        var guest = new Customer { Id = "guest" };
        var language = new Language { Id = "lang1" };
        _customerServiceMock.Setup(c => c.GetCustomerById("user")).ReturnsAsync(user);
        var parameters = new ExternalAuthParam { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "1" };
        await _externalAuthenticationService.AssociateCustomer(user, parameters);

        var result =
            await _externalAuthenticationService.Authenticate(parameters, guest, new Store { Id = "store1" }, language);

        _mediatorMock.Verify(m => m.Publish(It.Is<CustomerLoggedInEvent>(e =>
            e.Customer == user && e.PreviousCustomer == guest && e.StoreId == "store1" && e.Language == language),
            default), Times.Once);
        Assert.AreEqual(ExternalAuthStatus.LoggedIn, result.Status);
        Assert.AreSame(user, result.Customer);
    }

    [TestMethod]
    public async Task Authenticate_UnknownIdentity_RegistersPassedCustomerInPassedStore()
    {
        var guest = new Customer { Id = "guest" };
        var language = new Language { Id = "lang1" };
        var parameters = new ExternalAuthParam {
            ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "new", Email = "new@test.com"
        };

        var result =
            await _externalAuthenticationService.Authenticate(parameters, guest, new Store { Id = "store1" }, language);

        _customerManagerServiceMock.Verify(m => m.RegisterCustomer(It.Is<RegistrationRequest>(r =>
            r.Customer == guest && r.StoreId == "store1" && r.Email == "new@test.com")), Times.Once);
        _mediatorMock.Verify(m => m.Publish(It.Is<CustomerRegisteredEvent>(e =>
                e.Customer == guest && e.Language == language), default),
            Times.Once);
        Assert.AreEqual("guest", _externalAuthenticationRecordRepository.Table.Single().CustomerId);
        Assert.AreEqual(ExternalAuthStatus.Registered, result.Status);
        Assert.AreSame(guest, result.Customer);
    }

    [TestMethod]
    public async Task Authenticate_UnknownIdentityWithAdminApproval_IsAwaitingApproval()
    {
        var guest = new Customer { Id = "guest" };
        var parameters = new ExternalAuthParam {
            ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "new", Email = "new@test.com"
        };

        var result = await CreateService(UserRegistrationType.AdminApproval)
            .Authenticate(parameters, guest, new Store { Id = "store1" }, new Language());

        _customerManagerServiceMock.Verify(m => m.RegisterCustomer(It.Is<RegistrationRequest>(r =>
            r.Customer == guest && !r.IsApproved)), Times.Once);
        Assert.AreEqual(ExternalAuthStatus.AwaitingApproval, result.Status);
    }

    [TestMethod]
    public async Task Authenticate_UnknownIdentityWithRegistrationDisabled_RegistersNobody()
    {
        var parameters = new ExternalAuthParam { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "new" };

        var result = await CreateService(UserRegistrationType.Disabled)
            .Authenticate(parameters, new Customer { Id = "guest" }, new Store(), new Language());

        _customerManagerServiceMock.Verify(m => m.RegisterCustomer(It.IsAny<RegistrationRequest>()), Times.Never);
        Assert.AreEqual(ExternalAuthStatus.RegistrationDisabled, result.Status);
        Assert.IsFalse(_externalAuthenticationRecordRepository.Table.Any());
    }

    [TestMethod]
    public async Task Authenticate_UnknownIdentityWhileLoggedIn_AssociatesItWithLoggedInCustomer()
    {
        var customer = new Customer { Id = "registered" };
        _groupServiceMock.Setup(g => g.IsRegistered(customer)).ReturnsAsync(true);
        var parameters = new ExternalAuthParam { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "new" };

        var result = await _externalAuthenticationService.Authenticate(parameters, customer, new Store(), new Language());

        Assert.AreEqual(ExternalAuthStatus.Associated, result.Status);
        Assert.AreEqual("registered", _externalAuthenticationRecordRepository.Table.Single().CustomerId);
        _mediatorMock.Verify(m => m.Publish(It.IsAny<CustomerLoggedInEvent>(), default), Times.Never);
    }

    [TestMethod]
    public async Task Authenticate_IdentityOfAnotherCustomerWhileLoggedIn_IsAlreadyAssigned()
    {
        var owner = new Customer { Id = "owner" };
        var customer = new Customer { Id = "registered" };
        _groupServiceMock.Setup(g => g.IsRegistered(customer)).ReturnsAsync(true);
        _customerServiceMock.Setup(c => c.GetCustomerById("owner")).ReturnsAsync(owner);
        var parameters = new ExternalAuthParam { ProviderSystemName = "ExternalAuthenticationProviderTest", Identifier = "1" };
        await _externalAuthenticationService.AssociateCustomer(owner, parameters);

        var result = await _externalAuthenticationService.Authenticate(parameters, customer, new Store(), new Language());

        Assert.AreEqual(ExternalAuthStatus.AlreadyAssigned, result.Status);
        CollectionAssert.AreEqual(new[] { "Account is already assigned" }, result.Errors.ToArray());
    }

    [TestMethod]
    public async Task Authenticate_UnavailableProvider_IsError()
    {
        var parameters = new ExternalAuthParam { ProviderSystemName = "Unknown", Identifier = "1" };

        var result = await _externalAuthenticationService.Authenticate(parameters, new Customer(), new Store(), new Language());

        Assert.AreEqual(ExternalAuthStatus.Error, result.Status);
        _customerManagerServiceMock.Verify(m => m.RegisterCustomer(It.IsAny<RegistrationRequest>()), Times.Never);
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