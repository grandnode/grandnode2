using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
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

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new AddCustomerReservationCommand {
            Customer = new Customer(),
            RentalStartDate = DateTime.UtcNow.AddDays(1).Date,
            RentalEndDate = DateTime.UtcNow.AddDays(2).Date,
            Product = new Product {
                IncBothDate = true,
                IntervalUnitId = IntervalUnit.Day
            },
            ShoppingCartItem = new ShoppingCartItem()
        };

        _productReservationServiceMock
            .Setup(c => c.GetProductReservationsByProductId(It.IsAny<string>(), true, null, 0, int.MaxValue)).Returns(
                () => Task.FromResult((IPagedList<ProductReservation>)
                    new PagedList<ProductReservation> {
                        new() { Date = DateTime.UtcNow.AddDays(4).Date, Resource = "" },
                        new() { Date = DateTime.UtcNow.AddDays(3).Date, Resource = "" },
                        new() { Date = DateTime.UtcNow.AddDays(2).Date, Resource = "" },
                        new() { Date = DateTime.UtcNow.AddDays(1).Date, Resource = "" },
                        new() { Date = DateTime.UtcNow.Date, Resource = "" }
                    }
                ));

        _productReservationServiceMock.Setup(c => c.GetCustomerReservationsHelpers(It.IsAny<string>()))
            .ReturnsAsync(new List<CustomerReservationsHelper>());
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);

        //Assert
        _productReservationServiceMock.Verify(c =>
            c.InsertCustomerReservationsHelper(It.IsAny<CustomerReservationsHelper>()));
    }
}