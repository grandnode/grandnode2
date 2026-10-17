using Grand.Business.Core.Events.Marketing;
using Grand.Business.Marketing.Services.Customers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Customers;

[TestClass]
public class CustomerCoordinatesServiceTests
{
    private CustomerCoordinatesService _customerCoordinatesService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Customer> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Customer>();
        _mediatorMock = new Mock<IMediator>();

        _customerCoordinatesService = new CustomerCoordinatesService(_repository, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetGeoCoordinateTest_CustomerWithoutCoordinates_ReturnsZero()
    {
        //Act
        var result = await _customerCoordinatesService.GetGeoCoordinate(new Customer());
        //Assert
        Assert.AreEqual(0, result.longitude);
        Assert.AreEqual(0, result.latitude);
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
}