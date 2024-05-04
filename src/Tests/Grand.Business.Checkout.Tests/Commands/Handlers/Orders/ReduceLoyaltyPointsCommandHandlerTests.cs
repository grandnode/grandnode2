using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class ReduceLoyaltyPointsCommandHandlerTests
{
    private Mock<ICurrencyService> _currencyServiceMock;
    private Mock<ICustomerService> _customerServiceMock;
    private ReduceLoyaltyPointsCommandHandler _handler;
    private Mock<ILoyaltyPointsService> _loyaltyPointsServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<ITranslationService> _translationServiceMock;


    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _loyaltyPointsServiceMock = new Mock<ILoyaltyPointsService>();
        _mediatorMock = new Mock<IMediator>();
        _currencyServiceMock = new Mock<ICurrencyService>();
        _orderServiceMock = new Mock<IOrderService>();
        _translationServiceMock = new Mock<ITranslationService>();

        _handler = new ReduceLoyaltyPointsCommandHandler(_customerServiceMock.Object, _loyaltyPointsServiceMock.Object,
            _mediatorMock.Object, _orderServiceMock.Object, _translationServiceMock.Object,
            _currencyServiceMock.Object);
    }


    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new ReduceLoyaltyPointsCommand {
            Order = new Order { StoreId = "", OrderNumber = 1, CalcLoyaltyPoints = 100 }
        };
        var expectedCustomer = new Customer { Username = "John", Active = true };
        _customerServiceMock.Setup(c => c.GetCustomerById(It.IsAny<string>()))
            .Returns(() => Task.FromResult(expectedCustomer));
        _currencyServiceMock.Setup(c => c.GetCurrencyByCode(It.IsAny<string>()))
            .Returns(() => Task.FromResult(new Currency()));
        _currencyServiceMock.Setup(c => c.ConvertToPrimaryStoreCurrency(100, It.IsAny<Currency>()))
            .Returns(() => Task.FromResult((double)100));
        _translationServiceMock.Setup(x => x.GetResource(It.IsAny<string>())).Returns("Name");
        _mediatorMock.Setup(x => x.Send(It.IsAny<CalculateLoyaltyPointsCommand>(), default))
            .Returns(Task.FromResult(100));

        //Act
        var result = await _handler.Handle(command, CancellationToken.None);

        //Assert
        _loyaltyPointsServiceMock.Verify(
            c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), It.Is<int>(g => g < 0), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()), Times.Once);
    }
}