using Grand.Business.Storage.Services;
using Grand.Domain.Data;
using Grand.Domain.Media;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Storage.Tests.Services
{
    [TestClass]
    public class DownloadServiceTests
    {
        private Mock<IMediator> _mediatorMock;
        private Mock<IRepository<Download>> _repositoryMock;
        private Mock<IDatabaseContext> _dbContext;
        private DownloadService _service;

        [TestInitialize]
        public void Init()
        {
            _mediatorMock = new Mock<IMediator>();
            _repositoryMock = new Mock<IRepository<Download>>();
            _dbContext = new Mock<IDatabaseContext>();
            _service = new DownloadService(_repositoryMock.Object, _dbContext.Object, _mediatorMock.Object);
        }

        [TestMethod]
        public async Task GetDownloadById_InvokeExpectedMethod()
        {
            _repositoryMock.Setup(c => c.GetByIdAsync(It.IsAny<string>())).Returns(Task.FromResult(new Download() { DownloadUrl = "url",UseDownloadUrl=true }));
            var result=await _service.GetDownloadById("id");
            _repositoryMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
            Assert.AreEqual(result.DownloadUrl, "url");
        }

        [TestMethod]
        public async Task InsertDownload_InvokeExpectedMethod()
        {
            await _service.InsertDownload(new Download() { UseDownloadUrl = true });
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<Download>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<Download>>(), default), Times.Once);
        }

        [TestMethod]
        public void InsertDownload_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.InsertDownload(null));
        }

        [TestMethod]
        public async Task UpdateDownload_InvokeExpectedMethod()
        {
            await _service.UpdateDownload(new Download() { UseDownloadUrl = true });
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<Download>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<Download>>(), default), Times.Once);
        }

        [TestMethod]
        public void UpdateDownload_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.UpdateDownload(null));
        }

        [TestMethod]
        public async Task DeleteDownload_InvokeExpectedMethod()
        {
            await _service.DeleteDownload(new Download() { UseDownloadUrl = true });
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<Download>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<Download>>(), default), Times.Once);
        }

        [TestMethod]
        public void DeleteDownload_NullArgument_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _service.DeleteDownload(null));
        }
    }
}
