using AutoMapper;
using Grand.Business.Catalog.Services.Collections;
using Grand.Business.Catalog.Services.ExportImport;
using Grand.Business.Common.Services.Security;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Seo;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Mapper;
using Grand.Infrastructure.Tests.Caching;
using Grand.Infrastructure.TypeSearch;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.ExportImport;

[TestClass]
public class CollectionImportDataObjectTests
{
    private MemoryCacheBase _cacheBase;
    private CollectionImportDataObject _collectionImportDataObject;
    private Mock<ICollectionLayoutService> _collectionLayoutServiceMock;
    private ICollectionService _collectionService;
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IPictureService> _pictureServiceMock;


    private IRepository<Collection> _repository;
    private Mock<ISlugService> _slugServiceMock;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        InitAutoMapper();

        _repository = new MongoDBRepositoryTest<Collection>();

        _pictureServiceMock = new Mock<IPictureService>();
        _collectionLayoutServiceMock = new Mock<ICollectionLayoutService>();
        _slugServiceMock = new Mock<ISlugService>();
        _languageServiceMock = new Mock<ILanguageService>();
        _workContextMock = new Mock<IWorkContext>();
        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());

        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _collectionService = new CollectionService(_cacheBase, _repository, _workContextMock.Object,
            _mediatorMock.Object, new AclService(new AccessControlConfig()), new AccessControlConfig());

        _collectionImportDataObject = new CollectionImportDataObject(_collectionService, _pictureServiceMock.Object,
            _collectionLayoutServiceMock.Object, _slugServiceMock.Object, _languageServiceMock.Object,
            new SeoSettings());
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Insert()
    {
        //Arrange
        var collections = new List<CollectionDto>();
        collections.Add(new CollectionDto { Name = "test1", Published = true });
        collections.Add(new CollectionDto { Name = "test2", Published = true });
        collections.Add(new CollectionDto { Name = "test3", Published = true });
        _collectionLayoutServiceMock.Setup(c => c.GetCollectionLayoutById(It.IsAny<string>()))
            .Returns(Task.FromResult(new CollectionLayout()));
        _collectionLayoutServiceMock.Setup(c => c.GetAllCollectionLayouts())
            .Returns(Task.FromResult<IList<CollectionLayout>>(new List<CollectionLayout> { new() }));
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug" }));
        //Act
        await _collectionImportDataObject.Execute(collections);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(3, _repository.Table.Count());
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Update()
    {
        //Arrange
        var collection1 = new Collection {
            Name = "insert1"
        };
        await _collectionService.InsertCollection(collection1);
        var collection2 = new Collection {
            Name = "insert2"
        };
        await _collectionService.InsertCollection(collection2);
        var collection3 = new Collection {
            Name = "insert3"
        };
        await _collectionService.InsertCollection(collection3);


        var collections = new List<CollectionDto>();
        collections.Add(
            new CollectionDto { Id = collection1.Id, Name = "update1", Published = false, DisplayOrder = 1 });
        collections.Add(
            new CollectionDto { Id = collection2.Id, Name = "update2", Published = false, DisplayOrder = 2 });
        collections.Add(
            new CollectionDto { Id = collection3.Id, Name = "update3", Published = false, DisplayOrder = 3 });

        _collectionLayoutServiceMock.Setup(c => c.GetCollectionLayoutById(It.IsAny<string>()))
            .Returns(Task.FromResult(new CollectionLayout()));
        _collectionLayoutServiceMock.Setup(c => c.GetAllCollectionLayouts())
            .Returns(Task.FromResult<IList<CollectionLayout>>(new List<CollectionLayout> { new() }));
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug" }));
        //Act
        await _collectionImportDataObject.Execute(collections);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(3, _repository.Table.Count());
        Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == collection3.Id).Name);
        Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == collection3.Id).DisplayOrder);
        Assert.AreEqual(false, _repository.Table.FirstOrDefault(x => x.Id == collection3.Id).Published);
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Insert_Update()
    {
        //Arrange
        var collection3 = new Collection {
            Name = "insert3"
        };
        await _collectionService.InsertCollection(collection3);

        var collections = new List<CollectionDto>();
        collections.Add(new CollectionDto { Name = "update1", Published = false, DisplayOrder = 1 });
        collections.Add(new CollectionDto { Name = "update2", Published = false, DisplayOrder = 2 });
        collections.Add(
            new CollectionDto { Id = collection3.Id, Name = "update3", Published = false, DisplayOrder = 3 });

        _collectionLayoutServiceMock.Setup(c => c.GetCollectionLayoutById(It.IsAny<string>()))
            .Returns(Task.FromResult(new CollectionLayout()));
        _collectionLayoutServiceMock.Setup(c => c.GetAllCollectionLayouts())
            .Returns(Task.FromResult<IList<CollectionLayout>>(new List<CollectionLayout> { new() }));
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug" }));
        //Act
        await _collectionImportDataObject.Execute(collections);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(3, _repository.Table.Count());
        Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == collection3.Id).Name);
        Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == collection3.Id).DisplayOrder);
        Assert.AreEqual(false, _repository.Table.FirstOrDefault(x => x.Id == collection3.Id).Published);
    }

    private void InitAutoMapper()
    {
        var typeSearcher = new TypeSearcher();
        //find mapper configurations provided by other assemblies
        var mapperConfigurations = typeSearcher.ClassesOfType<IAutoMapperProfile>();

        //create and sort instances of mapper configurations
        var instances = mapperConfigurations
            .Select(mapperConfiguration => (IAutoMapperProfile)Activator.CreateInstance(mapperConfiguration))
            .OrderBy(mapperConfiguration => mapperConfiguration.Order);

        //create AutoMapper configuration
        var config = new MapperConfiguration(cfg =>
        {
            foreach (var instance in instances) cfg.AddProfile(instance.GetType());
        });

        //register automapper
        AutoMapperConfig.Init(config);
    }
}