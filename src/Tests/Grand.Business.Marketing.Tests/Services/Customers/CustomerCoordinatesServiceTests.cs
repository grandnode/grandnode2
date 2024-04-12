using Grand.Business.Core.Events.Marketing;
using Grand.Business.Marketing.Services.Customers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Customers;

[TestClass]
public class CustomerCoordinatesServiceTests
{
    private Customer _customer;
    private CustomerCoordinatesService _customerCoordinatesService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Customer> _repository;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Customer>();
        _mediatorMock = new Mock<IMediator>();
        _workContextMock = new Mock<IWorkContext>();
        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "" });
        _customer = new Customer();
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => _customer);

        _customerCoordinatesService =
            new CustomerCoordinatesService(_repository, _workContextMock.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetGeoCoordinateTest_WorkContext()
    {
        //Arrange
        await _customerCoordinatesService.SaveGeoCoordinate(1, 2);
        //Act
        var result = await _customerCoordinatesService.GetGeoCoordinate();
        //Assert
        Assert.AreEqual(1, result.longitude);
        Assert.AreEqual(2, result.latitude);
    }

    [TestMethod]
    public async Task GetGeoCoordinateTest_Customer()
    {
        //Arrange
        var customer = new Customer();
        await _repository.InsertAsync(customer);
        await _customerCoordinatesService.SaveGeoCoordinate(customer, 1, 2);
        //Act
        var result = await _customerCoordinatesService.GetGeoCoordinate(customer);
        //Assert
        Assert.AreEqual(1, result.longitude);
        Assert.AreEqual(2, result.latitude);
    }

    [TestMethod]
    public async Task SaveGeoCoordinateTest_Customer()
    {
        //Arrange
        var customer = new Customer();
        await _repository.InsertAsync(customer);
        //Act
        await _customerCoordinatesService.SaveGeoCoordinate(customer, 1, 2);
        //Assert
        Assert.AreEqual(1, _repository.Table.FirstOrDefault(x => x.Id == customer.Id).Coordinates.X);
        Assert.AreEqual(2, _repository.Table.FirstOrDefault(x => x.Id == customer.Id).Coordinates.Y);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<CustomerCoordinatesEvent>(), default), Times.Once);
    }

    [TestMethod]
    public async Task SaveGeoCoordinateTest_WorkContext()
    {
        //Act
        await _customerCoordinatesService.SaveGeoCoordinate(1, 2);
        //Assert
        Assert.AreEqual(1, _workContextMock.Object.CurrentCustomer.Coordinates.X);
        Assert.AreEqual(2, _workContextMock.Object.CurrentCustomer.Coordinates.Y);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<CustomerCoordinatesEvent>(), default), Times.Once);
    }
}