using Grand.Business.Catalog.Events.Handlers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Events.Handlers;

[TestClass]
public class CollectionDeletedEventHandlerTests
{
    private MemoryCacheBase _cacheBase;
    private IRepository<EntityUrl> _entityUrlRepository;

    private CollectionDeletedEventHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Product> _repository;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _repository = new MongoDBRepositoryTest<Product>();
        _entityUrlRepository = new MongoDBRepositoryTest<EntityUrl>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _handler = new CollectionDeletedEventHandler(_entityUrlRepository, _repository, _cacheBase);
    }


    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var collection = new Collection();
        var product = new Product();
        product.ProductCollections.Add(new ProductCollection { CollectionId = collection.Id });
        await _repository.InsertAsync(product);
        var product2 = new Product();
        product2.ProductCollections.Add(new ProductCollection { CollectionId = collection.Id });
        await _repository.InsertAsync(product2);
        var product3 = new Product();
        product3.ProductCollections.Add(new ProductCollection { CollectionId = "1" });
        await _repository.InsertAsync(product3);

        //Act
        await _handler.Handle(new EntityDeleted<Collection>(collection), CancellationToken.None);
        //Assert
        Assert.AreEqual(0,
            _repository.Table.Where(x => x.ProductCollections.Any(y => y.CollectionId == collection.Id)).Count());
        Assert.AreEqual(1, _repository.Table.Where(x => x.ProductCollections.Any(y => y.CollectionId == "1")).Count());
    }
}