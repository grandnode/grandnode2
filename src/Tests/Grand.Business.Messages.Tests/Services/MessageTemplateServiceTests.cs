using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Messages.Services;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Tests.Services
{
    [TestClass]
    public class MessageTemplateServiceTests
    {
        private Mock<ICacheBase> _cacheMock;
        private Mock<IAclService> _aclService;
        private Mock<IRepository<MessageTemplate>> _repositoryMock;
        private Mock<IMediator> _mediatorMock;
        private CatalogSettings _settings;
        private MessageTemplateService _service;

        [TestInitialize]
        public void Init()
        {
            _cacheMock = new Mock<ICacheBase>();
            _aclService = new Mock<IAclService>();
            _repositoryMock = new Mock<IRepository<MessageTemplate>>();
            _mediatorMock = new Mock<IMediator>();
            _settings = new CatalogSettings();
            _service = new MessageTemplateService(_cacheMock.Object, _aclService.Object, _repositoryMock.Object, _mediatorMock.Object);
        }
        
        [TestMethod]
        public void CopyMessageTemplate_NullArrguemnt_ThrowException()
        {
            Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>  await _service.CopyMessageTemplate(null));
        }

        [TestMethod]
        public async Task CopyMessageTemplate_InsertCopyEntity()
        {
            var template = new MessageTemplate()
            {
                Id = "id1",
                Name = "Name"
            };

            var result=await _service.CopyMessageTemplate(template);
            Assert.AreEqual(template.Name, result.Name);
            Assert.AreNotEqual(template.Id, result.Id);
            //should be insert into db
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<MessageTemplate>()), Times.Once);
        }

        [TestMethod]
        public async Task InsertMessageTemplate_InvokeExpectedMethods()
        {
            await _service.InsertMessageTemplate(new MessageTemplate());
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<MessageTemplate>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<MessageTemplate>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateMessageTemplate_InvokeExpectedMethods()
        {
            await _service.UpdateMessageTemplate(new MessageTemplate());
            _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<MessageTemplate>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<MessageTemplate>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task DeleteMessageTemplate_InvokeExpectedMethods()
        {
            await _service.DeleteMessageTemplate(new MessageTemplate());
            _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<MessageTemplate>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<MessageTemplate>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }
    }
}
