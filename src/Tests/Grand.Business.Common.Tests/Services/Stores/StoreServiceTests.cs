using Grand.Business.Common.Services.Stores;
using Grand.Data;
using Grand.Domain.Stores;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Stores;

[TestClass]
public class StoreServiceTests
{
    private Mock<ICacheBase> _cacheMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<Store>> _repository;
    private StoreService _service;

    [TestInitialize]
    public void Init()
    {
        _cacheMock = new Mock<ICacheBase>();
        _mediatorMock = new Mock<IMediator>();
        _repository = new Mock<IRepository<Store>>();
        _service = new StoreService(_cacheMock.Object, _repository.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task InsertStore_ValidArgument_InvokeExpectedMethods()
    {
        await _service.InsertStore(new Store());
        _repository.Verify(c => c.InsertAsync(It.IsAny<Store>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Store>>(), default), Times.Once);
        _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task UpdateStore_ValidArgument_InvokeExpectedMethods()
    {
        await _service.UpdateStore(new Store());
        _repository.Verify(c => c.UpdateAsync(It.IsAny<Store>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Store>>(), default), Times.Once);
        _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public async Task DeleteStore_ValidArgument_InvokeExpectedMethods()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Store>>>>()))
            .Returns(Task.FromResult(new List<Store> { new(), new() }));
        await _service.DeleteStore(new Store());
        _repository.Verify(c => c.DeleteAsync(It.IsAny<Store>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<Store>>(), default), Times.Once);
        _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
    }

    [TestMethod]
    public void DeleteStore_OnlyOneStore_ThrowException()
    {
        //can not remove store if it is only one 
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Store>>>>()))
            .Returns(Task.FromResult(new List<Store> { new() }));
        Assert.ThrowsExceptionAsync<Exception>(async () => await _service.DeleteStore(new Store()));
    }

    [TestMethod]
    public async Task GetStoreByHost_MatchingHost_ReturnsMatchingStore()
    {
        // Arrange
        var store1 = new Store { Url = "http://store1.com" };
        store1.Domains = new List<DomainHost>
        {
            new DomainHost { HostName = "store1.com", Url = "http://store1.com", Primary = true }
        };

        var store2 = new Store { Url = "http://store2.com" };
        store2.Domains = new List<DomainHost>
        {
            new DomainHost { HostName = "store2.com", Url = "http://store2.com", Primary = true }
        };

        var stores = new List<Store> { store1, store2 };

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Store>>>>()))
            .Returns(Task.FromResult(stores));

        // Act
        var result = await _service.GetStoreByHost("store1.com");

        // Assert
        Assert.AreEqual(store1, result);
    }

    [TestMethod]
    public async Task GetStoreByHost_NoMatchingHost_ReturnsNullStore()
    {
        // Arrange
        var store1 = new Store { Url = "http://store1.com" };
        store1.Domains = new List<DomainHost>
        {
            new DomainHost { HostName = "store1.com", Url = "http://store1.com", Primary = true }
        };

        var store2 = new Store { Url = "http://store2.com" };
        store2.Domains = new List<DomainHost>
        {
            new DomainHost { HostName = "store2.com", Url = "http://store2.com", Primary = true }
        };

        var stores = new List<Store> { store1, store2 };

        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<Store>>>>()))
            .Returns(Task.FromResult(stores));

        // Act - host not found in any DomainHost
        var result = await _service.GetStoreByHost("nonexisting.com");

        // Assert - returns first store
        Assert.AreEqual(null, result);
    }
}