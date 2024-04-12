using Grand.Business.Checkout.Services.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
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
using MediatR;
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
    private Mock<IUserFieldService> _userFieldServiceMock;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _workContextMock = new Mock<IWorkContext>();
        _productServiceMock = new Mock<IProductService>();
        _shoppingCartSettings = new ShoppingCartSettings();
        _customerServiceMock = new Mock<ICustomerService>();
        _mediatorMock = new Mock<IMediator>();
        _userFieldServiceMock = new Mock<IUserFieldService>();
        _shoppingCartValidatorMock = new Mock<IShoppingCartValidator>();

        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "", Name = "test store" });
        var customer = new Customer();
        customer.Groups.Add("1");
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => customer);
        _workContextMock.Setup(c => c.WorkingCurrency).Returns(() => new Currency());
        _workContextMock.Setup(c => c.TaxDisplayType).Returns(() => TaxDisplayType.ExcludingTax);

        _shoppingCartService = new ShoppingCartService(_workContextMock.Object, _productServiceMock.Object,
            _customerServiceMock.Object, _mediatorMock.Object, _userFieldServiceMock.Object,
            _shoppingCartValidatorMock.Object, _shoppingCartSettings);
    }

    [TestMethod]
    public async Task GetShoppingCartTest()
    {
        //Arrange
        var customer = new Customer();
        customer.Groups.Add("1");
        customer.ShoppingCartItems.Add(new ShoppingCartItem
            { ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => customer);

        _productServiceMock.Setup(a => a.GetProductById(It.IsAny<string>(), false)).Returns(() =>
            Task.FromResult(new Product { Id = "1", Published = true, Price = 10 }));

        //Act
        var result = await _shoppingCartService.GetShoppingCart();
        //Assert
        Assert.IsTrue(result.Any());
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
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => customer);

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
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => customer);

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
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => customer);

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
        Assert.AreEqual(1, result.warnings.Count);
    }

    [TestMethod]
    public async Task UpdateShoppingCartItemTest()
    {
        //Arrange
        var customer = new Customer();
        customer.Groups.Add("1");
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => customer);

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
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => customer);

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
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => customer);

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
        await _shoppingCartService.MigrateShoppingCart(customer, customer2, false);
        //Assert
        Assert.IsTrue(customer2.ShoppingCartItems.Any());
        Assert.IsFalse(customer.ShoppingCartItems.Any());
    }
}