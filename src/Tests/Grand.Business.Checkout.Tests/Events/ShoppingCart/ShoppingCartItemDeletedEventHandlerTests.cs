using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Checkout.Events.Shipping;
using Grand.Business.Checkout.Events.ShoppingCart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Moq;
using Grand.Domain.Catalog;

namespace Grand.Business.Checkout.Events.Shipping.Tests
{
    [TestClass()]
    public class ShoppingCartItemDeletedEventHandlerTests
    {
        private ShoppingCartItemDeletedEventHandler _shoppingCartItemDeletedEventHandler;

        private Mock<IProductReservationService> _productReservationServiceMock;

        [TestInitialize()]
        public void Init()
        {
            _productReservationServiceMock = new Mock<IProductReservationService>();
            _shoppingCartItemDeletedEventHandler = new ShoppingCartItemDeletedEventHandler(_productReservationServiceMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            _productReservationServiceMock.Setup(x => x.GetCustomerReservationsHelperBySciId(It.IsAny<string>())).Returns(Task.FromResult((IList<CustomerReservationsHelper>)new List<CustomerReservationsHelper>() { new CustomerReservationsHelper() }));
            var notification = new Infrastructure.Events.EntityDeleted<Domain.Orders.ShoppingCartItem>(new Domain.Orders.ShoppingCartItem() { RentalStartDateUtc = DateTime.UtcNow, RentalEndDateUtc = DateTime.UtcNow});
            //Act
            await _shoppingCartItemDeletedEventHandler.Handle(notification, CancellationToken.None);
            //Assert
            _productReservationServiceMock.Verify(c => c.DeleteCustomerReservationsHelper(It.IsAny<CustomerReservationsHelper>()), Times.Once);
        }
    }
}