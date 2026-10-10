using Grand.Mapping;
using Grand.Business.Catalog.Services.Collections;
using Grand.Business.Catalog.Services.ExportImport;
using Grand.Business.Common.Services.Security;
using Grand.Business.Common.Services.Seo;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.ExportImport;
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
using Grand.Infrastructure.Security;
using Grand.Infrastructure.Tests.Caching;
using Grand.Infrastructure.TypeSearch;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
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
    private Mock<IContextAccessor> _workContextMock;
    private ISeNameService _seNameService;
    private SecurityConfig _securityConfig;
    private Mock<ILogger<CollectionImportDataObject>> _loggerMock;
    [TestInitialize]
    public void Init()
    {
        InitAutoMapper();
        _securityConfig = new SecurityConfig();

        _repository = new MongoDBRepositoryTest<Collection>();

        _pictureServiceMock = new Mock<IPictureService>();
        _collectionLayoutServiceMock = new Mock<ICollectionLayoutService>();
        _slugServiceMock = new Mock<ISlugService>();
        _languageServiceMock = new Mock<ILanguageService>();
        _workContextMock = new Mock<IContextAccessor>();
        _workContextMock.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => new Customer());

        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _collectionService = new CollectionService(_cacheBase, _repository, _workContextMock.Object,
            _mediatorMock.Object, new AclService(new AccessControlConfig()), new AccessControlConfig());
        _seNameService = new SeNameService(_slugServiceMock.Object, _languageServiceMock.Object, new SeoSettings());
        _collectionImportDataObject = new CollectionImportDataObject(_collectionService, _pictureServiceMock.Object,
            _collectionLayoutServiceMock.Object, _slugServiceMock.Object, _seNameService,
            _securityConfig, new ImportHtmlGuard(new HtmlSanitizationService(_securityConfig), _securityConfig),
            (_loggerMock = new Mock<ILogger<CollectionImportDataObject>>()).Object);
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
        Assert.IsNotEmpty(_repository.Table);
        Assert.HasCount(3, _repository.Table);
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
        Assert.IsNotEmpty(_repository.Table);
        Assert.HasCount(3, _repository.Table);
        Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == collection3.Id).Name);
        Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == collection3.Id).DisplayOrder);
        Assert.IsFalse(_repository.Table.FirstOrDefault(x => x.Id == collection3.Id).Published);
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
        Assert.IsNotEmpty(_repository.Table);
        Assert.HasCount(3, _repository.Table);
        Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == collection3.Id).Name);
        Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == collection3.Id).DisplayOrder);
        Assert.IsFalse(_repository.Table.FirstOrDefault(x => x.Id == collection3.Id).Published);
    }

    private void SetupLayouts()
    {
        _collectionLayoutServiceMock.Setup(c => c.GetCollectionLayoutById(It.IsAny<string>()))
            .Returns(Task.FromResult(new CollectionLayout()));
        _collectionLayoutServiceMock.Setup(c => c.GetAllCollectionLayouts())
            .Returns(Task.FromResult<IList<CollectionLayout>>(new List<CollectionLayout> { new() }));
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
    }

    [TestMethod]
    public async Task Import_NewRow_IsCreatedWithId()
    {
        SetupLayouts();

        var result = await _collectionImportDataObject.Import(new List<CollectionDto> { new() { Name = "new one" } }, false);

        Assert.AreEqual(1, result.Created);
        var row = result.Rows[0];
        Assert.AreEqual(1, row.Row);
        Assert.AreEqual(ImportRowStatus.Created, row.Status);
        Assert.AreEqual("new one", row.Key);
        Assert.IsFalse(string.IsNullOrEmpty(row.Id));
        Assert.AreEqual(row.Id, _repository.Table.Single().Id);
    }

    [TestMethod]
    public async Task Import_ExistingById_IsUpdated()
    {
        SetupLayouts();
        var existing = new Collection { Name = "old" };
        await _collectionService.InsertCollection(existing);

        var result = await _collectionImportDataObject.Import(
            new List<CollectionDto> { new() { Id = existing.Id, Name = "renamed" } }, false);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[0].Status);
        Assert.AreEqual(existing.Id, result.Rows[0].Id);
        Assert.AreEqual("renamed", _repository.Table.Single().Name);
    }

    [TestMethod]
    public async Task Import_ExistingBySeName_IsUpdatedWithSameId()
    {
        SetupLayouts();
        var existing = new Collection { Name = "old", SeName = "my-slug" };
        await _collectionService.InsertCollection(existing);
        _slugServiceMock.Setup(c => c.GetBySlug("my-slug"))
            .Returns(Task.FromResult(new EntityUrl { EntityId = existing.Id, EntityName = "Collection", Slug = "my-slug" }));

        var result = await _collectionImportDataObject.Import(
            new List<CollectionDto> { new() { SeName = "my-slug", Name = "renamed" } }, false);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[0].Status);
        Assert.AreEqual(existing.Id, result.Rows[0].Id);
        Assert.AreEqual(1, _repository.Table.Count());
        Assert.AreEqual("renamed", _repository.Table.Single().Name);
    }

    [TestMethod]
    public async Task Import_SeNameOfAnotherEntityType_IsNotMatched()
    {
        SetupLayouts();
        _slugServiceMock.Setup(c => c.GetBySlug("my-slug"))
            .Returns(Task.FromResult(new EntityUrl { EntityId = "x", EntityName = "Product", Slug = "my-slug" }));

        var result = await _collectionImportDataObject.Import(
            new List<CollectionDto> { new() { SeName = "my-slug" } }, true);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
    }

    [TestMethod]
    public async Task Import_NewRowWithoutName_IsRejected()
    {
        SetupLayouts();

        var result = await _collectionImportDataObject.Import(new List<CollectionDto> { new() { Published = true } }, false);

        Assert.AreEqual(1, result.Rejected);
        Assert.IsTrue(result.Rows[0].Errors.Any(e => e.Contains("Name")));
        Assert.IsEmpty(_repository.Table);
    }

    [TestMethod]
    public async Task Import_ScriptInDescription_RejectsRowNamingFieldAndContinues()
    {
        SetupLayouts();
        var rows = new List<CollectionDto> {
            new() { Name = "bad", Description = "<p>hi</p><script>alert(1)</script>" },
            new() { Name = "good", Description = "<p>fine</p>" }
        };

        var result = await _collectionImportDataObject.Import(rows, false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.IsTrue(result.Rows[0].Errors.Any(e => e.Contains("Description")));
        Assert.AreEqual(ImportRowStatus.Created, result.Rows[1].Status);
        Assert.AreEqual(2, result.Rows[1].Row);
        Assert.AreEqual("good", _repository.Table.Single().Name);
    }

    [TestMethod]
    public async Task Import_MarkupInName_IsRejected()
    {
        SetupLayouts();

        var result = await _collectionImportDataObject.Import(
            new List<CollectionDto> { new() { Name = "<b>bold</b>" } }, false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.IsTrue(result.Rows[0].Errors.Any(e => e.Contains("Name")));
        Assert.IsEmpty(_repository.Table);
    }

    [TestMethod]
    public async Task Import_SanitizationDisabled_AcceptsMarkup()
    {
        SetupLayouts();
        _securityConfig.EnableHtmlSanitization = false;

        var result = await _collectionImportDataObject.Import(
            new List<CollectionDto> { new() { Name = "x", Description = "<script>alert(1)</script>" } }, false);

        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
    }

    [TestMethod]
    public async Task Import_DryRun_ReportsStatusesWithoutWriting()
    {
        SetupLayouts();
        var existing = new Collection { Name = "old" };
        await _collectionService.InsertCollection(existing);
        var rows = new List<CollectionDto> {
            new() { Name = "brand new", Picture = "http://127.0.0.1:1/pic.png" },
            new() { Id = existing.Id, Name = "renamed", Picture = "http://127.0.0.1:1/pic.png" },
            new() { Published = true }
        };

        var result = await _collectionImportDataObject.Import(rows, true);

        Assert.IsTrue(result.DryRun);
        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[1].Status);
        Assert.AreEqual(existing.Id, result.Rows[1].Id);
        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[2].Status);
        Assert.AreEqual(1, _repository.Table.Count());
        Assert.AreEqual("old", _repository.Table.Single().Name);
        _slugServiceMock.Verify(c => c.SaveSlug(It.IsAny<Collection>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _pictureServiceMock.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Import_UnknownId_IsRejected(bool dryRun)
    {
        SetupLayouts();

        var result = await _collectionImportDataObject.Import(
            new List<CollectionDto> { new() { Id = "nope", Name = "x" } }, dryRun);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.Contains("Id 'nope' was not found.", result.Rows[0].Errors);
        Assert.IsEmpty(_repository.Table);
    }

    [TestMethod]
    public async Task Execute_UnknownId_CreatesEntityWithThatId()
    {
        SetupLayouts();
        var id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

        await _collectionImportDataObject.Execute(new List<CollectionDto> { new() { Id = id, Name = "x" } });

        Assert.AreEqual(id, _repository.Table.Single().Id);
    }

    [TestMethod]
    public async Task Execute_NoIdButExistingSeName_CreatesNewEntity()
    {
        SetupLayouts();
        var existing = new Collection { Name = "old", SeName = "my-slug" };
        await _collectionService.InsertCollection(existing);
        _slugServiceMock.Setup(c => c.GetBySlug("my-slug"))
            .Returns(Task.FromResult(new EntityUrl { EntityId = existing.Id, EntityName = "Collection", Slug = "my-slug" }));

        await _collectionImportDataObject.Execute(new List<CollectionDto> { new() { SeName = "my-slug", Name = "other" } });

        Assert.AreEqual(2, _repository.Table.Count());
        Assert.AreEqual("old", _repository.Table.Single(x => x.Id == existing.Id).Name);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Import_EmptyNameOnExisting_IsRejected(bool dryRun)
    {
        SetupLayouts();
        var existing = new Collection { Name = "old" };
        await _collectionService.InsertCollection(existing);

        var result = await _collectionImportDataObject.Import(
            new List<CollectionDto> { new() { Id = existing.Id, Name = "" } }, dryRun);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.Contains("Name cannot be empty.", result.Rows[0].Errors);
        Assert.AreEqual("old", _repository.Table.Single().Name);
    }

    [TestMethod]
    public async Task Import_CancelledAfterFirstRow_RestAreRejectedAsNotProcessed()
    {
        SetupLayouts();
        using var cts = new CancellationTokenSource();
        //the layout lookup runs while row 1 is saved
        _collectionLayoutServiceMock.Setup(c => c.GetAllCollectionLayouts())
            .Callback(() => cts.Cancel())
            .Returns(Task.FromResult<IList<CollectionLayout>>(new List<CollectionLayout> { new() }));

        var result = await _collectionImportDataObject.Import(
            new List<CollectionDto> { new() { Name = "one" }, new() { Name = "two" }, new() { Name = "three" } }, false,
            cts.Token);

        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[1].Status);
        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[2].Status);
        Assert.AreEqual("three", result.Rows[2].Key);
        Assert.AreEqual("Not processed: the batch time limit was reached; send these rows again.",
            result.Rows[2].Errors.Single());
        Assert.AreEqual("one", _repository.Table.Single().Name);
    }

    [TestMethod]
    public async Task Execute_RejectedRows_LogsOneWarningWithRowNumbersAndReasons()
    {
        SetupLayouts();

        await _collectionImportDataObject.Execute(new List<CollectionDto> {
            new() { Name = "good" },
            new() { Name = "bad", Description = "<script>alert(1)</script>" }
        });

        Assert.AreEqual("good", _repository.Table.Single().Name);
        _loggerMock.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("row 2") && v.ToString()!.Contains("Description")),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Import_NameOnlyResent_MatchesExistingByName(bool dryRun)
    {
        SetupLayouts();
        var existing = new Collection { Name = "Acme Co", SeName = "acme-co" };
        await _collectionService.InsertCollection(existing);
        _slugServiceMock.Setup(c => c.GetBySlug("acme-co"))
            .ReturnsAsync(new EntityUrl { EntityId = existing.Id, EntityName = "Collection", Slug = "acme-co" });

        var result = await _collectionImportDataObject.Import(
            new List<CollectionDto> { new() { Name = "ACME CO", DisplayOrder = 7 } }, dryRun);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[0].Status);
        Assert.AreEqual(existing.Id, result.Rows[0].Id);
        Assert.Contains("Matched existing 'Acme Co' by name.", result.Rows[0].Warnings);
        Assert.AreEqual(1, _repository.Table.Count());
        Assert.AreEqual(dryRun ? 0 : 7, _repository.Table.Single().DisplayOrder);
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
            foreach (var instance in instances) cfg.AddProfile((Grand.Mapping.Profile)instance);
        });

        //register automapper
        AutoMapperConfig.Init(config);
    }
}