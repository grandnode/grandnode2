using AutoMapper;
using Grand.Business.Catalog.Services.ExportImport;
using Grand.Business.Catalog.Services.Products;
using Grand.Business.Common.Services.Security;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Seo;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
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
public class ProductImportDataObjectTests
{
    private Mock<IBrandService> _brandServiceMock;
    private MemoryCacheBase _cacheBase;
    private Mock<ICategoryService> _categoryServiceMock;
    private Mock<ICollectionService> _collectionServiceMock;

    private Mock<IDeliveryDateService> _deliveryDateServiceMock;
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<IMeasureService> _measureServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IPictureService> _pictureServiceMock;
    private Mock<IProductCategoryService> _productCategoryServiceMock;
    private Mock<IProductCollectionService> _productCollectionServiceMock;
    private ProductImportDataObject _productImportDataObject;
    private Mock<IProductLayoutService> _productLayoutServiceMock;
    private IProductService _productService;

    private IRepository<Product> _repository;
    private Mock<ISlugService> _slugServiceMock;
    private Mock<ITaxCategoryService> _taxServiceMock;
    private Mock<IWarehouseService> _warehouseServiceMock;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        InitAutoMapper();

        _repository = new MongoDBRepositoryTest<Product>();

        _pictureServiceMock = new Mock<IPictureService>();
        _productLayoutServiceMock = new Mock<IProductLayoutService>();
        _slugServiceMock = new Mock<ISlugService>();
        _languageServiceMock = new Mock<ILanguageService>();
        _deliveryDateServiceMock = new Mock<IDeliveryDateService>();
        _brandServiceMock = new Mock<IBrandService>();
        _collectionServiceMock = new Mock<ICollectionService>();
        _taxServiceMock = new Mock<ITaxCategoryService>();
        _categoryServiceMock = new Mock<ICategoryService>();
        _warehouseServiceMock = new Mock<IWarehouseService>();
        _measureServiceMock = new Mock<IMeasureService>();
        _productCategoryServiceMock = new Mock<IProductCategoryService>();
        _productCollectionServiceMock = new Mock<IProductCollectionService>();

