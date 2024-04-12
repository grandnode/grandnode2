using Grand.Business.Cms.Services;
using Grand.Business.Common.Services.Security;
using Grand.Business.Core.Interfaces.Common.Security;
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
public class PageServiceTests
{
    private IAclService _aclService;
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private PageService _pageService;

    private IRepository<Page> _repository;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Page>();

        _mediatorMock = new Mock<IMediator>();
        _workContextMock = new Mock<IWorkContext>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _aclService = new AclService(new AccessControlConfig());

        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "", Name = "test store" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());

        _pageService = new PageService(_repository, _workContextMock.Object, _aclService, _mediatorMock.Object,
            _cacheBase, new AccessControlConfig());
    }

    [TestMethod]
    public async Task GetPageByIdTest()
    {
        //Arrange
        var page = new Page();
        await _repository.InsertAsync(page);
        //Act
        var result = await _pageService.GetPageById(page.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetPageBySystemNameTest()
    {
        //Arrange
        var page = new Page { SystemName = "test" };
        await _repository.InsertAsync(page);
        //Act
        var result = await _pageService.GetPageBySystemName(page.SystemName);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetAllPagesTest()
    {
        //Arrange
        var page = new Page { SystemName = "test", Published = true };
        await _repository.InsertAsync(page);
        //Act
        var result = await _pageService.GetAllPages("");
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task InsertPageTest()
    {
        //Arrange
        var page = new Page();
        //Act
        await _pageService.InsertPage(page);
        //Assert
        Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Id == page.Id));
    }

    [TestMethod]
    public async Task UpdatePageTest()
    {
        //Arrange
        var page = new Page();
        await _pageService.InsertPage(page);
        //Act
        page.SystemName = "test";
        await _pageService.UpdatePage(page);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == page.Id).SystemName == "test");
    }

    [TestMethod]
    public async Task DeletePageTest()
    {
        //Arrange
        var page = new Page();
        await _pageService.InsertPage(page);
        //Act
        await _pageService.DeletePage(page);
        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Id == page.Id));
    }
}