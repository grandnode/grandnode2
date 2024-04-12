using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Messages.Commands.Handlers.Common;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Domain.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Commands;

[TestClass]
public class InsertContactUsCommandHandlerTests
{
    private InsertContactUsCommandHandler _handler;
    private Mock<IRepository<ContactUs>> _repositoryMock;

    [TestInitialize]
    public void Init()
    {
        _repositoryMock = new Mock<IRepository<ContactUs>>();
        _handler = new InsertContactUsCommandHandler(_repositoryMock.Object);
    }

    [TestMethod]
    public async Task Handler_InsertEntity()
    {
        var command = new InsertContactUsCommand {
            ContactAttributeDescription = "d",
            Email = "grand@gmail.com",
            ContactAttributes = new List<CustomAttribute>()
        };

        await _handler.Handle(command, default);
        _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<ContactUs>()), Times.Once);
    }
}