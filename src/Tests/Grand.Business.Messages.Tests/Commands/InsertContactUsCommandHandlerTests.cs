using Grand.Business.Messages.Commands.Handlers;
using Grand.Business.Messages.Commands.Models;
using Grand.Domain.Common;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Tests.Commands
{
    [TestClass]
    public class InsertContactUsCommandHandlerTests
    {
        private Mock<IRepository<ContactUs>> _repositoryMock;
        private Mock<IHttpContextAccessor> _accessor;
        private InsertContactUsCommandHandler _handler;

        [TestInitialize]
        public void Init()
        {
            _repositoryMock = new Mock<IRepository<ContactUs>>();
            _accessor = new Mock<IHttpContextAccessor>();
            _handler = new InsertContactUsCommandHandler(_repositoryMock.Object,_accessor.Object);
        }

        [TestMethod]
        public async Task Handler_InsertEntity()
        {
            var httpContextMock = new Mock<HttpContext>();
            _accessor.Setup(c => c.HttpContext).Returns(httpContextMock.Object);
            var command = new InsertContactUsCommand()
            {
                ContactAttributeDescription = "d",
                Email = "grand@gmail.com",
                ContactAttributes = new List<CustomAttribute>()
            };

            await _handler.Handle(command, default);
            _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<ContactUs>()), Times.Once);
        }
    }
}
