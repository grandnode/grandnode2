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
    public class PartiallyPaidOfflineCommandHandlerTests
    {
        private PartiallyPaidOfflineCommandHandler _handler;

        private Mock<IPaymentTransactionService> _paymentTransactionMock;
        private Mock<IOrderService> _orderServiceMock;
        private Mock<IMediator> _mediatorMock;

        [TestInitialize]
        public void Init()
        {
            _mediatorMock = new Mock<IMediator>();
            _orderServiceMock = new Mock<IOrderService>();
            _paymentTransactionMock = new Mock<IPaymentTransactionService>();

            _handler = new PartiallyPaidOfflineCommandHandler(_orderServiceMock.Object, _paymentTransactionMock.Object, _mediatorMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var command = new PartiallyPaidOfflineCommand() { PaymentTransaction = new PaymentTransaction() };
            _mediatorMock.Setup(x => x.Send(It.IsAny<CanPartiallyPaidOfflineQuery>(), default))
                .Returns(Task.FromResult(true));
            _orderServiceMock.Setup(x => x.GetOrderByGuid(It.IsAny<Guid>())).Returns(Task.FromResult(new Order()));
            //Act
            var result = await _handler.Handle(command, CancellationToken.None);

            _orderServiceMock.Verify(c => c.UpdateOrder(It.IsAny<Order>()), Times.Once);
            _paymentTransactionMock.Verify(c => c.UpdatePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Once);
        }
    }
}