using Grand.Business.Common.Services.Directory;
using Grand.Domain.Common;
using Grand.Domain.Data;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Directory
{
    [TestClass()]
    public class SearchTermServiceTests
    {
        private Mock<IRepository<SearchTerm>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private SearchTermService _service;

        [TestInitialize()]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<SearchTerm>>();
            _mediatorMock = new Mock<IMediator>();
            _service = new SearchTermService(_repositoryMock.Object, _mediatorMock.Object);
        }


        [TestMethod()]
        public async Task InsertSearchTerm_ValidArgument()
        {
            await _service.InsertSearchTerm(new SearchTerm());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<SearchTerm>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<SearchTerm>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task UpdateSearchTerm_ValidArgument()
        {
            await _service.UpdateSearchTerm(new SearchTerm());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<SearchTerm>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<SearchTerm>>(), default), Times.Once);
        }

        [TestMethod()]
        public async Task DeleteSearchTerm_ValidArgument()
        {
            await _service.DeleteSearchTerm(new SearchTerm());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<SearchTerm>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<SearchTerm>>(), default), Times.Once);
        }

        [TestMethod()]
        public void InsertSearchTerm_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.InsertSearchTerm(null));
        }

        [TestMethod()]
        public void UpdateSearchTerm_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdateSearchTerm(null));
        }

        [TestMethod()]
        public void DeleteSearchTerm_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.DeleteSearchTerm(null));
        }
    }
}
