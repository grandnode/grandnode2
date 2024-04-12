using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class AddRequiredProductsCommandHandlerTests
{
    private AddRequiredProductsCommandHandler _handler;
    private Mock<IProductService> _productServiceMock;
    private Mock<IShoppingCartService> _shoppingCartServiceMock;
    private ShoppingCartSettings _shoppingCartSettings;

    [TestInitialize]
    public void Init()
    {
        _shoppingCartServiceMock = new Mock<IShoppingCartService>();
        _shoppingCartSettings = new ShoppingCartSettings();
        _productServiceMock = new Mock<IProductService>();
        _handler = new AddRequiredProductsCommandHandler(_shoppingCartServiceMock.Object, _shoppingCartSettings,
            _productServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new AddRequiredProductsCommand {
            Customer = new Customer()
        };
        command.Customer.ShoppingCartItems.Add(new ShoppingCartItem
            { ProductId = "1", ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "" });
        command.Product = new Product
            { Id = "1", RequireOtherProducts = true, RequiredProductIds = "2,3", AutoAddRequiredProducts = true };
        command.ShoppingCartType = ShoppingCartType.ShoppingCart;
        command.StoreId = "";

        _productServiceMock.Setup(a => a.GetProductById("2", false)).Returns(() =>
            Task.FromResult(new Product { Id = "2", Published = true, Price = 10 }));
        _productServiceMock.Setup(a => a.GetProductById("3", false)).Returns(() =>
            Task.FromResult(new Product { Id = "3", Published = true, Price = 10 }));

        //Act
        var result = await _handler.Handle(command, CancellationToken.None);

        //Assert
        _shoppingCartServiceMock.Verify(
            c => c.AddToCart(It.IsAny<Customer>(), It.IsAny<string>(), ShoppingCartType.ShoppingCart, "", null, null,
                null, null, null, 1, false, "", "", "", It.IsAny<ShoppingCartValidatorOptions>()), Times.AtLeastOnce);
    }
}