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
using Grand.Mediator;
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
    private Mock<IContextAccessor> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Page>();

        _mediatorMock = new Mock<IMediator>();
        _workContextMock = new Mock<IContextAccessor>();

        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _aclService = new AclService(new AccessControlConfig());

        _workContextMock.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "", Name = "test store" });
        _workContextMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => new Customer());

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
        var result = await _pageService.GetPageBySystemName(page.SystemName, storeId: "");
        //Assert
        Assert.IsNotNull(result);
    }

    /// <summary>
    ///     A page limited to one store must not answer a lookup made in another store. The store
    ///     used to be optional here, so a caller that forgot it - the storefront contact page and
    ///     both panel dashboards did - got whichever page matched the system name first.
    /// </summary>
    [TestMethod]
    public async Task GetPageBySystemName_LimitedToAnotherStore_IsNotReturned()
    {
        //Arrange
        var page = new Page { SystemName = "test", LimitedToStores = true, Stores = { "store-1" } };
        await _repository.InsertAsync(page);
        //Act
        var result = await _pageService.GetPageBySystemName(page.SystemName, "store-2");
        //Assert
        Assert.IsNull(result);
    }

    /// <summary>
    ///     A page that is not limited to any store stays visible in every store - the fix narrows
    ///     what leaks, not what existing installations can see.
    /// </summary>
    [TestMethod]
    public async Task GetPageBySystemName_NotLimitedToStores_IsReturnedInAnyStore()
    {
        //Arrange
        var page = new Page { SystemName = "test", LimitedToStores = false };
        await _repository.InsertAsync(page);
        //Act
        var result = await _pageService.GetPageBySystemName(page.SystemName, "store-2");
        //Assert
        Assert.IsNotNull(result);
    }

    /// <summary>
    ///     The store panel overrides a shared page by copying it for one store, which leaves two pages
    ///     carrying one system name visible to that store. The store's own copy is inserted second, so
    ///     ordering by identifier alone handed the store back the shared page and made the copy inert.
    /// </summary>
    [TestMethod]
    public async Task GetPageBySystemName_StoreHasItsOwnCopy_ReturnsTheCopyNotTheSharedPage()
    {
        //Arrange
        var sharedPage = new Page { SystemName = "test", LimitedToStores = false };
        await _repository.InsertAsync(sharedPage);
        var storeCopy = new Page { SystemName = "test", LimitedToStores = true, Stores = { "store-1" } };
        await _repository.InsertAsync(storeCopy);
        //Act
        var result = await _pageService.GetPageBySystemName("test", "store-1");
        //Assert
        Assert.AreEqual(storeCopy.Id, result.Id);
    }

    /// <summary>
    ///     A store without a copy of its own keeps resolving to the shared page.
    /// </summary>
    [TestMethod]
    public async Task GetPageBySystemName_AnotherStoreHasTheCopy_ReturnsTheSharedPage()
    {
        //Arrange
        var sharedPage = new Page { SystemName = "test", LimitedToStores = false };
        await _repository.InsertAsync(sharedPage);
        await _repository.InsertAsync(new Page
            { SystemName = "test", LimitedToStores = true, Stores = { "store-1" } });
        //Act
        var result = await _pageService.GetPageBySystemName("test", "store-2");
        //Assert
        Assert.AreEqual(sharedPage.Id, result.Id);
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
        Assert.AreEqual("test", _repository.Table.FirstOrDefault(x => x.Id == page.Id).SystemName);
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