using Grand.Business.Messages.Commands.Handlers;
using Grand.Business.Messages.Commands.Models;
using Grand.Domain.Common;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Tests.Commands
{
    [TestClass]
    public class InsertContactUsCommandHandlerTests
    {
        private Mock<IRepository<ContactUs>> _repositoryMock;
        private InsertContactUsCommandHandler _handler;

        [TestInitialize]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<ContactUs>>();
            _handler = new InsertContactUsCommandHandler(_repositoryMock.Object);
        }

        [TestMethod]
        public async Task Handler_InsertEntity()
        {
            var command = new InsertContactUsCommand() {
                ContactAttributeDescription = "d",
                Email = "grand@gmail.com",
                ContactAttributes = new List<CustomAttribute>()
            };

            await _handler.Handle(command, default);
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<ContactUs>()), Times.Once);
        }
    }
}
