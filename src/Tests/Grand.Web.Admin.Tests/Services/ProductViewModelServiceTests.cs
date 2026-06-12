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
using Grand.Infrastructure;
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
    private Mock<IMeasureService> _measureServiceMock;
    private ProductViewModelService _productViewModelService;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ITaxCategoryService> _taxCategoryServiceMock;
    private Mock<ITranslationService> _translationServiceMock;
    private Mock<IWarehouseService> _warehouseServiceMock;

    [TestInitialize]
    public void Setup()
    {
        _discountServiceMock = new Mock<IDiscountService>();
        _enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        _measureServiceMock = new Mock<IMeasureService>();
        _storeServiceMock = new Mock<IStoreService>();
        _taxCategoryServiceMock = new Mock<ITaxCategoryService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _warehouseServiceMock = new Mock<IWarehouseService>();

        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = StaffStoreId });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        var currencyServiceMock = new Mock<ICurrencyService>();
        currencyServiceMock.Setup(c => c.GetCurrencyById(It.IsAny<string>())).ReturnsAsync((Currency)null);
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

        _enumTranslationServiceMock
            .Setup(e => e.ToSelectList(It.IsAny<ProductType>(), It.IsAny<bool>(), It.IsAny<int[]>()))
            .Returns(new SelectList(Enumerable.Empty<SelectListItem>()));

        _productViewModelService = new ProductViewModelService(
            new Mock<IProductService>().Object,
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
            new Mock<IVendorService>().Object,
            _translationServiceMock.Object,
            productLayoutServiceMock.Object,
            new Mock<ISpecificationAttributeService>().Object,
            contextAccessorMock.Object,
            new Mock<IGroupService>().Object,
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
            _enumTranslationServiceMock.Object);
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
        _warehouseServiceMock
            .Setup(w => w.GetAllWarehouses(StaffStoreId, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Warehouse> { new() { Id = "warehouseId", Name = "Main" } });

        var model = await _productViewModelService.PrepareProductListModel(StaffStoreId);

        _warehouseServiceMock.Verify(w => w.GetAllWarehouses(StaffStoreId, It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
        Assert.IsTrue(model.AvailableWarehouses.Any(x => x.Value == "warehouseId"));
    }

    [TestMethod]
    public async Task PrepareProductListModel_FilterStoresByStoreId()
    {
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store> {
            new() { Id = "store1", Shortcut = "Store 1" },
            new() { Id = "store2", Shortcut = "Store 2" }
        });

        var model = await _productViewModelService.PrepareProductListModel("store1");

        Assert.IsTrue(model.AvailableStores.Any(x => x.Value == "store1"));
        Assert.IsFalse(model.AvailableStores.Any(x => x.Value == "store2"));
    }
}
