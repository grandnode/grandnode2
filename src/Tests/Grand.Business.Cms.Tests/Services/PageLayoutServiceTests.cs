using Grand.Business.Cms.Services;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Pages;
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
public class PageLayoutServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private PageLayoutService _pageLayoutService;

    private IRepository<PageLayout> _repository;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<PageLayout>();

        _mediatorMock = new Mock<IMediator>();
        _workContextMock = new Mock<IWorkContext>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "", Name = "test store" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());

        _pageLayoutService = new PageLayoutService(_repository, _cacheBase, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetAllPageLayoutsTest()
    {
        //Arrange
        var pageLayout = new PageLayout();
        await _repository.InsertAsync(pageLayout);
        //Act
        var result = await _pageLayoutService.GetAllPageLayouts();
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetPageLayoutByIdTest()
    {
        //Arrange
        var pageLayout = new PageLayout();
        await _repository.InsertAsync(pageLayout);
        //Act
        var result = await _pageLayoutService.GetPageLayoutById(pageLayout.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task InsertPageLayoutTest()
    {
        //Arrange
        var pageLayout = new PageLayout();
        //Act
        await _pageLayoutService.InsertPageLayout(pageLayout);
        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Id == pageLayout.Id));
    }

    [TestMethod]
    public async Task UpdatePageLayoutTest()
    {
        //Arrange
        var pageLayout = new PageLayout();
        await _pageLayoutService.InsertPageLayout(pageLayout);
        //Act
        pageLayout.Name = "test";
        await _pageLayoutService.UpdatePageLayout(pageLayout);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == pageLayout.Id).Name == "test");
    }

    [TestMethod]
    public async Task DeletePageLayoutTest()
    {
        //Arrange
        var pageLayout = new PageLayout();
        await _pageLayoutService.InsertPageLayout(pageLayout);
        //Act
        await _pageLayoutService.DeletePageLayout(pageLayout);
        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Id == pageLayout.Id));
    }
}