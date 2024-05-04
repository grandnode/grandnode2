using Grand.Business.Customers.Services;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services;

[TestClass]
public class SalesEmployeeServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private IRepository<SalesEmployee> _repository;
    private SalesEmployeeService _salesEmployeeService;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<SalesEmployee>();
        _mediatorMock = new Mock<IMediator>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _salesEmployeeService = new SalesEmployeeService(_repository, _mediatorMock.Object, _cacheBase);
    }


    [TestMethod]
    public async Task GetSalesEmployeeByIdTest()
    {
        //Arrange
        var salesEmployee = new SalesEmployee();
        await _repository.InsertAsync(salesEmployee);
        //Act
        var result = await _salesEmployeeService.GetSalesEmployeeById(salesEmployee.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetAllTest()
    {
        //Arrange
        await _repository.InsertAsync(new SalesEmployee());
        await _repository.InsertAsync(new SalesEmployee());
        await _repository.InsertAsync(new SalesEmployee());
        //Act
        var result = await _salesEmployeeService.GetAll();
        //Assert
        Assert.AreEqual(3, result.Count);
    }

    [TestMethod]
    public async Task InsertSalesEmployeeTest()
    {
        //Act
        await _salesEmployeeService.InsertSalesEmployee(new SalesEmployee());
        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateSalesEmployeeTest()
    {
        //Arrange
        var salesEmployee = new SalesEmployee();
        await _repository.InsertAsync(salesEmployee);
        //Act
        salesEmployee.Email = "email@email.com";
        await _salesEmployeeService.UpdateSalesEmployee(salesEmployee);
        //Assert
        Assert.AreEqual("email@email.com", _repository.Table.FirstOrDefault(x => x.Id == salesEmployee.Id).Email);
    }

    [TestMethod]
    public async Task DeleteSalesEmployeeTest()
    {
        //Arrange
        var salesEmployee = new SalesEmployee();
        await _repository.InsertAsync(salesEmployee);
        //Act
        await _salesEmployeeService.DeleteSalesEmployee(salesEmployee);
        //Assert
        Assert.IsFalse(_repository.Table.Any());
    }
}