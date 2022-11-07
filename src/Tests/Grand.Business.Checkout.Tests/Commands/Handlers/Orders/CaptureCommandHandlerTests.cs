﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using MediatR;
using Moq;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Queries.Checkout.Orders;

namespace Grand.Business.Checkout.Commands.Handlers.Orders.Tests
{
    [TestClass()]
    public class CaptureCommandHandlerTests
    {
        private CaptureCommandHandler _handler;

        private Mock<IPaymentService> _paymentServiceMock;
        private Mock<IPaymentTransactionService> _paymentTransactionMock;
        private Mock<IOrderService> _orderServiceMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<ILogger> _loggerMock;

        [TestInitialize]
        public void Init()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger>();
            _orderServiceMock = new Mock<IOrderService>();
            _paymentServiceMock = new Mock<IPaymentService>();
            _paymentTransactionMock = new Mock<IPaymentTransactionService>();

            _handler = new CaptureCommandHandler(_paymentServiceMock.Object, _paymentTransactionMock.Object, _mediatorMock.Object, _orderServiceMock.Object, _loggerMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var command = new CaptureCommand() { PaymentTransaction = new PaymentTransaction() };
            _mediatorMock.Setup(x => x.Send(It.IsAny<CanCaptureQuery>(), default))
                .Returns(Task.FromResult(true));

            //Act
            var result = await _handler.Handle(command, CancellationToken.None);

            _paymentServiceMock.Verify(c => c.Capture(It.IsAny<PaymentTransaction>()), Times.Once);
        }
    }
}