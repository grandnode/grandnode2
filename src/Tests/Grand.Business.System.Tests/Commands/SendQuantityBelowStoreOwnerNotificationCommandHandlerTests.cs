using Grand.Business.Catalog.Commands.Models;
using Grand.Business.Messages.Interfaces;
using Grand.Business.System.Commands.Handlers.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.System.Tests.Commands
{
    [TestClass]
    public class SendQuantityBelowStoreOwnerNotificationCommandHandlerTests
    {
        private Mock<IMessageProviderService> _messageProviderMock;
        private LanguageSettings _settings;
        private SendQuantityBelowStoreOwnerNotificationCommandHandler _handler;

        [TestInitialize]
        public void Init()
        {
            _messageProviderMock = new Mock<IMessageProviderService>();
            _settings = new LanguageSettings();
            _handler = new SendQuantityBelowStoreOwnerNotificationCommandHandler(_messageProviderMock.Object, _settings);
        }

        [TestMethod]
        public async Task Handle_InvokeExpectedMethods()
        {
            _settings.DefaultAdminLanguageId = "1";
            var command = new SendQuantityBelowStoreOwnerCommand()
            {
                Product = new Domain.Catalog.Product(),
                ProductAttributeCombination = null
            };

            await _handler.Handle(command, default);
            _messageProviderMock.Verify(c => c.SendQuantityBelowStoreOwnerMessage(It.IsAny<Product>(), It.IsAny<string>()), Times.Once);
            command.ProductAttributeCombination = new ProductAttributeCombination();
            await _handler.Handle(command, default);
            _messageProviderMock.Verify(c => c.SendQuantityBelowStoreOwnerMessage(It.IsAny<Product>(),It.IsAny<ProductAttributeCombination>(), It.IsAny<string>()), Times.Once);
        }
    }
}
