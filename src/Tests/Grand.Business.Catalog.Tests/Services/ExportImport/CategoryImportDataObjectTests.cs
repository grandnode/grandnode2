using AutoMapper;
using Grand.Business.Catalog.Services.Categories;
using Grand.Business.Catalog.Services.ExportImport;
using Grand.Business.Common.Services.Security;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Categories;
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
public class CategoryImportDataObjectTests
{
    private MemoryCacheBase _cacheBase;
    private CategoryImportDataObject _categoryImportDataObject;
    private Mock<ICategoryLayoutService> _categoryLayoutServiceMock;
    private ICategoryService _categoryService;
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IPictureService> _pictureServiceMock;


    private IRepository<Category> _repository;
    private Mock<ISlugService> _slugServiceMock;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        InitAutoMapper();

        _repository = new MongoDBRepositoryTest<Category>();

        _pictureServiceMock = new Mock<IPictureService>();
        _categoryLayoutServiceMock = new Mock<ICategoryLayoutService>();
        _slugServiceMock = new Mock<ISlugService>();
        _languageServiceMock = new Mock<ILanguageService>();
        _workContextMock = new Mock<IWorkContext>();
        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());

        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _categoryService = new CategoryService(_cacheBase, _repository, _workContextMock.Object, _mediatorMock.Object,
            new AclService(new AccessControlConfig()), new AccessControlConfig());

        _categoryImportDataObject = new CategoryImportDataObject(_categoryService, _pictureServiceMock.Object,
            _categoryLayoutServiceMock.Object, _slugServiceMock.Object, _languageServiceMock.Object, new SeoSettings());
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Insert()
    {
        //Arrange
        var categorys = new List<CategoryDto>();
        categorys.Add(new CategoryDto { Name = "test1", Published = true });
        categorys.Add(new CategoryDto { Name = "test2", Published = true });
        categorys.Add(new CategoryDto { Name = "test3", Published = true });
        _categoryLayoutServiceMock.Setup(c => c.GetCategoryLayoutById(It.IsAny<string>()))
            .Returns(Task.FromResult(new CategoryLayout()));
        _categoryLayoutServiceMock.Setup(c => c.GetAllCategoryLayouts())
            .Returns(Task.FromResult<IList<CategoryLayout>>(new List<CategoryLayout> { new() }));
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug" }));
        //Act
        await _categoryImportDataObject.Execute(categorys);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(3, _repository.Table.Count());
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Update()
    {
        //Arrange
        var category1 = new Category {
            Name = "insert1"
        };
        await _categoryService.InsertCategory(category1);
        var category2 = new Category {
            Name = "insert2"
        };
        await _categoryService.InsertCategory(category2);
        var category3 = new Category {
            Name = "insert3"
        };
        await _categoryService.InsertCategory(category3);


        var categorys = new List<CategoryDto>();
        categorys.Add(new CategoryDto { Id = category1.Id, Name = "update1", Published = false, DisplayOrder = 1 });
        categorys.Add(new CategoryDto { Id = category2.Id, Name = "update2", Published = false, DisplayOrder = 2 });
        categorys.Add(new CategoryDto { Id = category3.Id, Name = "update3", Published = false, DisplayOrder = 3 });

        _categoryLayoutServiceMock.Setup(c => c.GetCategoryLayoutById(It.IsAny<string>()))
            .Returns(Task.FromResult(new CategoryLayout()));
        _categoryLayoutServiceMock.Setup(c => c.GetAllCategoryLayouts())
            .Returns(Task.FromResult<IList<CategoryLayout>>(new List<CategoryLayout> { new() }));
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug" }));
        //Act
        await _categoryImportDataObject.Execute(categorys);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(3, _repository.Table.Count());
        Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == category3.Id).Name);
        Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == category3.Id).DisplayOrder);
        Assert.AreEqual(false, _repository.Table.FirstOrDefault(x => x.Id == category3.Id).Published);
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Insert_Update()
    {
        //Arrange
        var category3 = new Category {
            Name = "insert3"
        };
        await _categoryService.InsertCategory(category3);

        var categorys = new List<CategoryDto>();
        categorys.Add(new CategoryDto { Name = "update1", Published = false, DisplayOrder = 1 });
        categorys.Add(new CategoryDto { Name = "update2", Published = false, DisplayOrder = 2 });
        categorys.Add(new CategoryDto { Id = category3.Id, Name = "update3", Published = false, DisplayOrder = 3 });

        _categoryLayoutServiceMock.Setup(c => c.GetCategoryLayoutById(It.IsAny<string>()))
            .Returns(Task.FromResult(new CategoryLayout()));
        _categoryLayoutServiceMock.Setup(c => c.GetAllCategoryLayouts())
            .Returns(Task.FromResult<IList<CategoryLayout>>(new List<CategoryLayout> { new() }));
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug" }));
        //Act
        await _categoryImportDataObject.Execute(categorys);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(3, _repository.Table.Count());
        Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == category3.Id).Name);
        Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == category3.Id).DisplayOrder);
        Assert.AreEqual(false, _repository.Table.FirstOrDefault(x => x.Id == category3.Id).Published);
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