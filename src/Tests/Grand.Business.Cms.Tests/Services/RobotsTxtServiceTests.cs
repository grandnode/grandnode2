using Grand.Business.Cms.Services;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Cms.Tests.Services;

[TestClass]
public class RobotsTxtServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;

    private IRepository<RobotsTxt> _repository;
    private RobotsTxtService _robotsTxtService;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<RobotsTxt>();

        _mediatorMock = new Mock<IMediator>();
        _workContextMock = new Mock<IWorkContext>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "", Name = "test store" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());

        _robotsTxtService = new RobotsTxtService(_repository, _mediatorMock.Object, _cacheBase);
    }

    [TestMethod]
    public async Task GetRobotsTxtTest()
    {
        //Arrange
        var robotsTxt = new RobotsTxt { StoreId = "1" };
        await _repository.InsertAsync(robotsTxt);
        //Act
        var result = await _robotsTxtService.GetRobotsTxt(robotsTxt.StoreId);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task InsertRobotsTxtTest()
    {
        //Arrange
        var robotsTxt = new RobotsTxt { StoreId = "1" };
        //Act
        await _robotsTxtService.InsertRobotsTxt(robotsTxt);
        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Id == robotsTxt.Id));
    }

    [TestMethod]
    public async Task UpdateRobotsTxtTest()
    {
        //Arrange
        var robotsTxt = new RobotsTxt { StoreId = "1" };
        await _robotsTxtService.InsertRobotsTxt(robotsTxt);
        //Act
        robotsTxt.Text = "test";
        await _robotsTxtService.UpdateRobotsTxt(robotsTxt);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == robotsTxt.Id).Text == "test");
    }

    [TestMethod]
    public async Task DeleteRobotsTxtTest()
    {
        //Arrange
        var robotsTxt = new RobotsTxt { StoreId = "1" };
        await _robotsTxtService.InsertRobotsTxt(robotsTxt);
        //Act
        await _robotsTxtService.DeleteRobotsTxt(robotsTxt);
        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Id == robotsTxt.Id));
    }
}