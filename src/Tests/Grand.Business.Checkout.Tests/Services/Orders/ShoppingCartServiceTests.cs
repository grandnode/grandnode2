using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class ShoppingCartServiceTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IProductService> _productServiceMock;
    private ShoppingCartService _shoppingCartService;
    private ShoppingCartSettings _shoppingCartSettings;
    private Mock<IShoppingCartValidator> _shoppingCartValidatorMock;

    [TestInitialize]
    public void Init()
    {
        _productServiceMock = new Mock<IProductService>();
        _shoppingCartSettings = new ShoppingCartSettings();
        _customerServiceMock = new Mock<ICustomerService>();
        _mediatorMock = new Mock<IMediator>();
        _shoppingCartValidatorMock = new Mock<IShoppingCartValidator>();

        _shoppingCartService = new ShoppingCartService(_productServiceMock.Object,
            _customerServiceMock.Object, _mediatorMock.Object, _shoppingCartValidatorMock.Object, _shoppingCartSettings);
    }

    [TestMethod]
    public async Task GetShoppingCartTest()
    {
        //Arrange
        var customer = new Customer();
        customer.Groups.Add("1");
        customer.ShoppingCartItems.Add(new ShoppingCartItem
            { ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "" });

        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));

        //Act
        var result = await _shoppingCartService.GetShoppingCart(customer);
        //Assert
        Assert.IsNotEmpty(result);
    }

    [TestMethod]
    public async Task FindShoppingCartItemTest()
    {
        //Arrange
        var customer = new Customer();
        customer.Groups.Add("1");
        customer.ShoppingCartItems.Add(new ShoppingCartItem
            { ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "", ProductId = "1" });
        customer.ShoppingCartItems.Add(new ShoppingCartItem
            { ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "", ProductId = "2" });

        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));

        //Act
        var result = await _shoppingCartService.FindShoppingCartItem(customer.ShoppingCartItems.ToList(),
            ShoppingCartType.ShoppingCart, "1");

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task AddToCartTest_Success()
    {
        //Arrange
        var customer = new Customer();
        customer.Groups.Add("1");

        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));
        _shoppingCartValidatorMock.Setup(x => x.CheckCommonWarnings(It.IsAny<Customer>(),
                It.IsAny<IList<ShoppingCartItem>>(), It.IsAny<Product>(), ShoppingCartType.ShoppingCart, null, null,
                It.IsAny<int>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult((IList<string>)new List<string>()));
        _shoppingCartValidatorMock.Setup(x => x.GetShoppingCartItemWarnings(It.IsAny<Customer>(),
                It.IsAny<ShoppingCartItem>(), It.IsAny<Product>(), It.IsAny<ShoppingCartValidatorOptions>()))
            .Returns(() => Task.FromResult((IList<string>)new List<string>()));

        //Act
        var result = await _shoppingCartService.AddToCart(customer, "2", ShoppingCartType.ShoppingCart, "");

        //Assert
        Assert.IsNotNull(result.shoppingCartItem);
    }

    [TestMethod]
    public async Task AddToCartTest_Warning()
    {
        //Arrange
        var customer = new Customer();
        customer.Groups.Add("1");

        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));
        _shoppingCartValidatorMock.Setup(x => x.CheckCommonWarnings(It.IsAny<Customer>(),
                It.IsAny<IList<ShoppingCartItem>>(), It.IsAny<Product>(), ShoppingCartType.ShoppingCart, null, null,
                It.IsAny<int>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult((IList<string>)new List<string>()));
        _shoppingCartValidatorMock.Setup(x => x.GetShoppingCartItemWarnings(It.IsAny<Customer>(),
                It.IsAny<ShoppingCartItem>(), It.IsAny<Product>(), It.IsAny<ShoppingCartValidatorOptions>()))
            .Returns(() => Task.FromResult((IList<string>)new List<string> { "Error" }));

        //Act
        var result = await _shoppingCartService.AddToCart(customer, "2", ShoppingCartType.ShoppingCart, "");

        //Assert
        Assert.HasCount(1, result.warnings);
    }

    [TestMethod]
    public async Task UpdateShoppingCartItemTest()
    {
        //Arrange
        var customer = new Customer();
        customer.Groups.Add("1");

        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));
        _shoppingCartValidatorMock.Setup(x => x.CheckCommonWarnings(It.IsAny<Customer>(),
                It.IsAny<IList<ShoppingCartItem>>(), It.IsAny<Product>(), ShoppingCartType.ShoppingCart, null, null,
                It.IsAny<int>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult((IList<string>)new List<string>()));
        _shoppingCartValidatorMock.Setup(x => x.GetShoppingCartItemWarnings(It.IsAny<Customer>(),
                It.IsAny<ShoppingCartItem>(), It.IsAny<Product>(), It.IsAny<ShoppingCartValidatorOptions>()))
            .Returns(() => Task.FromResult((IList<string>)new List<string>()));

        var item = await _shoppingCartService.AddToCart(customer, "2", ShoppingCartType.ShoppingCart, "");
        //Act
        var result = await _shoppingCartService.UpdateShoppingCartItem(customer, item.shoppingCartItem.Id, "",
            new List<CustomAttribute>(), null);
        //Assert
        _customerServiceMock.Verify(x => x.UpdateShoppingCartItem(It.IsAny<string>(), It.IsAny<ShoppingCartItem>()),
            Times.Once);
    }

    [TestMethod]
    public async Task DeleteShoppingCartItemTest()
    {
        //Arrange
        var customer = new Customer();
        customer.Groups.Add("1");

        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));
        _shoppingCartValidatorMock.Setup(x => x.CheckCommonWarnings(It.IsAny<Customer>(),
                It.IsAny<IList<ShoppingCartItem>>(), It.IsAny<Product>(), ShoppingCartType.ShoppingCart, null, null,
                It.IsAny<int>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult((IList<string>)new List<string>()));
        _shoppingCartValidatorMock.Setup(x => x.GetShoppingCartItemWarnings(It.IsAny<Customer>(),
                It.IsAny<ShoppingCartItem>(), It.IsAny<Product>(), It.IsAny<ShoppingCartValidatorOptions>()))
            .Returns(() => Task.FromResult((IList<string>)new List<string>()));

        var item = await _shoppingCartService.AddToCart(customer, "2", ShoppingCartType.ShoppingCart, "");
        //Act
        await _shoppingCartService.DeleteShoppingCartItem(customer, item.shoppingCartItem);
        //Assert
        _customerServiceMock.Verify(x => x.DeleteShoppingCartItem(It.IsAny<string>(), It.IsAny<ShoppingCartItem>()),
            Times.Once);
    }

    [TestMethod]
    public async Task MigrateShoppingCartTest()
    {
        //Arrange
        var customer = new Customer();
        customer.Groups.Add("1");

        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));
        _shoppingCartValidatorMock.Setup(x => x.CheckCommonWarnings(It.IsAny<Customer>(),
                It.IsAny<IList<ShoppingCartItem>>(), It.IsAny<Product>(), ShoppingCartType.ShoppingCart, null, null,
                It.IsAny<int>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult((IList<string>)new List<string>()));
        _shoppingCartValidatorMock.Setup(x => x.GetShoppingCartItemWarnings(It.IsAny<Customer>(),
                It.IsAny<ShoppingCartItem>(), It.IsAny<Product>(), It.IsAny<ShoppingCartValidatorOptions>()))
            .Returns(() => Task.FromResult((IList<string>)new List<string>()));

        var item = await _shoppingCartService.AddToCart(customer, "2", ShoppingCartType.ShoppingCart, "");
        var customer2 = new Customer();
        //Act
        await _shoppingCartService.MigrateShoppingCart(customer, customer2, "", false);
        //Assert
        Assert.IsNotEmpty(customer2.ShoppingCartItems);
        Assert.IsEmpty(customer.ShoppingCartItems);
    }

    [TestMethod]
    public async Task GetShoppingCart_ReturnsTheItemsOfTheGivenCustomer()
    {
        //Arrange
        var customer = new Customer();
        customer.ShoppingCartItems.Add(new ShoppingCartItem
            { ProductId = "1", ShoppingCartTypeId = ShoppingCartType.Wishlist, StoreId = "store" });
        _productServiceMock.Setup(a => a.GetProductById("1", false)).ReturnsAsync(new Product { Id = "1" });

        //Act
        var result = await _shoppingCartService.GetShoppingCart(customer, "store", ShoppingCartType.Wishlist);

        //Assert
        Assert.HasCount(1, result);
        Assert.AreSame(customer.ShoppingCartItems.First(), result[0]);
    }

    [TestMethod]
    public async Task MigrateShoppingCart_MovesCheckoutAttributesOfTheGivenStore()
    {
        //Arrange
        var fromCustomer = new Customer();
        var toCustomer = new Customer();

        //Act
        await _shoppingCartService.MigrateShoppingCart(fromCustomer, toCustomer, "store-b", false);

        //Assert
        _customerServiceMock.Verify(x => x.UpdateUserField(toCustomer, SystemCustomerFieldNames.CheckoutAttributes,
            It.IsAny<List<CustomAttribute>>(), "store-b"), Times.Once);
    }
}