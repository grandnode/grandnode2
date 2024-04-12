using Grand.Business.Catalog.Services.Collections;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using MediatR;
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
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _cacheMock = new Mock<ICacheBase>();
        _repositoryMock = new Mock<IRepository<Collection>>();
        _workContextMock = new Mock<IWorkContext>();
        _mediatorMock = new Mock<IMediator>();
        _aclMock = new Mock<IAclService>();
        _settings = new CatalogSettings();
        _collectionService = new CollectionService(_cacheMock.Object, _repositoryMock.Object, _workContextMock.Object
            , _mediatorMock.Object, _aclMock.Object, new AccessControlConfig());
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
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _collectionService.DeleteCollection(null));
    }

    [TestMethod]
    public void InsertCollection_NullArgument_ThrowException()
    {
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _collectionService.InsertCollection(null));
    }

    [TestMethod]
    public void UpdateCollection_NullArgument_ThrowException()
    {
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _collectionService.UpdateCollection(null));
    }
}