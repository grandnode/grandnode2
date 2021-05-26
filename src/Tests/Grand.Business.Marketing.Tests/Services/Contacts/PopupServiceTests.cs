using Grand.Business.Marketing.Services.Contacts;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Events;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Tests.Services.Contacts
{
    [TestClass()]
    public class PopupServiceTests
    {
        private Mock<IRepository<PopupActive>> _repoMock;
        private Mock<IRepository<PopupArchive>> _repositoryArchiveMock;
        private Mock<IMediator> _mediatorMock;
        private PopupService _popupService;

        [TestInitialize()]
        public void Init()
        {
            CommonPath.BaseDirectory = "";

            _repoMock = new Mock<IRepository<PopupActive>>();
            _repositoryArchiveMock = new Mock<IRepository<PopupArchive>>();
            _mediatorMock = new Mock<IMediator>();
            _popupService = new PopupService(_repoMock.Object, _repositoryArchiveMock.Object, _mediatorMock.Object);
        }

        [TestMethod()]
        public void InsertPopupActive_NullArguemnt_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _popupService.InsertPopupActive(null));
        }

        [TestMethod()]
        public async Task InsertPopupActive_ValidArguemnt_InvokeRepositoryAndPublishEvent()
        {
            await _popupService.InsertPopupActive(new PopupActive());
            _repoMock.Verify(c => c.InsertAsync(It.IsAny<PopupActive>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<PopupActive>>(), default), Times.Once);
        }
    }
}
