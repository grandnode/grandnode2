using Grand.Business.Customers.Services;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
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
    public class CustomerAttributeServiceTests
    {
        private Mock<ICacheBase> _cacheMock;
        private Mock<IRepository<CustomerAttribute>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private CustomerAttributeService _atrService;

        [TestInitialize]
        public void Init()
        {
            _cacheMock = new Mock<ICacheBase>();
            _repositoryMock = new Mock<IRepository<CustomerAttribute>>();
            _mediatorMock = new Mock<IMediator>();
            _atrService = new CustomerAttributeService(_cacheMock.Object, _repositoryMock.Object, _mediatorMock.Object);
        }

        [TestMethod()]
        public async Task InsertCustomerAttribute_ValidArguemnts_InvokeRepositoryAndCache()
        {
            await _atrService.InsertCustomerAttribute(new CustomerAttribute());
                _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<CustomerAttribute>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<CustomerAttribute>>(), default(CancellationToken)), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(),true));
        }

        [TestMethod()]
        public async Task UpdateCustomerAttribute_ValidArguemnts_InvokeRepositoryAndCache()
        {
            await _atrService.UpdateCustomerAttribute(new CustomerAttribute());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<CustomerAttribute>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<CustomerAttribute>>(), default(CancellationToken)), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
        }

        [TestMethod()]
        public async Task DeleteCustomerAttribute_ValidArguemnts_InvokeRepositoryAndCache()
        {
            await _atrService.DeleteCustomerAttribute(new CustomerAttribute());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<CustomerAttribute>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<CustomerAttribute>>(), default(CancellationToken)), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
        }
    }
}
