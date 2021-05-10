using Grand.Business.Messages.Services;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using Grand.SharedKernel;
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
    public class EmailAccountServiceTests
    {
        private Mock<ICacheBase> _cacheMock;
        private Mock<IMediator> _mediatorMock;
        private Mock<IRepository<EmailAccount>> _repository;
        private EmailAccountService _service;

        [TestInitialize]
        public void Init()
        {
            _cacheMock = new Mock<ICacheBase>();
            _mediatorMock = new Mock<IMediator>();
            _repository = new Mock<IRepository<EmailAccount>>();
            _service = new EmailAccountService(_repository.Object,_cacheMock.Object,_mediatorMock.Object);
        }

        [TestMethod]
        public async Task InsertEmailAccount_InvokeExpectedMethods()
        {
            await _service.InsertEmailAccount(new EmailAccount());
            _repository.Verify(c => c.InsertAsync(It.IsAny<EmailAccount>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<EmailAccount>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(),It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateEmailAccount_InvokeExpectedMethods()
        {
            await _service.UpdateEmailAccount(new EmailAccount());
            _repository.Verify(c => c.UpdateAsync(It.IsAny<EmailAccount>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<EmailAccount>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task DeleteEmailAccount_InvokeExpectedMethods()
        {

            _cacheMock.Setup(c => c.GetAsync<List<EmailAccount>>(It.IsAny<string>(), It.IsAny<Func<Task<List<EmailAccount>>>>()))
              .Returns(Task.FromResult(new List<EmailAccount>() { new EmailAccount(),new EmailAccount() }));
            await _service.DeleteEmailAccount(new EmailAccount());
            _repository.Verify(c => c.DeleteAsync(It.IsAny<EmailAccount>()), Times.Once);
            _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<EmailAccount>>(), default), Times.Once);
            _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public void  DeleteEmailAccount_ExistOnlyOneAccount_ThrowException()
        {
            //we can't delete account if exist only one
            _cacheMock.Setup(c => c.GetAsync<List<EmailAccount>>(It.IsAny<string>(), It.IsAny<Func<Task<List<EmailAccount>>>>()))
              .Returns(Task.FromResult(new List<EmailAccount>() { new EmailAccount() }));
            Assert.ThrowsExceptionAsync<GrandException>(async () => await _service.DeleteEmailAccount(new EmailAccount()));
        }
    }
}
