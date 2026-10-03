using Grand.Business.Catalog.Events.Handlers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Events.Handlers;

[TestClass]
public class BrandDeletedEventHandlerTests
{
    private IRepository<EntityUrl> _entityUrlRepository;
    private BrandDeletedEventHandler _handler;
    private IRepository<Product> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _entityUrlRepository = new MongoDBRepositoryTest<EntityUrl>();
        var cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), new Mock<IMediator>().Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _handler = new BrandDeletedEventHandler(_entityUrlRepository, _repository, cacheBase);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var brand = new Brand();
        var product = new Product {
            BrandId = brand.Id
        };
        await _repository.InsertAsync(product);
        var product2 = new Product {
            BrandId = brand.Id
        };
        await _repository.InsertAsync(product2);
        var product3 = new Product {
            BrandId = "1"
        };
        await _repository.InsertAsync(product3);

        //Act
        await _handler.Handle(new EntityDeleted<Brand>(brand), CancellationToken.None);
        //Assert
        Assert.AreEqual(0, _repository.Table.Where(x => x.BrandId == brand.Id).Count());
        Assert.AreEqual(1, _repository.Table.Where(x => x.BrandId == "1").Count());
    }

    [TestMethod]
    public async Task Handle_DeletesEveryUrlOfTheBrand_KeepsOtherEntitiesUrls()
    {
        var brand = new Brand();
        await _entityUrlRepository.InsertAsync(new EntityUrl { EntityId = brand.Id, EntityName = EntityTypes.Brand, Slug = "brand-en" });
        await _entityUrlRepository.InsertAsync(new EntityUrl { EntityId = brand.Id, EntityName = EntityTypes.Brand, Slug = "brand-pl", LanguageId = "pl" });
        //same id under another entity type and another brand's url must survive
        await _entityUrlRepository.InsertAsync(new EntityUrl { EntityId = brand.Id, EntityName = EntityTypes.Category, Slug = "category" });
        await _entityUrlRepository.InsertAsync(new EntityUrl { EntityId = "other", EntityName = EntityTypes.Brand, Slug = "other-brand" });

        await _handler.Handle(new EntityDeleted<Brand>(brand), CancellationToken.None);

        Assert.AreEqual(0, _entityUrlRepository.Table.Count(x => x.EntityId == brand.Id && x.EntityName == EntityTypes.Brand));
        Assert.AreEqual(2, _entityUrlRepository.Table.Count());
    }
}
