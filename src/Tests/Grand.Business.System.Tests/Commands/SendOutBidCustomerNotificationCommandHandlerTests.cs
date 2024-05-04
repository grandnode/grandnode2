using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.System.Commands.Handlers.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.System.Tests.Commands;

[TestClass]
public class SendOutBidCustomerNotificationCommandHandlerTests
{
    private SendOutBidCustomerNotificationCommandHandler _handler;
    private Mock<IMessageProviderService> _messageProviderMock;

    [TestInitialize]
    public void Init()
    {
        _messageProviderMock = new Mock<IMessageProviderService>();
        _handler = new SendOutBidCustomerNotificationCommandHandler(_messageProviderMock.Object);
    }

    [TestMethod]
    public async Task Handle_InvokeExpectedMethod()
    {
        var command = new SendOutBidCustomerCommand {
            Product = new Product(),
            Bid = new Bid(),
            Language = new Language()
        };
        await _handler.Handle(command, default);
        _messageProviderMock.Verify(c =>
            c.SendOutBidCustomerMessage(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<Bid>()));
    }
}