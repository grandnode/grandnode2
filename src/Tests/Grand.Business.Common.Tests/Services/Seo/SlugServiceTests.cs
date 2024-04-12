using Grand.Business.Common.Services.Seo;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Seo;

[TestClass]
public class SlugServiceTests
{
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private IRepository<EntityUrl> _repository;

    private SlugService _slugService;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<EntityUrl>();

        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _slugService = new SlugService(_cacheBase, _repository);
    }

    [TestMethod]
    public async Task GetEntityUrlByIdTest()
    {
        //Arrange
        var entity = new EntityUrl { Slug = "slug" };
        await _repository.InsertAsync(entity);
        //Act
        var result = _slugService.GetEntityUrlById(entity.Id);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task InsertEntityUrlTest()
    {
        //Arrange
        var entity = new EntityUrl { Slug = "slug" };
        //Act
        await _slugService.InsertEntityUrl(entity);
        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }

    [TestMethod]
    public async Task UpdateEntityUrlTest()
    {
        //Arrange
        var entity = new EntityUrl { Slug = "slug" };
        await _slugService.InsertEntityUrl(entity);
        //Act
        entity.Slug = "slug2";
        await _slugService.UpdateEntityUrl(entity);
        //Assert
        Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == entity.Id).Slug == entity.Slug);
    }

    [TestMethod]
    public async Task DeleteEntityUrlTest()
    {
        //Arrange
        var entity = new EntityUrl { Slug = "slug" };
        await _slugService.InsertEntityUrl(entity);
        //Act
        await _slugService.DeleteEntityUrl(entity);
        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Id == entity.Id));
    }

    [TestMethod]
    public async Task GetBySlugTest()
    {
        //Arrange
        var entity = new EntityUrl { Slug = "slug" };
        await _slugService.InsertEntityUrl(entity);
        //Act
        var result = await _slugService.GetBySlug(entity.Slug);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetBySlugCachedTest()
    {
        //Arrange
        var entity = new EntityUrl { Slug = "slug" };
        await _slugService.InsertEntityUrl(entity);
        //Act
        var result = await _slugService.GetBySlugCached(entity.Slug);
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetAllEntityUrlTest()
    {
        //Arrange
        await _slugService.InsertEntityUrl(new EntityUrl());
        await _slugService.InsertEntityUrl(new EntityUrl());
        await _slugService.InsertEntityUrl(new EntityUrl());
        //Act
        var result = await _slugService.GetAllEntityUrl();
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task GetActiveSlugTest()
    {
        //Arrange
        await _slugService.InsertEntityUrl(new EntityUrl { IsActive = true });
        //Act
        var result = await _slugService.GetAllEntityUrl();
        //Assert
        Assert.IsTrue(result.Any());
    }

    [TestMethod]
    public async Task SaveSlugTest()
    {
        //Act
        await _slugService.SaveSlug(new Category(), "slug", "1");
        //Assert
        Assert.IsTrue(_repository.Table.Any());
    }
}