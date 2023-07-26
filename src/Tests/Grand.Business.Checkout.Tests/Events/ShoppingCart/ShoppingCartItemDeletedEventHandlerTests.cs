using Grand.Business.Checkout.Events.ShoppingCart;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.ShoppingCart
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