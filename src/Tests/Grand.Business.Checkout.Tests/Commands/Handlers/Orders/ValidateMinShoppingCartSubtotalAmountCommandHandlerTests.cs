using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class ValidateMinShoppingCartSubtotalAmountCommandHandlerTests
{
    private ValidateMinShoppingCartSubtotalAmountCommandHandler _handler;
    private OrderSettings _orderSettings;
    private Mock<IOrderCalculationService> _orderTotalCalculationServiceMock;

    [TestInitialize]
    public void Init()
    {
        _orderTotalCalculationServiceMock = new Mock<IOrderCalculationService>();
        _orderSettings = new OrderSettings { MinOrderSubtotalAmount = 10 };
        _handler = new ValidateMinShoppingCartSubtotalAmountCommandHandler(_orderTotalCalculationServiceMock.Object,
            _orderSettings);
    }


    [TestMethod]
    public async Task HandleTest()
    {
        var command = new ValidateMinShoppingCartSubtotalAmountCommand {
            Customer = new Customer(),
            Cart = new List<ShoppingCartItem> { new() }
        };

        _orderTotalCalculationServiceMock
            .Setup(x => x.GetShoppingCartSubTotal(It.IsAny<IList<ShoppingCartItem>>(), false)).Returns(() =>
                Task.FromResult<(double discountAmount, List<ApplyDiscount> appliedDiscounts, double
                    subTotalWithoutDiscount, double subTotalWithDiscount, SortedDictionary<double, double> taxRates)>((
                    20, null, 100, 100, null)));
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }
}