using Grand.Business.Catalog.Services.Products;
using Grand.Business.Checkout.Validators;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Validators;

[TestClass]
public class ShoppingCartInventoryProductValidatorTests
{
    private static readonly Language Language = new() { Id = "lang1" };
    private Mock<IProductService> _productServiceMock;
    private ShoppingCartInventoryProductValidator _validator;

    [TestInitialize]
    public void Init()
    {
        //the resource name comes back as the message, so a test can tell the warnings apart
        var translationService = new Mock<ITranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>())).Returns((string name, string _, string _, bool _) => name);
        _productServiceMock = new Mock<IProductService>();
        _validator = new ShoppingCartInventoryProductValidator(translationService.Object, _productServiceMock.Object,
            new StockQuantityService(), new ShoppingCartSettings());
    }

    private async Task<IList<string>> Validate(Product product, int quantity, Customer customer = null,
        IList<CustomAttribute> attributes = null)
    {
        product.OrderMinimumQuantity = 1;
        product.OrderMaximumQuantity = 100;
        var result = await _validator.ValidateAsync(new ShoppingCartInventoryProductValidatorRecord(
            customer ?? new Customer(), product,
            new ShoppingCartItem {
                Id = "line", ProductId = product.Id, Quantity = quantity, Attributes = attributes,
                ShoppingCartTypeId = ShoppingCartType.ShoppingCart
            },
            Language));
        return result.Errors.Select(x => x.ErrorMessage).ToList();
    }

    private static Product WithCombination(int stock, bool allowOutOfStockOrders = false)
    {
        var product = new Product { Id = "p", ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes };
        product.ProductAttributeCombinations.Add(new ProductAttributeCombination {
            StockQuantity = stock, AllowOutOfStockOrders = allowOutOfStockOrders
        });
        return product;
    }

    private Product Bundle(Product child, int childQuantity)
    {
        _productServiceMock.Setup(x => x.GetProductById(child.Id, false)).ReturnsAsync(child);
        var bundle = new Product { Id = "b", ManageInventoryMethodId = ManageInventoryMethod.ManageStockByBundleProducts };
        bundle.BundleProducts.Add(new BundleProduct { ProductId = child.Id, Quantity = childQuantity });
        return bundle;
    }

    [TestMethod]
    public async Task ManageStock_QuantityAboveStock_QuantityExceedsStock()
    {
        var errors = await Validate(new Product {
            Id = "p", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 2
        }, 3);

        CollectionAssert.AreEqual(new[] { "ShoppingCart.QuantityExceedsStock" }, errors.ToArray());
    }

    [TestMethod]
    public async Task ManageStock_NothingLeft_OutOfStock()
    {
        var errors = await Validate(new Product {
            Id = "p", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 1,
            ReservedQuantity = 1
        }, 1);

        CollectionAssert.AreEqual(new[] { "ShoppingCart.OutOfStock" }, errors.ToArray());
    }

    [TestMethod]
    public async Task ManageStock_BackordersAllowed_Valid()
    {
        var errors = await Validate(new Product {
            Id = "p", ManageInventoryMethodId = ManageInventoryMethod.ManageStock,
            BackorderModeId = BackorderMode.AllowQtyBelowZero
        }, 5);

        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public async Task ManageStock_OtherCartLinesOfTheProduct_CountToo()
    {
        var customer = new Customer();
        customer.ShoppingCartItems.Add(new ShoppingCartItem {
            Id = "other", ProductId = "p", Quantity = 2, WarehouseId = "",
            ShoppingCartTypeId = ShoppingCartType.ShoppingCart
        });

        var errors = await Validate(new Product {
            Id = "p", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 3
        }, 2, customer);

        CollectionAssert.AreEqual(new[] { "ShoppingCart.QuantityExceedsStock" }, errors.ToArray());
    }

    [TestMethod]
    public async Task ByAttributes_NoCombination_CombinationNotExist()
    {
        var product = new Product { Id = "p", ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes };

        var errors = await Validate(product, 1, attributes: new List<CustomAttribute>());

        CollectionAssert.AreEqual(new[] { "ShoppingCart.Combination.NotExist" }, errors.ToArray());
    }

    [TestMethod]
    public async Task ByAttributes_CombinationEmpty_OutOfStock()
    {
        var errors = await Validate(WithCombination(0), 1, attributes: new List<CustomAttribute>());

        CollectionAssert.AreEqual(new[] { "ShoppingCart.OutOfStock" }, errors.ToArray());
    }

    [TestMethod]
    public async Task ByAttributes_QuantityAboveCombinationStock_QuantityExceedsStock()
    {
        var errors = await Validate(WithCombination(1), 2, attributes: new List<CustomAttribute>());

        CollectionAssert.AreEqual(new[] { "ShoppingCart.QuantityExceedsStock" }, errors.ToArray());
    }

    [TestMethod]
    public async Task ByAttributes_OutOfStockOrdersAllowed_Valid()
    {
        var errors = await Validate(WithCombination(0, true), 2, attributes: new List<CustomAttribute>());

        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public async Task Bundle_ChildShort_OutOfStockBundleProduct()
    {
        var child = new Product {
            Id = "c", Name = "child", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 3
        };

        var errors = await Validate(Bundle(child, 2), 2);

        CollectionAssert.AreEqual(new[] { "ShoppingCart.OutOfStock.BundleProduct" }, errors.ToArray());
    }

    [TestMethod]
    public async Task Bundle_ChildWithBackorders_Valid()
    {
        var child = WithCombination(0);
        child.Id = "c";
        child.BackorderModeId = BackorderMode.AllowQtyBelowZero;

        var errors = await Validate(Bundle(child, 2), 1, attributes: new List<CustomAttribute>());

        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public async Task Bundle_ChildCombinationShort_QuantityExceedsStockBundleProduct()
    {
        var child = WithCombination(3);
        child.Id = "c";

        var errors = await Validate(Bundle(child, 2), 2, attributes: new List<CustomAttribute>());

        CollectionAssert.AreEqual(new[] { "ShoppingCart.QuantityExceedsStock.BundleProduct" }, errors.ToArray());
    }

    [TestMethod]
    public async Task Bundle_ChildWithoutCombination_CombinationNotExist()
    {
        var child = new Product { Id = "c", ManageInventoryMethodId = ManageInventoryMethod.ManageStockByAttributes };

        var errors = await Validate(Bundle(child, 1), 1, attributes: new List<CustomAttribute>());

        CollectionAssert.AreEqual(new[] { "ShoppingCart.Combination.NotExist" }, errors.ToArray());
    }
}
