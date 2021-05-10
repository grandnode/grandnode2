using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Tests.Commands.Orders
{
    [TestClass]
    public class CheckOrderStatusCommandHandlerTests
    {
        private Mock<IMediator> _mediatorMock;
        private Mock<IOrderService> _orderSerivce;
        private OrderSettings _settings;
        private CheckOrderStatusCommandHandler _handler;

        [TestInitialize]
        public void Init()
        {
            _mediatorMock = new Mock<IMediator>();
            _orderSerivce = new Mock<IOrderService>();
            _settings = new OrderSettings();
            _handler = new CheckOrderStatusCommandHandler(_mediatorMock.Object,_orderSerivce.Object,_settings);
        }

        [TestMethod]
        public async Task Handle_InvokeExpectedMethods()
        {
            CheckOrderStatusCommand request = new CheckOrderStatusCommand();
            request.Order = new Order();
            request.Order.PaymentStatusId = PaymentStatus.Paid;
            request.Order.PaidDateUtc = null;
            request.Order.OrderStatusId = (int)OrderStatusSystem.Pending;


            await _handler.Handle(request, default);
            _orderSerivce.Verify(c => c.UpdateOrder(request.Order), Times.Once);
            _mediatorMock.Verify(c => c.Send(It.IsAny<SetOrderStatusCommand>(), default), Times.AtLeastOnce);
        }
    }
}
