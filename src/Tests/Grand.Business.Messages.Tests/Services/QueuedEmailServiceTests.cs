using Grand.Business.Messages.Services;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Tests.Services
{
    [TestClass]
    public class QueuedEmailServiceTests
    {
        private Mock<IRepository<QueuedEmail>> _repository;
        private Mock<IMediator> _mediatorMock;
        private QueuedEmailService _service;

        [TestInitialize]
        public void Init()
        {
            _repository = new Mock<IRepository<QueuedEmail>>();
            _mediatorMock = new Mock<IMediator>();
            _service = new QueuedEmailService(_repository.Object,_mediatorMock.Object);
        }


        [TestMethod()]
        public async Task InsertQueuedEmail_ValidArgument_InvokeExpectedMethods()
        {
            await _service.InsertQueuedEmail(new QueuedEmail());
            _repository.Verify(c => c.InsertAsync(It.IsAny<QueuedEmail>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<QueuedEmail>>(), default), Times.Once);
        }

        [TestMethod]
        public void InsertQueuedEmail_NullArguemnt_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.InsertQueuedEmail(new QueuedEmail()));
        }

        [TestMethod()]
        public async Task UpdateQueuedEmail_ValidArgument_InvokeExpectedMethods()
        {
            await _service.UpdateQueuedEmail(new QueuedEmail());
            _repository.Verify(c => c.UpdateAsync(It.IsAny<QueuedEmail>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<QueuedEmail>>(), default), Times.Once);
        }

        [TestMethod]
        public void UpdateQueuedEmai_NullArguemnt_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdateQueuedEmail(new QueuedEmail()));
        }

        [TestMethod()]
        public async Task DeleteQueuedEmail_ValidArgument_InvokeExpectedMethods()
        {
          
            await _service.DeleteQueuedEmail(new QueuedEmail());
            _repository.Verify(c => c.DeleteAsync(It.IsAny<QueuedEmail>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<QueuedEmail>>(), default), Times.Once);
        }

        [TestMethod]
        public void DeleteQueuedEmai_NullArguemnt_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.DeleteQueuedEmail(new QueuedEmail()));
        }
    }
}
