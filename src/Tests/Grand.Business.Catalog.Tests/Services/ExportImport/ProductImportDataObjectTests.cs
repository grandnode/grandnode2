using Grand.Mapping;
using Grand.Business.Catalog.Services.ExportImport;
using Grand.Business.Catalog.Services.Products;
using Grand.Business.Common.Services.Security;
using Grand.Business.Common.Services.Seo;
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
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Utilities.System;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
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
using Grand.Infrastructure.Security;
using Grand.Infrastructure.Tests.Caching;
using Grand.Infrastructure.TypeSearch;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
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
    private Mock<IContextAccessor> _workContextMock;
    private ISeNameService _seNameService;
    private SecurityConfig _securityConfig;
    private TaxSettings _taxSettings;
    private Mock<ILogger<ProductImportDataObject>> _loggerMock;
    private List<string> _downloadedUrls;
    private Func<string, DownloadedImage> _downloadResult;

    [TestInitialize]
    public void Init()
    {
        InitAutoMapper();
        _securityConfig = new SecurityConfig();
        _taxSettings = new TaxSettings { DefaultTaxCategoryId = "tax-default" };
        _downloadedUrls = new List<string>();
        _downloadResult = _ => new DownloadedImage([1, 2, 3], "image/png");

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

        _workContextMock = new Mock<IContextAccessor>();
        _workContextMock.Setup(c => c.StoreContext.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.WorkContext.CurrentCustomer).Returns(() => new Customer());

        _mediatorMock = new Mock<IMediator>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _productService = new ProductService(_cacheBase, _repository, _workContextMock.Object, _mediatorMock.Object,
            new AclService(new AccessControlConfig()));
        _seNameService = new SeNameService(_slugServiceMock.Object, _languageServiceMock.Object, new SeoSettings());
        _productImportDataObject = new ProductImportDataObject
        (_productService, _pictureServiceMock.Object, _productLayoutServiceMock.Object, _deliveryDateServiceMock.Object,
            _taxServiceMock.Object, _warehouseServiceMock.Object, _measureServiceMock.Object, _slugServiceMock.Object,
            _categoryServiceMock.Object, _productCategoryServiceMock.Object, _brandServiceMock.Object,
            _collectionServiceMock.Object,
            _productCollectionServiceMock.Object,
            _seNameService,
            _securityConfig,
            _taxSettings,
            new ImportHtmlGuard(new HtmlSanitizationService(_securityConfig), _securityConfig),
            (_loggerMock = new Mock<ILogger<ProductImportDataObject>>()).Object,
            url =>
            {
                _downloadedUrls.Add(url);
                return Task.FromResult(_downloadResult(url));
            });
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
        _deliveryDateServiceMock.Setup(c => c.GetAllDeliveryDates(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.FromResult<IPagedList<DeliveryDate>>(new PagedList<DeliveryDate>(new List<DeliveryDate> { new() }, 0, int.MaxValue)));

        _taxServiceMock.Setup(c => c.GetTaxCategoryById(It.IsAny<string>()))
            .Returns(Task.FromResult(new TaxCategory()));
        _taxServiceMock.Setup(c => c.GetAllTaxCategories())
            .Returns(Task.FromResult<IList<TaxCategory>>(new List<TaxCategory> { new() }));

        _warehouseServiceMock.Setup(c => c.GetWarehouseById(It.IsAny<string>()))
            .Returns(Task.FromResult(new Warehouse()));
        _warehouseServiceMock.Setup(c => c.GetAllWarehouses(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.FromResult<IPagedList<Warehouse>>(new PagedList<Warehouse>(new List<Warehouse> { new() }, 0, int.MaxValue)));

        _measureServiceMock.Setup(c => c.GetMeasureUnitById(It.IsAny<string>()))
            .Returns(Task.FromResult(new MeasureUnit()));
        _measureServiceMock.Setup(c => c.GetAllMeasureUnits())
            .Returns(Task.FromResult<IList<MeasureUnit>>(new List<MeasureUnit> { new() }));

        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug", EntityName = "Product" }));

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
        _deliveryDateServiceMock.Setup(c => c.GetAllDeliveryDates(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.FromResult<IPagedList<DeliveryDate>>(new PagedList<DeliveryDate>(new List<DeliveryDate> { new() }, 0, int.MaxValue)));

        _taxServiceMock.Setup(c => c.GetTaxCategoryById(It.IsAny<string>()))
            .Returns(Task.FromResult(new TaxCategory()));
        _taxServiceMock.Setup(c => c.GetAllTaxCategories())
            .Returns(Task.FromResult<IList<TaxCategory>>(new List<TaxCategory> { new() }));

        _warehouseServiceMock.Setup(c => c.GetWarehouseById(It.IsAny<string>()))
            .Returns(Task.FromResult(new Warehouse()));
        _warehouseServiceMock.Setup(c => c.GetAllWarehouses(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.FromResult<IPagedList<Warehouse>>(new PagedList<Warehouse>(new List<Warehouse> { new() }, 0, int.MaxValue)));

        _measureServiceMock.Setup(c => c.GetMeasureUnitById(It.IsAny<string>()))
            .Returns(Task.FromResult(new MeasureUnit()));
        _measureServiceMock.Setup(c => c.GetAllMeasureUnits())
            .Returns(Task.FromResult<IList<MeasureUnit>>(new List<MeasureUnit> { new() }));

        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug", EntityName = "Product" }));
        //Act
        await _productImportDataObject.Execute(products);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(3, _repository.Table.Count());
        Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == product3.Id).Name);
        Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == product3.Id).DisplayOrder);
        Assert.IsFalse(_repository.Table.FirstOrDefault(x => x.Id == product3.Id).Published);
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
        _deliveryDateServiceMock.Setup(c => c.GetAllDeliveryDates(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.FromResult<IPagedList<DeliveryDate>>(new PagedList<DeliveryDate>(new List<DeliveryDate> { new() }, 0, int.MaxValue)));

        _taxServiceMock.Setup(c => c.GetTaxCategoryById(It.IsAny<string>()))
            .Returns(Task.FromResult(new TaxCategory()));
        _taxServiceMock.Setup(c => c.GetAllTaxCategories())
            .Returns(Task.FromResult<IList<TaxCategory>>(new List<TaxCategory> { new() }));

        _warehouseServiceMock.Setup(c => c.GetWarehouseById(It.IsAny<string>()))
            .Returns(Task.FromResult(new Warehouse()));
        _warehouseServiceMock.Setup(c => c.GetAllWarehouses(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.FromResult<IPagedList<Warehouse>>(new PagedList<Warehouse>(new List<Warehouse> { new() }, 0, int.MaxValue)));

        _measureServiceMock.Setup(c => c.GetMeasureUnitById(It.IsAny<string>()))
            .Returns(Task.FromResult(new MeasureUnit()));
        _measureServiceMock.Setup(c => c.GetAllMeasureUnits())
            .Returns(Task.FromResult<IList<MeasureUnit>>(new List<MeasureUnit> { new() }));

        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug", EntityName = "Product" }));
        //Act
        await _productImportDataObject.Execute(products);

        //Assert
        Assert.IsTrue(_repository.Table.Any());
        Assert.AreEqual(3, _repository.Table.Count());
        Assert.AreEqual("update3", _repository.Table.FirstOrDefault(x => x.Id == product3.Id).Name);
        Assert.AreEqual(3, _repository.Table.FirstOrDefault(x => x.Id == product3.Id).DisplayOrder);
        Assert.IsFalse(_repository.Table.FirstOrDefault(x => x.Id == product3.Id).Published);
    }

    private void SetupLookups()
    {
        _productLayoutServiceMock.Setup(c => c.GetProductLayoutById(It.IsAny<string>()))
            .Returns(Task.FromResult(new ProductLayout()));
        _productLayoutServiceMock.Setup(c => c.GetAllProductLayouts())
            .Returns(Task.FromResult<IList<ProductLayout>>(new List<ProductLayout> { new() }));
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.FromResult<IList<Language>>(new List<Language>()));
        _slugServiceMock.Setup(c => c.GetBySlug(It.IsAny<string>()))
            .Returns(Task.FromResult(new EntityUrl { Slug = "slug", EntityName = "Product" }));
        _pictureServiceMock.Setup(c => c.InsertPicture(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Reference>(), It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync(() => new Picture { Id = Guid.NewGuid().ToString() });
    }

    [TestMethod]
    public async Task Import_NewRow_IsCreatedWithId()
    {
        SetupLookups();

        var result = await _productImportDataObject.Import(new List<ProductDto> { new() { Name = "new one", Sku = "S1" } }, false);

        Assert.AreEqual(1, result.Created);
        var row = result.Rows[0];
        Assert.AreEqual(1, row.Row);
        Assert.AreEqual(ImportRowStatus.Created, row.Status);
        Assert.AreEqual("new one", row.Key);
        Assert.AreEqual(row.Id, _repository.Table.Single().Id);
    }

    [TestMethod]
    public async Task Import_ExistingBySku_IsUpdated()
    {
        SetupLookups();
        var existing = new Product { Name = "old", Sku = "SKU-1" };
        await _productService.InsertProduct(existing);

        var result = await _productImportDataObject.Import(
            new List<ProductDto> { new() { Sku = "SKU-1", Name = "renamed" } }, false);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[0].Status);
        Assert.AreEqual(existing.Id, result.Rows[0].Id);
        Assert.AreEqual("renamed", _repository.Table.Single().Name);
    }

    [TestMethod]
    public async Task Import_UnknownId_IsRejected()
    {
        SetupLookups();

        var result = await _productImportDataObject.Import(
            new List<ProductDto> { new() { Id = "0123456789abcdef01234567", Name = "x" } }, false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.IsTrue(result.Rows[0].Errors.Any(e => e.Contains("was not found")));
        Assert.IsFalse(_repository.Table.Any());
    }

    [TestMethod]
    public async Task Execute_UnknownId_StillCreatesProductWithThatId()
    {
        SetupLookups();
        var id = "0123456789abcdef01234567";

        await _productImportDataObject.Execute(new List<ProductDto> { new() { Id = id, Name = "panel" } });

        Assert.AreEqual(id, _repository.Table.Single().Id);
    }

    [TestMethod]
    public async Task Execute_UnknownId_FallsBackToSku()
    {
        SetupLookups();
        var existing = new Product { Name = "old", Sku = "SKU-9" };
        await _productService.InsertProduct(existing);

        await _productImportDataObject.Execute(
            new List<ProductDto> { new() { Id = "0123456789abcdef01234567", Sku = "SKU-9", Name = "by sku" } });

        Assert.AreEqual("by sku", _repository.Table.Single().Name);
    }

    [TestMethod]
    public async Task Import_PartialUpdate_KeepsNameAndDescription()
    {
        SetupLookups();
        var existing = new Product { Name = "keep me", FullDescription = "<p>keep</p>", Sku = "SKU-2", Price = 1 };
        await _productService.InsertProduct(existing);

        var result = await _productImportDataObject.Import(
            new List<ProductDto> { new() { Sku = "SKU-2", Price = 42 } }, false);

        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[0].Status);
        var saved = _repository.Table.Single();
        Assert.AreEqual("keep me", saved.Name);
        Assert.AreEqual("<p>keep</p>", saved.FullDescription);
        Assert.AreEqual(42, saved.Price);
    }

    [TestMethod]
    public async Task Import_NewRowWithoutName_IsRejected()
    {
        SetupLookups();

        var result = await _productImportDataObject.Import(new List<ProductDto> { new() { Sku = "NONAME" } }, false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.IsTrue(result.Rows[0].Errors.Any(e => e.Contains("Name")));
        Assert.IsFalse(_repository.Table.Any());
    }

    [TestMethod]
    public async Task Import_EmptyName_IsRejectedAlsoInDryRun()
    {
        SetupLookups();
        var existing = new Product { Name = "old", Sku = "SKU-3" };
        await _productService.InsertProduct(existing);

        var result = await _productImportDataObject.Import(
            new List<ProductDto> { new() { Sku = "SKU-3", Name = "" } }, true);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.AreEqual("old", _repository.Table.Single().Name);
    }

    [TestMethod]
    public async Task Import_IframeInFullDescription_RejectsRowAndContinues()
    {
        SetupLookups();
        var rows = new List<ProductDto> {
            new() { Name = "bad", FullDescription = "<iframe src=\"https://evil.example\"></iframe>" },
            new() { Name = "good" }
        };

        var result = await _productImportDataObject.Import(rows, false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.IsTrue(result.Rows[0].Errors.Any(e => e.Contains("FullDescription")));
        Assert.AreEqual(ImportRowStatus.Created, result.Rows[1].Status);
        Assert.AreEqual("good", _repository.Table.Single().Name);
    }

    [TestMethod]
    public async Task Execute_ScriptInFullDescription_IsNotImported()
    {
        SetupLookups();

        await _productImportDataObject.Execute(new List<ProductDto> {
            new() { Name = "bad", FullDescription = "<script>alert(1)</script>" }
        });

        Assert.IsFalse(_repository.Table.Any());
    }

    [TestMethod]
    public async Task Import_UnknownCategoryAndCollection_WarnsAndStillSaves()
    {
        SetupLookups();

        var result = await _productImportDataObject.Import(new List<ProductDto> {
            new() { Name = "p", CategoryIds = "nocat", CollectionIds = "nocol" }
        }, false);

        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        Assert.IsTrue(result.Rows[0].Warnings.Any(w => w.Contains("nocat")));
        Assert.IsTrue(result.Rows[0].Warnings.Any(w => w.Contains("nocol")));
        Assert.AreEqual(1, _repository.Table.Count());
    }

    [TestMethod]
    public async Task Import_UnknownBrand_Warns()
    {
        SetupLookups();

        var result = await _productImportDataObject.Import(new List<ProductDto> {
            new() { Name = "p", BrandId = "nobrand" }
        }, false);

        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        Assert.IsTrue(result.Rows[0].Warnings.Any(w => w.Contains("nobrand")));
        Assert.AreEqual("", _repository.Table.Single().BrandId);
    }

    [TestMethod]
    public async Task Import_DryRun_WritesAndDownloadsNothing()
    {
        SetupLookups();
        var existing = new Product { Name = "old", Sku = "SKU-4" };
        await _productService.InsertProduct(existing);

        var result = await _productImportDataObject.Import(new List<ProductDto> {
            new() { Name = "fresh", Picture1 = "https://example.com/a.png" },
            new() { Sku = "SKU-4", Name = "renamed", PictureUrls = ["https://example.com/b.png"] }
        }, true);

        Assert.IsTrue(result.DryRun);
        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[1].Status);
        Assert.AreEqual(1, _repository.Table.Count());
        Assert.AreEqual("old", _repository.Table.Single().Name);
        Assert.AreEqual(0, _downloadedUrls.Count);
        _slugServiceMock.Verify(c => c.SaveSlug(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _pictureServiceMock.Verify(c => c.InsertPicture(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Reference>(), It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Import_PictureUrls_ReplacePicture1To3()
    {
        SetupLookups();

        var result = await _productImportDataObject.Import(new List<ProductDto> {
            new() {
                Name = "p", Picture1 = "https://example.com/old.png",
                PictureUrls = ["https://example.com/x.png", "https://example.com/y.png"]
            }
        }, false);

        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        CollectionAssert.AreEqual(new[] { "https://example.com/x.png", "https://example.com/y.png" }, _downloadedUrls);
    }

    [TestMethod]
    public async Task Import_Picture1To3_AreDownloadedWhenNoPictureUrls()
    {
        SetupLookups();

        await _productImportDataObject.Import(new List<ProductDto> {
            new() { Name = "p", Picture1 = "https://example.com/1.png", Picture3 = "https://example.com/3.png" }
        }, false);

        CollectionAssert.AreEqual(new[] { "https://example.com/1.png", "https://example.com/3.png" }, _downloadedUrls);
    }

    [TestMethod]
    public async Task Import_FailedPictureDownload_Warns()
    {
        SetupLookups();
        _downloadResult = _ => null;

        var result = await _productImportDataObject.Import(new List<ProductDto> {
            new() { Name = "p", Picture1 = "https://example.com/dead.png" }
        }, false);

        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        Assert.IsTrue(result.Rows[0].Warnings.Any(w => w.Contains("dead.png")));
    }

    [TestMethod]
    public async Task Import_PictureCap_StopsDownloadingAfter20AndWarns()
    {
        SetupLookups();
        var rows = new List<ProductDto>();
        for (var i = 0; i < 5; i++)
            rows.Add(new ProductDto {
                Name = $"p{i}",
                //a Sku keeps the "no Sku" warning out of the picture warnings checked below
                Sku = $"S{i}",
                PictureUrls = Enumerable.Range(0, 5).Select(n => $"https://example.com/{i}-{n}.png").ToList()
            });

        var result = await _productImportDataObject.Import(rows, false);

        Assert.AreEqual(20, _downloadedUrls.Count);
        Assert.IsFalse(result.Rows[3].Warnings.Any());
        Assert.AreEqual(5, result.Rows[4].Warnings.Count(w => w.Contains("picture limit for one batch reached")));
        Assert.AreEqual(5, result.Created);
    }

    [TestMethod]
    public async Task Execute_MoreThan20Pictures_AreAllDownloaded()
    {
        SetupLookups();
        var rows = new List<ProductDto>();
        for (var i = 0; i < 5; i++)
            rows.Add(new ProductDto {
                Name = $"p{i}",
                PictureUrls = Enumerable.Range(0, 5).Select(n => $"https://example.com/{i}-{n}.png").ToList()
            });

        await _productImportDataObject.Execute(rows);

        Assert.AreEqual(25, _downloadedUrls.Count);
    }

    [TestMethod]
    public async Task Import_MarkupInPlainFields_RejectsRow()
    {
        SetupLookups();

        var result = await _productImportDataObject.Import(new List<ProductDto> {
            new() { Name = "<b>bold</b>" },
            new() { Name = "ok", Sku = "<img src=x onerror=alert(1)>" }
        }, false);

        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[0].Status);
        Assert.IsTrue(result.Rows[0].Errors.Any(e => e.Contains("Name")));
        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[1].Status);
        Assert.IsTrue(result.Rows[1].Errors.Any(e => e.Contains("Sku")));
        Assert.IsFalse(_repository.Table.Any());
    }

    [TestMethod]
    public async Task Import_EmptyName_GivesOneError()
    {
        SetupLookups();

        var result = await _productImportDataObject.Import(new List<ProductDto> { new() { Name = "" } }, false);

        Assert.AreEqual(1, result.Rows[0].Errors.Count);
    }

    [TestMethod]
    public async Task Import_StoredNameEmptyAndNoNameSent_RejectedInDryRunAndRealRun()
    {
        SetupLookups();
        await _productService.InsertProduct(new Product { Name = "", Sku = "SKU-E" });
        var rows = new List<ProductDto> { new() { Sku = "SKU-E", Price = 5 } };

        var dry = await _productImportDataObject.Import(rows, true);
        var real = await _productImportDataObject.Import(rows, false);

        Assert.AreEqual(ImportRowStatus.Rejected, dry.Rows[0].Status);
        Assert.AreEqual(ImportRowStatus.Rejected, real.Rows[0].Status);
    }

    [TestMethod]
    public async Task Import_CancelledAfterFirstRow_RestAreRejectedAsNotProcessed()
    {
        SetupLookups();
        using var cts = new CancellationTokenSource();
        //the layout lookup runs while row 1 is saved
        _productLayoutServiceMock.Setup(c => c.GetAllProductLayouts())
            .Callback(() => cts.Cancel())
            .Returns(Task.FromResult<IList<ProductLayout>>(new List<ProductLayout> { new() }));

        var result = await _productImportDataObject.Import(new List<ProductDto> {
            new() { Name = "one", Sku = "S1" }, new() { Name = "two", Sku = "S2" }, new() { Name = "three", Sku = "S3" }
        }, false, cts.Token);

        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[1].Status);
        Assert.AreEqual(ImportRowStatus.Rejected, result.Rows[2].Status);
        Assert.AreEqual("three", result.Rows[2].Key);
        Assert.AreEqual("Not processed: the batch time limit was reached; send these rows again.",
            result.Rows[1].Errors.Single());
        Assert.AreEqual("one", _repository.Table.Single().Name);
    }

    [TestMethod]
    public async Task Execute_RejectedRows_LogsOneWarningWithRowNumbersAndReasons()
    {
        SetupLookups();

        await _productImportDataObject.Execute(new List<ProductDto> {
            new() { Name = "good", Sku = "G" },
            new() { Name = "bad", Sku = "B", FullDescription = "<script>alert(1)</script>" }
        });

        Assert.AreEqual("good", _repository.Table.Single().Name);
        _loggerMock.Verify(l => l.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("row 2") && v.ToString()!.Contains("FullDescription")),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Import_NewRowWithoutSku_WarnsAboutDuplicates(bool dryRun)
    {
        SetupLookups();
        var existing = new Product { Name = "old" };
        await _productService.InsertProduct(existing);

        var result = await _productImportDataObject.Import(new List<ProductDto> {
            new() { Name = "no sku" },
            new() { Name = "with sku", Sku = "S1" },
            new() { Id = existing.Id, Name = "renamed" }
        }, dryRun);

        const string warning = "Row has no Sku; sending it again creates a duplicate.";
        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        Assert.Contains(warning, result.Rows[0].Warnings);
        Assert.DoesNotContain(warning, result.Rows[1].Warnings);
        Assert.AreEqual(ImportRowStatus.Updated, result.Rows[2].Status);
        Assert.DoesNotContain(warning, result.Rows[2].Warnings);
    }

    [TestMethod]
    public async Task Import_NewRow_GetsThePanelCreateDefaults()
    {
        SetupLookups();
        _taxServiceMock.Setup(c => c.GetTaxCategoryById("tax-default")).ReturnsAsync(new TaxCategory { Id = "tax-default" });

        await _productImportDataObject.Import(new List<ProductDto> { new() { Name = "buyable", Sku = "B1" } }, false);

        var product = _repository.Table.Single();
        Assert.IsTrue(product.VisibleIndividually);
        Assert.AreEqual(1, product.OrderMinimumQuantity);
        Assert.AreEqual(10000, product.OrderMaximumQuantity);
        Assert.AreEqual(1, product.NotifyAdminForQuantityBelow);
        Assert.IsTrue(product.IsShipEnabled);
        Assert.IsTrue(product.AllowCustomerReviews);
        Assert.AreEqual("tax-default", product.TaxCategoryId);
        Assert.IsFalse(product.Published);
    }

    [TestMethod]
    public async Task Import_NewRow_ExplicitValuesWinOverCreateDefaults()
    {
        SetupLookups();
        _taxServiceMock.Setup(c => c.GetTaxCategoryById("tax-own")).ReturnsAsync(new TaxCategory { Id = "tax-own" });

        await _productImportDataObject.Import(new List<ProductDto> {
            new() {
                Name = "own", Sku = "B2", VisibleIndividually = false, OrderMinimumQuantity = 2,
                OrderMaximumQuantity = 5, NotifyAdminForQuantityBelow = 3, IsShipEnabled = false,
                AllowCustomerReviews = false, TaxCategoryId = "tax-own", Published = true
            }
        }, false);

        var product = _repository.Table.Single();
        Assert.IsFalse(product.VisibleIndividually);
        Assert.AreEqual(2, product.OrderMinimumQuantity);
        Assert.AreEqual(5, product.OrderMaximumQuantity);
        Assert.AreEqual(3, product.NotifyAdminForQuantityBelow);
        Assert.IsFalse(product.IsShipEnabled);
        Assert.IsFalse(product.AllowCustomerReviews);
        Assert.AreEqual("tax-own", product.TaxCategoryId);
        Assert.IsTrue(product.Published);
    }

    [TestMethod]
    public async Task Import_ExistingRow_DoesNotGetCreateDefaults()
    {
        SetupLookups();
        await _productService.InsertProduct(new Product { Name = "old", Sku = "B3" });

        await _productImportDataObject.Import(new List<ProductDto> { new() { Sku = "B3", Name = "renamed" } }, false);

        var product = _repository.Table.Single();
        Assert.IsFalse(product.VisibleIndividually);
        Assert.AreEqual(0, product.OrderMaximumQuantity);
        Assert.IsNull(product.TaxCategoryId);
    }

    [TestMethod]
    public async Task Execute_PanelCreate_DoesNotGetRowCreateDefaults()
    {
        SetupLookups();
        _taxServiceMock.Setup(c => c.GetTaxCategoryById("tax-default")).ReturnsAsync(new TaxCategory { Id = "tax-default" });

        await _productImportDataObject.Execute(new List<ProductDto> { new() { Name = "panel", Sku = "P1" } });

        var product = _repository.Table.Single();
        Assert.IsFalse(product.VisibleIndividually);
        Assert.AreEqual(0, product.OrderMinimumQuantity);
        Assert.AreEqual(0, product.OrderMaximumQuantity);
        Assert.IsFalse(product.IsShipEnabled);
        Assert.IsTrue(string.IsNullOrEmpty(product.TaxCategoryId));
    }

    [TestMethod]
    public async Task Import_DryRun_NewRowWithDefaultsWritesNothing()
    {
        SetupLookups();

        var result = await _productImportDataObject.Import(new List<ProductDto> { new() { Name = "dry", Sku = "D1" } }, true);

        Assert.AreEqual(ImportRowStatus.Created, result.Rows[0].Status);
        Assert.AreEqual("", result.Rows[0].Id);
        Assert.IsFalse(_repository.Table.Any());
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