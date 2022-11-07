﻿using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Commands.Handlers.Orders.Tests
{
    [TestClass()]
    public class PartiallyRefundCommandHandlerTests
    {
        private PartiallyRefundCommandHandler _handler;
        private Mock<IPaymentService> _paymentServiceMock;
        private Mock<IPaymentTransactionService> _paymentTransactionMock;
        private Mock<IOrderService> _orderServiceMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<IMessageProviderService> _messageProviderServiceMock;
        private Mock<ILogger> _loggerMock;

        [TestInitialize]
        public void Init()
        {
            _mediatorMock = new Mock<IMediator>();
            _orderServiceMock = new Mock<IOrderService>();
            _paymentTransactionMock = new Mock<IPaymentTransactionService>();
            _messageProviderServiceMock = new Mock<IMessageProviderService>();
            _paymentServiceMock = new Mock<IPaymentService>();
            _loggerMock = new Mock<ILogger>();

            _handler = new PartiallyRefundCommandHandler(_paymentServiceMock.Object, _paymentTransactionMock.Object, _mediatorMock.Object, _messageProviderServiceMock.Object, _loggerMock.Object, _orderServiceMock.Object, new Domain.Localization.LanguageSettings());
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var command = new PartiallyRefundCommand() { PaymentTransaction = new PaymentTransaction() };
            _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyRefundQuery>(), default))
                .Returns(Task.FromResult(true));
            _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).Returns(Task.FromResult(new Order()));
            _paymentServiceMock.Setup(x => x.Refund(It.IsAny<RefundPaymentRequest>())).Returns(Task.FromResult(new RefundPaymentResult() { NewTransactionStatus = TransactionStatus.Refunded }));
            _paymentTransactionMock.Setup(x => x.GetById(It.IsAny<string>())).Returns(Task.FromResult(new PaymentTransaction()));
            //Act
            var result = await _handler.Handle(command, CancellationToken.None);
            //Assert
            _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
            _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Once);
        }
    }
}