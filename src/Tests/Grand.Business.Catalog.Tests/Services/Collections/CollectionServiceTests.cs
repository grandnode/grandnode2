using Grand.Business.Catalog.Services.Collections;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Collections;

[TestClass]
public class CollectionServiceTests
{
    private Mock<IAclService> _aclMock;
    private Mock<ICacheBase> _cacheMock;
    private ICollectionService _collectionService;
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<Collection>> _repositoryMock;
    private CatalogSettings _settings;

    [TestInitialize]
    public void Init()
    {
        _cacheMock = new Mock<ICacheBase>();
        _repositoryMock = new Mock<IRepository<Collection>>();
        _mediatorMock = new Mock<IMediator>();
        _aclMock = new Mock<IAclService>();
        _settings = new CatalogSettings();
        _collectionService = new CollectionService(_cacheMock.Object, _repositoryMock.Object, _mediatorMock.Object, _aclMock.Object, new AccessControlConfig());
    }


    [TestMethod]
    public async Task GetAllCollectionFeaturedProductsOnHomePage_AuthorizesAgainstGivenCustomerAndStore()
    {
        //Arrange
        var collection = new Collection { Id = "a", Published = true, FeaturedProductsOnHomePage = true };
        var customer = new Customer();
        _repositoryMock.Setup(r => r.Table).Returns(new List<Collection> { collection }.AsQueryable());
        _repositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Collection>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Collection> q, CancellationToken _) => q.ToList());
        _aclMock.Setup(a => a.Authorize(collection, customer)).Returns(true);
        _aclMock.Setup(a => a.Authorize(collection, "store1")).Returns(true);

        //Act
        var inStore1 = await _collectionService.GetAllCollectionFeaturedProductsOnHomePage(customer, "store1");
        var inStore2 = await _collectionService.GetAllCollectionFeaturedProductsOnHomePage(customer, "store2");

        //Assert
        Assert.HasCount(1, inStore1);
        Assert.IsEmpty(inStore2);
    }

    [TestMethod]
    public async Task DeleteCollection_ValidArguments_InvokeMethods()
    {
        await _collectionService.DeleteCollection(new Collection());
        _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<Collection>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<Collection>>(), default), Times.Once);
    }


    [TestMethod]
    public async Task InsertCollection_ValidArguments_InvokeMethods()
    {
        await _collectionService.InsertCollection(new Collection());
        _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<Collection>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Collection>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task UpdateCollection_ValidArguments_InvokeMethods()
    {
        await _collectionService.UpdateCollection(new Collection());
        _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Collection>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Collection>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task GeCollectionById_ValidArgument_GetByCache()
    {
        await _collectionService.GetCollectionById("id");
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<Collection>>>()), Times.Once);
    }

    [TestMethod]
    public void DeleteCollection_NullArgument_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _collectionService.DeleteCollection(null));
    }

    [TestMethod]
    public void InsertCollection_NullArgument_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _collectionService.InsertCollection(null));
    }

    [TestMethod]
    public void UpdateCollection_NullArgument_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _collectionService.UpdateCollection(null));
    }
}