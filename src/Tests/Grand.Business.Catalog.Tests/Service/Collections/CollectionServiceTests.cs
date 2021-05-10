using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Services.Collections;
using Grand.Business.Common.Interfaces.Security;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Infrastructure;
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

namespace Grand.Business.Catalog.Tests.Service.Collections
{
    [TestClass()]
    public class CollectionServiceTests
    {
        private Mock<ICacheBase> _cacheMock;
        private Mock<IRepository<Collection>> _repositoryMock;
        private Mock<IWorkContext> _workContextMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<IAclService> _aclMock;
        private CatalogSettings _settings;
        private ICollectionService _collectionService;

        [TestInitialize()]
        public void Init()
        {
            _cacheMock = new Mock<ICacheBase>();
            _repositoryMock = new Mock<IRepository<Collection>>();
            _workContextMock = new Mock<IWorkContext>();
            _mediatorMock = new Mock<IMediator>();
            _aclMock = new Mock<IAclService>();
            _settings = new CatalogSettings();
            _collectionService = new CollectionService(_cacheMock.Object,_repositoryMock.Object,_workContextMock.Object
                ,_mediatorMock.Object,_aclMock.Object);
        }


        [TestMethod()]
        public async Task DeleteCollection_ValidArguments_InoveMethods()
        {
            await _collectionService.DeleteCollection(new Collection());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<Collection>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<Collection>>(), default(CancellationToken)), Times.Once);
        }


        [TestMethod()]
        public async Task InsertCollection_ValidArguments_InoveMethods()
        {
            await _collectionService.InsertCollection(new Collection());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<Collection>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Collection>>(), default(CancellationToken)), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateCollection_ValidArguments_InoveMethods()
        {
            await _collectionService.UpdateCollection(new Collection());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Collection>()), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true));
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Collection>>(), default(CancellationToken)), Times.Once);
        }
        
        [TestMethod()]
        public async Task GeCollectionById_ValidArgument_GetByCache()
        {
            await _collectionService.GetCollectionById("id");
            _cacheMock.Verify(c => c.GetAsync<Collection>(It.IsAny<string>(), It.IsAny<Func<Task<Collection>>>()),Times.Once);
        }

        [TestMethod()]
        public void DeleteCollection_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _collectionService.DeleteCollection(null));
        }

        [TestMethod()]
        public void InsertCollection_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _collectionService.InsertCollection(null));
        }

        [TestMethod()]
        public void UpdateCollection_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _collectionService.UpdateCollection(null));
        }
    }
}
