using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Checkout.Commands.Handlers.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Business.Core.Utilities.Checkout;

namespace Grand.Business.Checkout.Commands.Handlers.Orders.Tests
{
    [TestClass()]
    public class AddRequiredProductsCommandHandlerTests
    {
        private AddRequiredProductsCommandHandler _handler;
        private Mock<IShoppingCartService> _shoppingCartServiceMock;
        private ShoppingCartSettings _shoppingCartSettings;
        private Mock<IProductService> _productServiceMock;

        [TestInitialize]
        public void Init()
        {
            _shoppingCartServiceMock = new Mock<IShoppingCartService>();
            _shoppingCartSettings = new ShoppingCartSettings();
            _productServiceMock = new Mock<IProductService>();
            _handler = new AddRequiredProductsCommandHandler(_shoppingCartServiceMock.Object, _shoppingCartSettings, _productServiceMock.Object);

        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var command = new Core.Commands.Checkout.Orders.AddRequiredProductsCommand();
            command.Customer = new Domain.Customers.Customer();
            command.Customer.ShoppingCartItems.Add(new ShoppingCartItem() { ProductId = "1", ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "" });
            command.Product = new Domain.Catalog.Product() { Id = "1", RequireOtherProducts = true, RequiredProductIds = "2,3", AutoAddRequiredProducts = true };
            command.ShoppingCartType = ShoppingCartType.ShoppingCart;
            command.StoreId = "";

            _productServiceMock.Setup(a => a.GetProductById("2", false)).Returns(() => Task.FromResult(new Product() { Id = "2", Published = true, Price = 10 }));
            _productServiceMock.Setup(a => a.GetProductById("3", false)).Returns(() => Task.FromResult(new Product() { Id = "3", Published = true, Price = 10 }));

            //Act
            var result = await _handler.Handle(command, CancellationToken.None);

            //Assert
            _shoppingCartServiceMock.Verify(c => c.AddToCart(It.IsAny<Customer>(), It.IsAny<string>(), ShoppingCartType.ShoppingCart, "", null, null,null, null, null, 1, false, "", "", "", It.IsAny<ShoppingCartValidatorOptions>()), Times.AtLeastOnce);
        }
    }
}