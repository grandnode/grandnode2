using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using MediatR;
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
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _groupServiceMock = new Mock<IGroupService>();
        _authenticationServiceMock = new Mock<IGrandAuthenticationService>();
        _customerManagerServiceMock = new Mock<ICustomerManagerService>();
        _mediatorMock = new Mock<IMediator>();
        _externalAuthenticationRecordRepository = new MongoDBRepositoryTest<ExternalAuthentication>();
        _workContextMock = new Mock<IWorkContext>();
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
            _workContextMock.Object, _externalAuthenticationProviders,
            new CustomerSettings(), externalAuthenticationSetting);
    }


    [TestMethod]
    public void LoadActiveAuthenticationProvidersTest()
    {
        //Act
        var result = _externalAuthenticationService.LoadActiveAuthenticationProviders();
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
            _externalAuthenticationService.AuthenticationProviderIsAvailable("ExternalAuthenticationProviderTest");
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void AuthenticationProviderIsAvailableTest_NotAvailable()
    {
        //Act
        var result = _externalAuthenticationService.AuthenticationProviderIsAvailable("123");
        //Assert
        Assert.IsFalse(result);
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