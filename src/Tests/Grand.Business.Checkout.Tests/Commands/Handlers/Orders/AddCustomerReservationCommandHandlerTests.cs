using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders
{
    [TestClass()]
    public class AddCustomerReservationCommandHandlerTests
    {
        private AddCustomerReservationCommandHandler _handler;
        private Mock<IProductReservationService> _productReservationServiceMock;
        
        [TestInitialize]
        public void Init()
        {
            _productReservationServiceMock = new Mock<IProductReservationService>();
            _handler = new AddCustomerReservationCommandHandler(_productReservationServiceMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var command = new Core.Commands.Checkout.Orders.AddCustomerReservationCommand();
            command.Customer = new Domain.Customers.Customer();
            command.RentalStartDate = DateTime.UtcNow.AddDays(1).Date;
            command.RentalEndDate = DateTime.UtcNow.AddDays(2).Date;
            command.Product = new Domain.Catalog.Product() {
                IncBothDate = true,
                IntervalUnitId = IntervalUnit.Day
            };
            command.ShoppingCartItem = new Domain.Orders.ShoppingCartItem() { };

            _productReservationServiceMock.Setup(c => c.GetProductReservationsByProductId(It.IsAny<string>(), true, null, 0, int.MaxValue)).Returns(() => Task.FromResult((IPagedList<ProductReservation>)
                new PagedList<ProductReservation>() {
                    new ProductReservation() { Date = DateTime.UtcNow.AddDays(4).Date, Resource = "" },
                    new ProductReservation() { Date = DateTime.UtcNow.AddDays(3).Date, Resource = "" },
                    new ProductReservation() { Date = DateTime.UtcNow.AddDays(2).Date, Resource = "" },
                    new ProductReservation() { Date = DateTime.UtcNow.AddDays(1).Date, Resource = "" },
                    new ProductReservation() { Date = DateTime.UtcNow.Date, Resource = "" } }
                ));

            _productReservationServiceMock.Setup(c => c.GetCustomerReservationsHelpers(It.IsAny<string>())).ReturnsAsync((IList<CustomerReservationsHelper>)
               new List<CustomerReservationsHelper>() { });
            //Act
            var result = await _handler.Handle(command, CancellationToken.None);

            //Assert
            _productReservationServiceMock.Verify(c => c.InsertCustomerReservationsHelper(It.IsAny<CustomerReservationsHelper>()));
        }
    }
}