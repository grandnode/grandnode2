using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.System.Commands.Handlers.Catalog;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.System.Tests.Commands
{
    [TestClass]
    public class SendOutBidCustomerNotificationCommandHandlerTests
    {
        private Mock<IMessageProviderService> _messageProviderMock;
        private SendOutBidCustomerNotificationCommandHandler _handler;

        [TestInitialize]
        public void Init()
        {
            _messageProviderMock = new Mock<IMessageProviderService>();
            _handler = new SendOutBidCustomerNotificationCommandHandler(_messageProviderMock.Object);
        }

        [TestMethod]
        public async Task Handle_InvokeExpectedMethod()
        {
            var command = new SendOutBidCustomerCommand() {
                Product = new Domain.Catalog.Product(),
                Bid = new Domain.Catalog.Bid(),
                Language = new Domain.Localization.Language()
            };
            await _handler.Handle(command, default);
            _messageProviderMock.Verify(c => c.SendOutBidCustomerMessage(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<Bid>()));
        }
    }
}
