using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
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
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Media;
using Grand.Domain.Seo;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Infrastructure.Mapper;
using Grand.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class ProductViewModelServiceTests
{
    private const string StaffStoreId = "staffStoreId";

    private Mock<IDiscountService> _discountServiceMock;
    private Mock<IEnumTranslationService> _enumTranslationServiceMock;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<IMeasureService> _measureServiceMock;
    private Mock<IProductService> _productServiceMock;
    private ProductViewModelService _productViewModelService;
    private Mock<IAdminDataScope<Product>> _scopeMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ITaxCategoryService> _taxCategoryServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IVendorService> _vendorServiceMock;
    private Mock<IWarehouseService> _warehouseServiceMock;

    [TestInitialize]
    public void Setup()
    {
        var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile<ProductProfile>(); });
        AutoMapperConfig.Init(mapperConfig);

        _discountServiceMock = new Mock<IDiscountService>();
        _enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        _groupServiceMock = new Mock<IGroupService>();
        _measureServiceMock = new Mock<IMeasureService>();
        _productServiceMock = new Mock<IProductService>();
        _storeServiceMock = new Mock<IStoreService>();
        _taxCategoryServiceMock = new Mock<ITaxCategoryService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _vendorServiceMock = new Mock<IVendorService>();
        _warehouseServiceMock = new Mock<IWarehouseService>();

        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        var currencyServiceMock = new Mock<ICurrencyService>();
        currencyServiceMock.Setup(c => c.GetCurrencyById(It.IsAny<string>())).ReturnsAsync((Currency)null);
        currencyServiceMock.Setup(c => c.GetAllCurrencies(It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(new List<Currency>());
        _measureServiceMock.Setup(m => m.GetMeasureWeightById(It.IsAny<string>())).ReturnsAsync((MeasureWeight)null);
        _measureServiceMock.Setup(m => m.GetMeasureDimensionById(It.IsAny<string>()))
            .ReturnsAsync((MeasureDimension)null);
        _measureServiceMock.Setup(m => m.GetAllMeasureWeights()).ReturnsAsync(new List<MeasureWeight>());
        _measureServiceMock.Setup(m => m.GetAllMeasureUnits()).ReturnsAsync(new List<MeasureUnit>());

        var productLayoutServiceMock = new Mock<IProductLayoutService>();
        productLayoutServiceMock.Setup(p => p.GetAllProductLayouts()).ReturnsAsync(new List<ProductLayout>());

        var deliveryDateServiceMock = new Mock<IDeliveryDateService>();
        deliveryDateServiceMock
            .Setup(d => d.GetAllDeliveryDates(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<DeliveryDate>());

        _warehouseServiceMock
            .Setup(w => w.GetAllWarehouses(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Warehouse>());

        _taxCategoryServiceMock.Setup(t => t.GetAllTaxCategories(It.IsAny<string>()))
            .ReturnsAsync(new List<TaxCategory>());

        _discountServiceMock.Setup(d => d.GetDiscountsQuery(It.IsAny<DiscountType?>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Discount>());

        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());

        _groupServiceMock.Setup(g => g.GetAllCustomerGroups(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<CustomerGroup>());

        _enumTranslationServiceMock
            .Setup(e => e.ToSelectList(It.IsAny<ProductType>(), It.IsAny<bool>(), It.IsAny<int[]>()))
            .Returns(new SelectList(Enumerable.Empty<SelectListItem>()));

        // Default: Admin's Global scope - no default store/vendor, homepage option and store dropdown both show.
        _scopeMock = new Mock<IAdminDataScope<Product>>();
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Admin");
        _scopeMock.Setup(s => s.ShowStoreSelector).Returns(true);
        _scopeMock.Setup(s => s.DefaultVendorId).Returns((string)null);

        _productViewModelService = new ProductViewModelService(
            _productServiceMock.Object,
            new Mock<IInventoryManageService>().Object,
            new Mock<IPictureService>().Object,
            new Mock<IProductAttributeService>().Object,
            new Mock<IProductTagService>().Object,
            currencyServiceMock.Object,
            _measureServiceMock.Object,
            new Mock<IDateTimeService>().Object,
            new Mock<ICollectionService>().Object,
            new Mock<IProductCollectionService>().Object,
            new Mock<ICategoryService>().Object,
            new Mock<IProductCategoryService>().Object,
            _vendorServiceMock.Object,
            _translationServiceMock.Object,
            productLayoutServiceMock.Object,
            new Mock<ISpecificationAttributeService>().Object,
            contextAccessorMock.Object,
            _groupServiceMock.Object,
            _warehouseServiceMock.Object,
            deliveryDateServiceMock.Object,
            _taxCategoryServiceMock.Object,
            _discountServiceMock.Object,
            new Mock<ICustomerService>().Object,
            _storeServiceMock.Object,
            new Mock<IOutOfStockSubscriptionService>().Object,
            new Mock<IDownloadService>().Object,
            new Mock<ILanguageService>().Object,
            new Mock<IProductAttributeFormatter>().Object,
            new Mock<IStockQuantityService>().Object,
            new CurrencySettings(),
            new MeasureSettings(),
            new TaxSettings(),
            new SeoSettings(),
            new Mock<IAuctionService>().Object,
            new Mock<IPriceFormatter>().Object,
            new Mock<ISeNameService>().Object,
            _enumTranslationServiceMock.Object,
            _scopeMock.Object);
    }

    [TestMethod]
    public async Task PrepareProductModel_UseStaffStoreIdForTaxCategories()
    {
        _taxCategoryServiceMock.Setup(t => t.GetAllTaxCategories(StaffStoreId))
            .ReturnsAsync(new List<TaxCategory> { new() { Id = "taxId", Name = "Standard" } });

        var model = new ProductModel();
        await _productViewModelService.PrepareProductModel(model, null, false, false);

        _taxCategoryServiceMock.Verify(t => t.GetAllTaxCategories(StaffStoreId), Times.Once);
        Assert.IsTrue(model.AvailableTaxCategories.Any(x => x.Value == "taxId"));
    }

    [TestMethod]
    public async Task PrepareProductModel_UseStaffStoreIdForDiscounts()
    {
        var model = new ProductModel();
        await _productViewModelService.PrepareProductModel(model, null, false, false);

        _discountServiceMock.Verify(d => d.GetDiscountsQuery(DiscountType.AssignedToSkus, StaffStoreId,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task PrepareProductModel_UseModelStoreIdForWarehouses()
    {
        _warehouseServiceMock
            .Setup(w => w.GetAllWarehouses("modelStoreId", It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Warehouse> { new() { Id = "warehouseId", Name = "Main" } });

        var model = new ProductModel { StoreId = "modelStoreId" };
        await _productViewModelService.PrepareProductModel(model, null, false, false);

        _warehouseServiceMock.Verify(w => w.GetAllWarehouses("modelStoreId", It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
        Assert.IsTrue(model.AvailableWarehouses.Any(x => x.Value == "warehouseId"));
    }

    [TestMethod]
    public async Task PrepareProductListModel_UseStoreIdForWarehouses()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns(StaffStoreId);
        _warehouseServiceMock
            .Setup(w => w.GetAllWarehouses(StaffStoreId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Warehouse> { new() { Id = "warehouseId", Name = "Main" } });

        var model = await _productViewModelService.PrepareProductListModel();

        _warehouseServiceMock.Verify(w => w.GetAllWarehouses(StaffStoreId, It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
        Assert.IsTrue(model.AvailableWarehouses.Any(x => x.Value == "warehouseId"));
    }

    [TestMethod]
    public async Task PrepareProductListModel_FilterStoresByStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store1");
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store> {
            new() { Id = "store1", Shortcut = "Store 1" },
            new() { Id = "store2", Shortcut = "Store 2" }
        });

        var model = await _productViewModelService.PrepareProductListModel();

        Assert.IsTrue(model.AvailableStores.Any(x => x.Value == "store1"));
        Assert.IsFalse(model.AvailableStores.Any(x => x.Value == "store2"));
    }

    [TestMethod]
    public async Task PrepareProductListModel_GlobalScope_IncludesHomepageOptionAndStoreDropdown()
    {
        // Default Setup() scope: Admin's Global scope (DefaultStoreId null, ShowStoreSelector true).
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store> {
            new() { Id = "store1", Shortcut = "Store 1" }
        });

        var model = await _productViewModelService.PrepareProductListModel();

        Assert.IsTrue(model.AvailablePublishedOptions.Any(x => x.Value == "3"),
            "Admin should offer the 'Show on homepage' option.");
        Assert.IsTrue(model.AvailableStores.Any(x => x.Value == "store1"),
            "Admin should offer a store dropdown.");
    }

    [TestMethod]
    public async Task PrepareProductListModel_VendorScope_HidesHomepageOptionAndStoreDropdown()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");
        _scopeMock.Setup(s => s.ShowStoreSelector).Returns(false);
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store> {
            new() { Id = "store1", Shortcut = "Store 1" }
        });

        var model = await _productViewModelService.PrepareProductListModel();

        Assert.IsFalse(model.AvailablePublishedOptions.Any(x => x.Value == "3"),
            "Vendor can't feature products on the homepage.");
        Assert.IsFalse(model.AvailableStores.Any(),
            "Vendor doesn't pick stores - no dropdown should be populated at all.");
    }

    [TestMethod]
    public async Task PrepareTierPriceModel_VendorScope_HidesStoreDropdown()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Vendor");
        _scopeMock.Setup(s => s.ShowStoreSelector).Returns(false);
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store> {
            new() { Id = "store1", Shortcut = "Store 1" }
        });

        var model = new ProductModel.TierPriceModel();
        await _productViewModelService.PrepareTierPriceModel(model);

        Assert.IsFalse(model.AvailableStores.Any(),
            "Vendor doesn't pick stores - no dropdown should be populated at all, matching the original Vendor service's PrepareTierPriceModel.");
    }

    [TestMethod]
    public async Task PrepareTierPriceModel_GlobalScope_IncludesStoreDropdown()
    {
        // Default Setup() scope: Admin's Global scope (DefaultStoreId null, ShowStoreSelector true).
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store> {
            new() { Id = "store1", Shortcut = "Store 1" }
        });

        var model = new ProductModel.TierPriceModel();
        await _productViewModelService.PrepareTierPriceModel(model);

        Assert.IsTrue(model.AvailableStores.Any(x => x.Value == "store1"),
            "Admin should offer a store dropdown for tier prices.");
    }

    // --- Vendor-scoped product search (ARCH-001 Phase 1 Task 10) ------------------------------------

    private void SetupSearchProducts()
    {
        _productServiceMock.Setup(p => p.SearchProducts(
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IList<string>>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ProductType?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<IList<string>>(),
                It.IsAny<ProductSortingEnum>(), It.IsAny<bool>(), It.IsAny<bool?>()))
            .ReturnsAsync((new PagedList<Product>(), new List<string>()));
    }

    private void VerifySearchProductsCalledWithVendorId(string expectedVendorId)
    {
        _productServiceMock.Verify(p => p.SearchProducts(
            It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IList<string>>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), expectedVendorId, It.IsAny<string>(),
            It.IsAny<ProductType?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
            It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<IList<string>>(), It.IsAny<IList<string>>(),
            It.IsAny<ProductSortingEnum>(), It.IsAny<bool>(), It.IsAny<bool?>()), Times.Once);
    }

    [TestMethod]
    public async Task PrepareBulkEditProductModel_VendorScope_ForcesVendorFilter()
    {
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor1");
        SetupSearchProducts();

        await _productViewModelService.PrepareBulkEditProductModel(new BulkEditListModel(), 1, 10);

        VerifySearchProductsCalledWithVendorId("vendor1");
    }

    [TestMethod]
    public async Task PrepareBulkEditProductModel_GlobalScope_DoesNotFilterByVendor()
    {
        // Default Setup() scope: Admin's Global scope (DefaultVendorId null).
        SetupSearchProducts();

        await _productViewModelService.PrepareBulkEditProductModel(new BulkEditListModel(), 1, 10);

        VerifySearchProductsCalledWithVendorId("");
    }

    [TestMethod]
    public async Task PrepareProductsModel_VendorScope_OverridesModelVendorId()
    {
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor1");
        SetupSearchProducts();

        var model = new ProductListModel { SearchVendorId = "vendor2" };
        await _productViewModelService.PrepareProductsModel(model, 1, 10);

        VerifySearchProductsCalledWithVendorId("vendor1");
    }

    [TestMethod]
    public async Task PrepareProductsModel_GlobalScope_UsesModelVendorId()
    {
        // Default Setup() scope: Admin's Global scope (DefaultVendorId null).
        SetupSearchProducts();

        var model = new ProductListModel { SearchVendorId = "vendor2" };
        await _productViewModelService.PrepareProductsModel(model, 1, 10);

        VerifySearchProductsCalledWithVendorId("vendor2");
    }

    [TestMethod]
    public async Task PrepareProducts_VendorScope_OverridesModelVendorId()
    {
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor1");
        SetupSearchProducts();

        var model = new ProductListModel { SearchVendorId = "vendor2" };
        await _productViewModelService.PrepareProducts(model);

        VerifySearchProductsCalledWithVendorId("vendor1");
    }

    [TestMethod]
    public async Task PrepareProductModel_AddProductModel_VendorScope_OverridesModelVendorId()
    {
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor1");
        SetupSearchProducts();

        var model = new ProductModel.AddRelatedProductModel { SearchVendorId = "vendor2" };
        await _productViewModelService.PrepareProductModel(model, 1, 10);

        VerifySearchProductsCalledWithVendorId("vendor1");
    }

    [TestMethod]
    public async Task PrepareRelatedProductModel_VendorScope_HidesVendorDropdown()
    {
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor1");

        var model = await _productViewModelService.PrepareRelatedProductModel();

        Assert.IsFalse(model.AvailableVendors.Any(),
            "Vendor's original PrepareAddProductModel<T> never populated a vendor picker - the search is always forced to the current vendor.");
        _vendorServiceMock.Verify(v => v.GetAllVendors(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);
    }

    [TestMethod]
    public async Task PrepareRelatedProductModel_GlobalScope_IncludesVendorDropdown()
    {
        // Default Setup() scope: Admin's Global scope (DefaultVendorId null).
        _vendorServiceMock.Setup(v => v.GetAllVendors(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), true))
            .ReturnsAsync(new PagedList<Vendor> { new() { Id = "vendor1", Name = "Vendor 1" } });

        var model = await _productViewModelService.PrepareRelatedProductModel();

        Assert.IsTrue(model.AvailableVendors.Any(x => x.Value == "vendor1"),
            "Admin/Store should keep offering a vendor picker.");
    }

    [TestMethod]
    public async Task InsertProductModel_VendorScope_ForcesVendorId()
    {
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor1");
        var model = new ProductModel { Name = "Test product" };

        var product = await _productViewModelService.InsertProductModel(model);

        Assert.AreEqual("vendor1", product.VendorId,
            "Vendor's original InsertProductModel always set product.VendorId = CurrentVendor.Id - without " +
            "this, a vendor-created product would map whatever (usually blank) VendorId the model carries " +
            "and end up orphaned, inaccessible to the vendor who just created it.");
    }

    [TestMethod]
    public async Task InsertProductModel_GlobalScope_DoesNotForceVendorId()
    {
        // Default Setup() scope: Admin's Global scope (DefaultVendorId null).
        var model = new ProductModel { Name = "Test product", VendorId = "vendor2" };

        var product = await _productViewModelService.InsertProductModel(model);

        Assert.AreEqual("vendor2", product.VendorId,
            "Admin/Store should keep whatever vendor ownership the create form assigns.");
    }

    [TestMethod]
    public async Task UpdateProductModel_VendorScope_ForcesVendorId_PreventsCrossVendorReassignment()
    {
        _scopeMock.Setup(s => s.DefaultVendorId).Returns("vendor1");
        var product = new Product { Id = "p1", VendorId = "vendor1" };
        var model = new ProductModel { Id = "p1", VendorId = "vendor-attacker-supplied" };

        var result = await _productViewModelService.UpdateProductModel(product, model);

        Assert.AreEqual("vendor1", result.VendorId,
            "AdminShared's shared ProductProfile does not ignore ProductModel.VendorId -> Product.VendorId " +
            "(Admin's edit form legitimately reassigns vendor ownership); without re-forcing it after mapping, " +
            "a vendor could reassign their own product to a different vendor id via a tampered VendorId field.");
    }

    [TestMethod]
    public async Task UpdateProductModel_GlobalScope_UsesModelVendorId()
    {
        // Default Setup() scope: Admin's Global scope (DefaultVendorId null).
        var product = new Product { Id = "p1", VendorId = "vendor1" };
        var model = new ProductModel { Id = "p1", VendorId = "vendor2" };

        var result = await _productViewModelService.UpdateProductModel(product, model);

        Assert.AreEqual("vendor2", result.VendorId,
            "Admin should keep being able to reassign a product's vendor ownership via the edit form.");
    }

    // --- Per-id ownership filter on Insert*ProductModel (ARCH-001 Phase 1 Task 11) ------------------
    // Mirrors Vendor's original InsertRelatedProductModel ("if (product == null ||
    // !HasAccessToProduct(product)) continue;") and the pattern already used for the selected-ids loop
    // in BaseProductController.AssociatedProductAddPopup(POST) (Task 8). This gap was inert while Vendor
    // wasn't yet subclassed onto BaseProductController; wiring Vendor in (Task 11) makes it live.

    private void SetupOwnershipForInsertTests(Product ownedProduct, Product notOwnedProduct)
    {
        _productServiceMock.Setup(p => p.GetProductById(ownedProduct.Id, It.IsAny<bool>())).ReturnsAsync(ownedProduct);
        _productServiceMock.Setup(p => p.GetProductById(notOwnedProduct.Id, It.IsAny<bool>())).ReturnsAsync(notOwnedProduct);
        _scopeMock.Setup(s => s.HasAccess(ownedProduct)).ReturnsAsync(true);
        _scopeMock.Setup(s => s.HasAccess(notOwnedProduct)).ReturnsAsync(false);
    }

    [TestMethod]
    public async Task InsertRelatedProductModel_DropsNotOwnedSelectedId()
    {
        var mainProduct = new Product { Id = "main" };
        var owned = new Product { Id = "owned" };
        var notOwned = new Product { Id = "not-owned" };
        _productServiceMock.Setup(p => p.GetProductById("main", It.IsAny<bool>())).ReturnsAsync(mainProduct);
        SetupOwnershipForInsertTests(owned, notOwned);

        var model = new ProductModel.AddRelatedProductModel {
            ProductId = "main",
            SelectedProductIds = ["owned", "not-owned"]
        };
        await _productViewModelService.InsertRelatedProductModel(model);

        Assert.IsTrue(mainProduct.RelatedProducts.Any(x => x.ProductId2 == "owned"),
            "The owned selected product should be inserted.");
        Assert.IsFalse(mainProduct.RelatedProducts.Any(x => x.ProductId2 == "not-owned"),
            "A not-owned selected product must be silently dropped, not inserted.");
        _productServiceMock.Verify(p => p.InsertRelatedProduct(It.Is<RelatedProduct>(r => r.ProductId2 == "not-owned"), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task InsertSimilarProductModel_DropsNotOwnedSelectedId()
    {
        var mainProduct = new Product { Id = "main" };
        var owned = new Product { Id = "owned" };
        var notOwned = new Product { Id = "not-owned" };
        _productServiceMock.Setup(p => p.GetProductById("main", It.IsAny<bool>())).ReturnsAsync(mainProduct);
        SetupOwnershipForInsertTests(owned, notOwned);

        var model = new ProductModel.AddSimilarProductModel {
            ProductId = "main",
            SelectedProductIds = ["owned", "not-owned"]
        };
        await _productViewModelService.InsertSimilarProductModel(model);

        Assert.IsTrue(mainProduct.SimilarProducts.Any(x => x.ProductId2 == "owned"),
            "The owned selected product should be inserted.");
        Assert.IsFalse(mainProduct.SimilarProducts.Any(x => x.ProductId2 == "not-owned"),
            "A not-owned selected product must be silently dropped, not inserted.");
        _productServiceMock.Verify(p => p.InsertSimilarProduct(It.Is<SimilarProduct>(r => r.ProductId2 == "not-owned")), Times.Never);
    }

    [TestMethod]
    public async Task InsertBundleProductModel_DropsNotOwnedSelectedId()
    {
        var mainProduct = new Product { Id = "main" };
        var owned = new Product { Id = "owned" };
        var notOwned = new Product { Id = "not-owned" };
        _productServiceMock.Setup(p => p.GetProductById("main", It.IsAny<bool>())).ReturnsAsync(mainProduct);
        SetupOwnershipForInsertTests(owned, notOwned);

        var model = new ProductModel.AddBundleProductModel {
            ProductId = "main",
            SelectedProductIds = ["owned", "not-owned"]
        };
        await _productViewModelService.InsertBundleProductModel(model);

        Assert.IsTrue(mainProduct.BundleProducts.Any(x => x.ProductId == "owned"),
            "The owned selected product should be inserted.");
        Assert.IsFalse(mainProduct.BundleProducts.Any(x => x.ProductId == "not-owned"),
            "A not-owned selected product must be silently dropped, not inserted.");
        _productServiceMock.Verify(p => p.InsertBundleProduct(It.Is<BundleProduct>(r => r.ProductId == "not-owned"), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task InsertCrossSellProductModel_DropsNotOwnedSelectedId()
    {
        var mainProduct = new Product { Id = "main" };
        var owned = new Product { Id = "owned" };
        var notOwned = new Product { Id = "not-owned" };
        _productServiceMock.Setup(p => p.GetProductById("main", It.IsAny<bool>())).ReturnsAsync(mainProduct);
        SetupOwnershipForInsertTests(owned, notOwned);

        var model = new ProductModel.AddCrossSellProductModel {
            ProductId = "main",
            SelectedProductIds = ["owned", "not-owned"]
        };
        await _productViewModelService.InsertCrossSellProductModel(model);

        _productServiceMock.Verify(p => p.InsertCrossSellProduct(It.Is<CrossSellProduct>(r => r.ProductId2 == "owned")), Times.Once);
        _productServiceMock.Verify(p => p.InsertCrossSellProduct(It.Is<CrossSellProduct>(r => r.ProductId2 == "not-owned")), Times.Never,
            "A not-owned selected product must be silently dropped, not inserted.");
    }

    [TestMethod]
    public async Task InsertRecommendedProductModel_DropsNotOwnedSelectedId()
    {
        var mainProduct = new Product { Id = "main" };
        var owned = new Product { Id = "owned" };
        var notOwned = new Product { Id = "not-owned" };
        _productServiceMock.Setup(p => p.GetProductById("main", It.IsAny<bool>())).ReturnsAsync(mainProduct);
        SetupOwnershipForInsertTests(owned, notOwned);

        var model = new ProductModel.AddRecommendedProductModel {
            ProductId = "main",
            SelectedProductIds = ["owned", "not-owned"]
        };
        await _productViewModelService.InsertRecommendedProductModel(model);

        _productServiceMock.Verify(p => p.InsertRecommendedProduct("main", "owned"), Times.Once);
        _productServiceMock.Verify(p => p.InsertRecommendedProduct("main", "not-owned"), Times.Never,
            "A not-owned selected product must be silently dropped, not inserted.");
    }
}
