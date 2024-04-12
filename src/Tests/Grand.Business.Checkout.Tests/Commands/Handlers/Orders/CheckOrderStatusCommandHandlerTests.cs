using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class CheckOrderStatusCommandHandlerTests
{
    private CheckOrderStatusCommandHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderSerivce;
    private OrderSettings _settings;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _orderSerivce = new Mock<IOrderService>();
        _settings = new OrderSettings();
        _handler = new CheckOrderStatusCommandHandler(_mediatorMock.Object, _orderSerivce.Object, _settings);
    }

    [TestMethod]
    public async Task Handle_InvokeExpectedMethods()
    {
        var request = new CheckOrderStatusCommand {
            Order = new Order {
                PaymentStatusId = PaymentStatus.Paid,
                PaidDateUtc = null,
                OrderStatusId = (int)OrderStatusSystem.Pending
            }
        };


        await _handler.Handle(request, default);
        _orderSerivce.Verify(c => c.UpdateOrder(request.Order), Times.Once);
        _mediatorMock.Verify(c => c.Send(It.IsAny<SetOrderStatusCommand>(), default), Times.AtLeastOnce);
    }
}