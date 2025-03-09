using Grand.Business.Catalog.Services.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Discounts;

[TestClass]
public class DiscountHandlerServiceTests
{
    private readonly Mock<IDiscountService> _discountServiceMock;
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly Mock<IBrandService> _brandServiceMock;
    private readonly Mock<ICollectionService> _collectionServiceMock;
    private readonly Mock<IVendorService> _vendorServiceMock;
    private readonly Mock<IDiscountValidationService> _discountValidationServiceMock;
    private readonly CatalogSettings _catalogSettings;
    private readonly DiscountHandlerService _discountApplicationService;

    public DiscountHandlerServiceTests()
    {
        _discountServiceMock = new Mock<IDiscountService>();
        _categoryServiceMock = new Mock<ICategoryService>();
        _brandServiceMock = new Mock<IBrandService>();
        _collectionServiceMock = new Mock<ICollectionService>();
        _vendorServiceMock = new Mock<IVendorService>();
        _discountValidationServiceMock = new Mock<IDiscountValidationService>();
        _catalogSettings = new CatalogSettings { IgnoreDiscounts = false };

        _discountApplicationService = new DiscountHandlerService(
            _discountServiceMock.Object,
            _categoryServiceMock.Object,
            _brandServiceMock.Object,
            _collectionServiceMock.Object,
            _vendorServiceMock.Object,
            _discountValidationServiceMock.Object,
            _catalogSettings);
    }

    [TestMethod]
    public async Task GetAllowedDiscounts_ShouldReturnEmptyList_WhenIgnoreDiscountsIsTrue()
    {
        // Arrange
        _catalogSettings.IgnoreDiscounts = true;
        var product = new Product();
        var customer = new Customer();
        var store = new Store();
        var currency = new Currency();

        // Act
        var result = await _discountApplicationService.GetAllowedDiscounts(product, customer, store, currency);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetAllowedDiscounts_ShouldReturnDiscounts_WhenDiscountsAreValid()
    {
        // Arrange
        var product = new Product();
        product.AppliedDiscounts.Add("discount1");
        var customer = new Customer();
        var store = new Store();
        var currency = new Currency();

        var discount = new Discount {
            Id = "discount1",
            DiscountTypeId = DiscountType.AssignedToSkus,
            IsCumulative = true,
            MaximumDiscountedQuantity = 10
        };
        var allProductDiscounts = new List<Discount>
   {
        new Discount
        {
            Id = "discount2",
            DiscountTypeId = DiscountType.AssignedToAllProducts,
            IsCumulative = true,
            MaximumDiscountedQuantity = 5
        }
    };
        _discountServiceMock.Setup(x => x.GetDiscountById(It.IsAny<string>())).ReturnsAsync(discount);
        _discountValidationServiceMock.Setup(x => x.ValidateDiscount(It.IsAny<Discount>(), It.IsAny<Customer>(), It.IsAny<Store>(), It.IsAny<Currency>()))
            .ReturnsAsync(new DiscountValidationResult { IsValid = true });
        _discountValidationServiceMock.Setup(x => x.ValidateDiscount(It.IsAny<Discount>(), It.IsAny<Customer>(), It.IsAny<Store>(), It.IsAny<Currency>()))
        .ReturnsAsync(new DiscountValidationResult { IsValid = true });
        _discountServiceMock.Setup(x => x.GetActiveDiscountsByContext(DiscountType.AssignedToAllProducts, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<Discount>());

        // Act
        var result = await _discountApplicationService.GetAllowedDiscounts(product, customer, store, currency);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("discount1", result[0].DiscountId);
    }
}