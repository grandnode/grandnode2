using Grand.Business.Catalog.Services.Products;
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

namespace Grand.Business.Catalog.Tests.Service.Products
{
    [TestClass()]
    public class CustomerGroupProductServiceTests
    {
        private Mock<IRepository<CustomerGroupProduct>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<ICacheBase> _cacheMock;
        private CustomerGroupProductService _custometGroupService;

        [TestInitialize()]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<CustomerGroupProduct>>();
            _mediatorMock = new Mock<IMediator>();
            _cacheMock = new Mock<ICacheBase>();
            _custometGroupService = new CustomerGroupProductService(_repositoryMock.Object,_cacheMock.Object,_mediatorMock.Object);
        }

        [TestMethod()]
        public async Task DeleteCustomerGroupProduct_ValidArguments_InoveMethods()
        {
            await _custometGroupService.DeleteCustomerGroupProduct(new CustomerGroupProduct());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<CustomerGroupProduct>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<CustomerGroupProduct>>(), default(CancellationToken)), Times.Once);
        }


        [TestMethod()]
        public async Task InsertCustomerGroupProduct_ValidArguments_InoveMethods()
        {
            await _custometGroupService.InsertCustomerGroupProduct(new CustomerGroupProduct());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<CustomerGroupProduct>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<CustomerGroupProduct>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task GetCustomerGroupProducts_ValidArguments_InoveMethods()
        {
            await _custometGroupService.GetCustomerGroupProducts("id");
            //get by cache 
            _cacheMock.Verify(c => c.GetAsync<List<CustomerGroupProduct>>(It.IsAny<string>(),It.IsAny<Func<Task<List<CustomerGroupProduct>>>>()), Times.Exactly(1));
        }
    }
}