        _workContextMock = new Mock<IWorkContext>();
        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());

        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _productService = new ProductService(_cacheBase, _repository, _workContextMock.Object, _mediatorMock.Object,
            new AclService(new AccessControlConfig()));

        _productImportDataObject = new ProductImportDataObject
        (_productService, _pictureServiceMock.Object, _productLayoutServiceMock.Object, _deliveryDateServiceMock.Object,
            _taxServiceMock.Object, _warehouseServiceMock.Object, _measureServiceMock.Object, _slugServiceMock.Object,
            _languageServiceMock.Object,
            _categoryServiceMock.Object, _productCategoryServiceMock.Object, _brandServiceMock.Object,
            _collectionServiceMock.Object,
            _productCollectionServiceMock.Object,
            new SeoSettings());
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Insert()
    {
        //Arrange
        var products = new List<ProductDto>();
        products.Add(new ProductDto { Name = "test1", Published = true });
        products.Add(new ProductDto { Name = "test2", Published = true });
        products.Add(new ProductDto { Name = "test3", Published = true });

        _productLayoutServiceMock.Setup(c => c.GetProductLayoutById(It.IsAny<string>()))
            .Returns(Task.FromResult(new ProductLayout()));
        _productLayoutServiceMock.Setup(c => c.GetAllProductLayouts())
            .Returns(Task.FromResult<IList<ProductLayout>>(new List<ProductLayout> { new() }));

        _deliveryDateServiceMock.Setup(c => c.GetDeliveryDateById(It.IsAny<string>()))
            .Returns(Task.FromResult(new DeliveryDate()));
        _deliveryDateServiceMock.Setup(c => c.GetAllDeliveryDates())
            .Returns(Task.FromResult<IList<DeliveryDate>>(new List<DeliveryDate> { new() }));

        _taxServiceMock.Setup(c => c.GetTaxCategoryById(It.IsAny<string>()))
            .Returns(Task.FromResult(new TaxCategory()));
        _taxServiceMock.Setup(c => c.GetAllTaxCategories())
            .Returns(Task.FromResult<IList<TaxCategory>>(new List<TaxCategory> { new() }));

        _warehouseServiceMock.Setup(c => c.GetWarehouseById(It.IsAny<string>()))
            .Returns(Task.FromResult(new Warehouse()));
        _warehouseServiceMock.Setup(c => c.GetAllWarehouses())
            .Returns(Task.FromResult<IList<Warehouse>>(new List<Warehouse> { new() }));

        _measureServiceMock.Setup(c => c.GetMeasureUnitById(It.IsAny<string>()))
            .Returns(Task.FromResult(new MeasureUnit()));
        _measureServiceMock.Setup(c => c.GetAllMeasureUnits())
            .Returns(Task.FromResult<IList<MeasureUnit>>(new List<MeasureUnit> { new() }));

        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug" }));

        //Act
        await _productImportDataObject.Execute(products);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(3, _repository.Table.Count());
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Update()
    {
        //Arrange
        var product1 = new Product {
            Name = "insert1"
        };
        await _productService.InsertProduct(product1);
        var product2 = new Product {
            Name = "insert2"
        };
        await _productService.InsertProduct(product2);
        var product3 = new Product {
            Name = "insert3"
        };
        await _productService.InsertProduct(product3);


        var products = new List<ProductDto>();
        products.Add(new ProductDto { Id = product1.Id, Name = "update1", Published = false, DisplayOrder = 1 });
        products.Add(new ProductDto { Id = product2.Id, Name = "update2", Published = false, DisplayOrder = 2 });
        products.Add(new ProductDto { Id = product3.Id, Name = "update3", Published = false, DisplayOrder = 3 });

        _productLayoutServiceMock.Setup(c => c.GetProductLayoutById(It.IsAny<string>()))
            .Returns(Task.FromResult(new ProductLayout()));
        _productLayoutServiceMock.Setup(c => c.GetAllProductLayouts())
            .Returns(Task.FromResult<IList<ProductLayout>>(new List<ProductLayout> { new() }));
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));

        _deliveryDateServiceMock.Setup(c => c.GetDeliveryDateById(It.IsAny<string>()))
            .Returns(Task.FromResult(new DeliveryDate()));
        _deliveryDateServiceMock.Setup(c => c.GetAllDeliveryDates())
            .Returns(Task.FromResult<IList<DeliveryDate>>(new List<DeliveryDate> { new() }));

        _taxServiceMock.Setup(c => c.GetTaxCategoryById(It.IsAny<string>()))
            .Returns(Task.FromResult(new TaxCategory()));
        _taxServiceMock.Setup(c => c.GetAllTaxCategories())
            .Returns(Task.FromResult<IList<TaxCategory>>(new List<TaxCategory> { new() }));

        _warehouseServiceMock.Setup(c => c.GetWarehouseById(It.IsAny<string>()))
            .Returns(Task.FromResult(new Warehouse()));
        _warehouseServiceMock.Setup(c => c.GetAllWarehouses())
            .Returns(Task.FromResult<IList<Warehouse>>(new List<Warehouse> { new() }));

        _measureServiceMock.Setup(c => c.GetMeasureUnitById(It.IsAny<string>()))
            .Returns(Task.FromResult(new MeasureUnit()));
        _measureServiceMock.Setup(c => c.GetAllMeasureUnits())
            .Returns(Task.FromResult<IList<MeasureUnit>>(new List<MeasureUnit> { new() }));

        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug" }));
        //Act
        await _productImportDataObject.Execute(products);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(3, _repository.Table.Count());
        Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == product3.Id).Name);
        Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == product3.Id).DisplayOrder);
        Assert.AreEqual(false, _repository.Table.FirstOrDefault(x => x.Id == product3.Id).Published);
    }

    [TestMethod]
    public async Task ExecuteTest_Import_Insert_Update()
    {
        //Arrange
        var product3 = new Product {
            Name = "insert3"
        };
        await _productService.InsertProduct(product3);

        var products = new List<ProductDto>();
        products.Add(new ProductDto { Name = "update1", Published = false, DisplayOrder = 1 });
        products.Add(new ProductDto { Name = "update2", Published = false, DisplayOrder = 2 });
        products.Add(new ProductDto { Id = product3.Id, Name = "update3", Published = false, DisplayOrder = 3 });

        _productLayoutServiceMock.Setup(c => c.GetProductLayoutById(It.IsAny<string>()))
            .Returns(Task.FromResult(new ProductLayout()));
        _productLayoutServiceMock.Setup(c => c.GetAllProductLayouts())
            .Returns(Task.FromResult<IList<ProductLayout>>(new List<ProductLayout> { new() }));
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));

        _deliveryDateServiceMock.Setup(c => c.GetDeliveryDateById(It.IsAny<string>()))
            .Returns(Task.FromResult(new DeliveryDate()));
        _deliveryDateServiceMock.Setup(c => c.GetAllDeliveryDates())
            .Returns(Task.FromResult<IList<DeliveryDate>>(new List<DeliveryDate> { new() }));

        _taxServiceMock.Setup(c => c.GetTaxCategoryById(It.IsAny<string>()))
            .Returns(Task.FromResult(new TaxCategory()));
        _taxServiceMock.Setup(c => c.GetAllTaxCategories())
            .Returns(Task.FromResult<IList<TaxCategory>>(new List<TaxCategory> { new() }));

        _warehouseServiceMock.Setup(c => c.GetWarehouseById(It.IsAny<string>()))
            .Returns(Task.FromResult(new Warehouse()));
        _warehouseServiceMock.Setup(c => c.GetAllWarehouses())
            .Returns(Task.FromResult<IList<Warehouse>>(new List<Warehouse> { new() }));

        _measureServiceMock.Setup(c => c.GetMeasureUnitById(It.IsAny<string>()))
            .Returns(Task.FromResult(new MeasureUnit()));
        _measureServiceMock.Setup(c => c.GetAllMeasureUnits())
            .Returns(Task.FromResult<IList<MeasureUnit>>(new List<MeasureUnit> { new() }));

        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug" }));
        //Act
        await _productImportDataObject.Execute(products);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(3, _repository.Table.Count());
        Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == product3.Id).Name);
        Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == product3.Id).DisplayOrder);
        Assert.AreEqual(false, _repository.Table.FirstOrDefault(x => x.Id == product3.Id).Published);
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