using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Tax;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.Common.Localization;
using Grand.Web.Vendor.Mapper;
using Grand.Web.Vendor.Models.Catalog;
using Grand.Web.Vendor.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Vendor.Tests.Services;

// Characterization tests for the vendor-specific behavior of this forked ProductViewModelService, ahead
// of the planned consolidation back into Grand.Web.AdminShared.Services.ProductViewModelService (the two
// classes are ~85% identical; the remainder is the tenant-isolation logic covered here). These tests must
// keep passing (or have their expectation deliberately revised in the same change) once the fork is
// removed and the AdminShared implementation is parameterized/extended for the Vendor area instead.
[TestClass]
public class ProductViewModelServiceTests
{
    private const string CurrentVendorId = "vendor-1";
    private const string OtherVendorId = "vendor-2";

    private Mock<IProductService> _productServiceMock;
    private Mock<ISeNameService> _seNameServiceMock;
    private ProductViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<ProductProfile>(); });
        AutoMapperConfig.Init(mapperConfig);

        _productServiceMock = new Mock<IProductService>();
        _seNameServiceMock = new Mock<ISeNameService>();
        _seNameServiceMock
            .Setup(s => s.TranslationSeNameProperties(It.IsAny<IList<ProductLocalizedModel>>(),
                It.IsAny<Product>(), It.IsAny<System.Linq.Expressions.Expression<Func<ProductLocalizedModel, string>>>()))
            .ReturnsAsync(new List<Domain.Localization.TranslationEntity>());
        _seNameServiceMock
            .Setup(s => s.ValidateSeName(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync("se-name");

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentVendor).Returns(new Grand.Domain.Vendors.Vendor { Id = CurrentVendorId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        _service = new ProductViewModelService(
            _productServiceMock.Object,
            new Mock<IInventoryManageService>().Object,
            new Mock<IPictureService>().Object,
            new Mock<IProductAttributeService>().Object,
            new Mock<ICurrencyService>().Object,
            new Mock<IMeasureService>().Object,
            new Mock<IDateTimeService>().Object,
            new Mock<ICollectionService>().Object,
            new Mock<IProductCollectionService>().Object,
            new Mock<ICategoryService>().Object,
            new Mock<IProductCategoryService>().Object,
            translationServiceMock.Object,
            new Mock<IProductLayoutService>().Object,
            new Mock<ISpecificationAttributeService>().Object,
            contextAccessorMock.Object,
            new Mock<IWarehouseService>().Object,
            new Mock<IDeliveryDateService>().Object,
            new Mock<ITaxCategoryService>().Object,
            new Mock<ICustomerService>().Object,
            new Mock<IStoreService>().Object,
            new Mock<IOutOfStockSubscriptionService>().Object,
            new Mock<ILanguageService>().Object,
            new Mock<IProductAttributeFormatter>().Object,
            new Mock<IStockQuantityService>().Object,
            new Mock<IAuctionService>().Object,
            new Mock<IPriceFormatter>().Object,
            new CurrencySettings(),
            new MeasureSettings(),
            new TaxSettings(),
            _seNameServiceMock.Object,
            new Mock<IEnumTranslationService>().Object);
    }

    [TestMethod]
    public async Task InsertProductModel_SetsVendorIdFromCurrentVendor()
    {
        var model = new ProductModel { Name = "New product" };

        var product = await _service.InsertProductModel(model);

        Assert.AreEqual(CurrentVendorId, product.VendorId);
        _productServiceMock.Verify(p => p.InsertProduct(It.Is<Product>(x => x.VendorId == CurrentVendorId)),
            Times.Once);
    }

    [TestMethod]
    public async Task PrepareProducts_FiltersSearchByCurrentVendor()
    {
        IPagedList<Product> paged = new PagedList<Product> { new() { Id = "p1", VendorId = CurrentVendorId } };
        _productServiceMock.Setup(p => p.SearchProducts(
                false, 0, int.MaxValue, It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), CurrentVendorId, It.IsAny<string>(), It.IsAny<ProductType?>(), false, false,
                It.IsAny<bool?>(), It.IsAny<bool?>(), null, null, "", It.IsAny<string>(), false, true, false, "",
                null, null, ProductSortingEnum.Position, true, It.IsAny<bool?>()))
            .ReturnsAsync((paged, (IList<string>)null));

        var products = await _service.PrepareProducts(new ProductListModel());

        Assert.AreEqual(1, products.Count);
        _productServiceMock.Verify(p => p.SearchProducts(
            false, 0, int.MaxValue, It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), CurrentVendorId, It.IsAny<string>(), It.IsAny<ProductType?>(), false, false,
            It.IsAny<bool?>(), It.IsAny<bool?>(), null, null, "", It.IsAny<string>(), false, true, false, "",
            null, null, ProductSortingEnum.Position, true, It.IsAny<bool?>()), Times.Once);
    }

    [TestMethod]
    public async Task DeleteSelected_SkipsProductsNotOwnedByCurrentVendor()
    {
        var own = new Product { Id = "own", VendorId = CurrentVendorId };
        var other = new Product { Id = "other", VendorId = OtherVendorId };
        _productServiceMock.Setup(p => p.GetProductsByIds(new[] { "own", "other" }, true))
            .ReturnsAsync(new List<Product> { own, other });

        await _service.DeleteSelected(new[] { "own", "other" });

        _productServiceMock.Verify(p => p.DeleteProduct(own), Times.Once);
        _productServiceMock.Verify(p => p.DeleteProduct(other), Times.Never);
    }

    [TestMethod]
    public async Task InsertRelatedProductModel_SkipsCandidateNotOwnedByCurrentVendor()
    {
        var source = new Product { Id = "source", VendorId = CurrentVendorId };
        var other = new Product { Id = "other", VendorId = OtherVendorId };
        _productServiceMock.Setup(p => p.GetProductById("source", true)).ReturnsAsync(source);
        _productServiceMock.Setup(p => p.GetProductById("other", false)).ReturnsAsync(other);

        await _service.InsertRelatedProductModel(new ProductModel.AddRelatedProductModel {
            ProductId = "source",
            SelectedProductIds = ["other"]
        });

        Assert.IsFalse(source.RelatedProducts.Any(x => x.ProductId2 == "other"));
        _productServiceMock.Verify(p => p.InsertRelatedProduct(It.IsAny<RelatedProduct>(), "source"), Times.Never);
    }

    [TestMethod]
    public async Task InsertSimilarProductModel_SkipsCandidateNotOwnedByCurrentVendor()
    {
        // Regression test for a fixed authorization bug: this used to check
        // HasAccessToProduct(productId1) - the product already being edited, which the vendor is
        // guaranteed to own - instead of HasAccessToProduct(product), the candidate being linked in
        // via `id`. That made the check a no-op: any vendor could link any other vendor's product as
        // "similar". Now it checks the candidate, matching InsertRelatedProductModel/
        // InsertBundleProductModel.
        var source = new Product { Id = "source", VendorId = CurrentVendorId };
        var other = new Product { Id = "other", VendorId = OtherVendorId };
        _productServiceMock.Setup(p => p.GetProductById("source", true)).ReturnsAsync(source);
        _productServiceMock.Setup(p => p.GetProductById("other", false)).ReturnsAsync(other);

        await _service.InsertSimilarProductModel(new ProductModel.AddSimilarProductModel {
            ProductId = "source",
            SelectedProductIds = ["other"]
        });

        Assert.IsFalse(source.SimilarProducts.Any(x => x.ProductId2 == "other"));
        _productServiceMock.Verify(p => p.InsertSimilarProduct(It.IsAny<SimilarProduct>()), Times.Never);
    }

    [TestMethod]
    public async Task InsertSimilarProductModel_LinksCandidateOwnedByCurrentVendor()
    {
        var source = new Product { Id = "source", VendorId = CurrentVendorId };
        var own = new Product { Id = "own", VendorId = CurrentVendorId };
        _productServiceMock.Setup(p => p.GetProductById("source", true)).ReturnsAsync(source);
        _productServiceMock.Setup(p => p.GetProductById("own", false)).ReturnsAsync(own);

        await _service.InsertSimilarProductModel(new ProductModel.AddSimilarProductModel {
            ProductId = "source",
            SelectedProductIds = ["own"]
        });

        Assert.IsTrue(source.SimilarProducts.Any(x => x.ProductId2 == "own"));
        _productServiceMock.Verify(p => p.InsertSimilarProduct(It.IsAny<SimilarProduct>()), Times.Once);
    }

    [TestMethod]
    public async Task InsertBundleProductModel_SkipsCandidateNotOwnedByCurrentVendor()
    {
        // Same fixed bug as InsertSimilarProductModel, same fix.
        var source = new Product { Id = "source", VendorId = CurrentVendorId };
        var other = new Product { Id = "other", VendorId = OtherVendorId };
        _productServiceMock.Setup(p => p.GetProductById("source", true)).ReturnsAsync(source);
        _productServiceMock.Setup(p => p.GetProductById("other", false)).ReturnsAsync(other);

        await _service.InsertBundleProductModel(new ProductModel.AddBundleProductModel {
            ProductId = "source",
            SelectedProductIds = ["other"]
        });

        Assert.IsFalse(source.BundleProducts.Any(x => x.ProductId == "other"));
        _productServiceMock.Verify(p => p.InsertBundleProduct(It.IsAny<BundleProduct>(), It.IsAny<string>()),
            Times.Never);
    }
}
