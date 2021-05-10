using Grand.Business.Customers.Services;
using Grand.Domain.Customers;
using Grand.Domain.Data;
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

namespace Grand.Business.Customers.Tests.Services
{
    [TestClass()]
    public class CustomerNoteServiceTests
    {
        private Mock<IRepository<CustomerNote>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private CustomerNoteService _noteService;

        [TestInitialize()]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<CustomerNote>>();
            _mediatorMock = new Mock<IMediator>();
            _noteService = new CustomerNoteService(_repositoryMock.Object,_mediatorMock.Object);
        }

        [TestMethod()]
        public async Task InsertCustomerNote_ValidArguments_InvokeRepositoryAndPublishEvent()
        {
            await _noteService.InsertCustomerNote(new CustomerNote());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<CustomerNote>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<CustomerNote>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteCustomerNote_ValidArguments_InvokeRepositoryAndPublishEvent()
        {
            await _noteService.DeleteCustomerNote(new CustomerNote());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<CustomerNote>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<CustomerNote>>(), default(CancellationToken)), Times.Once);
        }
    }
}
