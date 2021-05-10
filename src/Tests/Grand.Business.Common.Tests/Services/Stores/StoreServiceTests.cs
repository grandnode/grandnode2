using Grand.Business.Common.Services.Stores;
using Grand.Domain.Data;
using Grand.Domain.Stores;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Stores
{
    [TestClass()]
    public class StoreServiceTests
    {
        private Mock<ICacheBase> _cacheMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<IRepository<Store>> _repository;
        private StoreService _service;

        [TestInitialize]
        public void Init()
        {
            _cacheMock = new Mock<ICacheBase>();
            _mediatorMock = new Mock<IMediator>();
            _repository = new Mock<IRepository<Store>>();
            _service = new StoreService(_cacheMock.Object, _repository.Object, _mediatorMock.Object);
        }

        [TestMethod()]
        public async Task InsertStore_ValidArgument_InvokeExpectedMethods()
        {
            await _service.InsertStore(new Store());
            _repository.Verify(c => c.InsertAsync(It.IsAny<Store>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Store>>(), default), Times.Once);
            _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateStore_ValidArgument_InvokeExpectedMethods()
        {
            await _service.UpdateStore(new Store());
            _repository.Verify(c => c.UpdateAsync(It.IsAny<Store>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Store>>(), default), Times.Once);
            _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteStore_ValidArgument_InvokeExpectedMethods()
        {
            _cacheMock.Setup(c => c.GetAsync<List<Store>>(It.IsAny<string>(), It.IsAny<Func<Task<List<Store>>>>()))
                .Returns(Task.FromResult(new List<Store>() { new Store(), new Store() }));
            await _service.DeleteStore(new Store());
            _repository.Verify(c => c.DeleteAsync(It.IsAny<Store>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<Store>>(), default), Times.Once);
            _cacheMock.Verify(c => c.Clear(It.IsAny<bool>()), Times.Once);
        }

        [TestMethod()]
        public void DeleteStore_OnlyOneStore_ThrowException()
        {
            //can not remove store if it is only one 
            _cacheMock.Setup(c => c.GetAsync<List<Store>>(It.IsAny<string>(), It.IsAny<Func<Task<List<Store>>>>()))
                .Returns(Task.FromResult(new List<Store>() { new Store() }));
            Assert.ThrowsExceptionAsync<Exception>(async () => await _service.DeleteStore(new Store()));
        }
    }
}
