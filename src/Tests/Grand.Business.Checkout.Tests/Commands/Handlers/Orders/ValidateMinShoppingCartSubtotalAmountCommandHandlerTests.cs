using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
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
    private Mock<IShoppingCartSubTotalService> _shoppingCartSubTotalServiceMock;

    [TestInitialize]
    public void Init()
    {
        _shoppingCartSubTotalServiceMock = new Mock<IShoppingCartSubTotalService>();
        _orderSettings = new OrderSettings { MinOrderSubtotalAmount = 10 };
        _handler = new ValidateMinShoppingCartSubtotalAmountCommandHandler(_shoppingCartSubTotalServiceMock.Object,
            _orderSettings);
    }


    [TestMethod]
    public async Task HandleTest()
    {
        var command = new ValidateMinShoppingCartSubtotalAmountCommand {
            Cart = new List<ShoppingCartItem> { new() },
            Context = new Grand.Business.Core.Utilities.Checkout.CalculationContext(new Customer(),
                new Grand.Domain.Stores.Store(), new Grand.Domain.Directory.Currency(),
                Grand.Domain.Tax.TaxDisplayType.ExcludingTax)
        };

        _shoppingCartSubTotalServiceMock
            .Setup(x => x.GetShoppingCartSubTotal(It.IsAny<IList<ShoppingCartItem>>(), It.IsAny<Grand.Business.Core.Utilities.Checkout.CalculationContext>(), false)).Returns(() =>
                Task.FromResult(new Grand.Business.Core.Utilities.Checkout.ShoppingCartSubTotal(20, null, 100, 100,
                    null)));
        //Act
        var result = await _handler.Handle(command, CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }
}