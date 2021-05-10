using Grand.Business.Checkout.Services.Payments;
using Grand.Domain.Data;
using Grand.Domain.Payments;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Tests.Services.Payments
{
    [TestClass]
    public class PaymentTransactionServiceTests
    {
        private Mock<IRepository<PaymentTransaction>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private PaymentTransactionService _service;

        [TestInitialize]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<PaymentTransaction>>();
            _mediatorMock = new Mock<IMediator>();
            _service = new PaymentTransactionService(_repositoryMock.Object,_mediatorMock.Object);
        }

        [TestMethod]
        public async Task InsertPaymentTransaction_InvokeExpectedMethods()
        {
            await _service.InsertPaymentTransaction(new PaymentTransaction());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<PaymentTransaction>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<PaymentTransaction>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void InsertPaymentTransaction_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.InsertPaymentTransaction(null));
        }

        [TestMethod]
        public async Task UpdatePaymentTransaction_InvokeExpectedMethods()
        {
            await _service.UpdatePaymentTransaction(new PaymentTransaction());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<PaymentTransaction>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<PaymentTransaction>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void UpdatePaymentTransaction_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdatePaymentTransaction(null));
        }

        [TestMethod]
        public async Task DeletePaymentTransaction_InvokeExpectedMethods()
        {
            await _service.DeletePaymentTransaction(new PaymentTransaction());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<PaymentTransaction>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<PaymentTransaction>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod]
        public void DeletePaymentTransaction_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.DeletePaymentTransaction(null));
        }
    }
}
