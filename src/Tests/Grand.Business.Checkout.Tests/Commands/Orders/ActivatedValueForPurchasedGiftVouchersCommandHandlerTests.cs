using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Messages.Interfaces;
using Grand.Domain;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Tests.Commands.Orders
{
    [TestClass]
    public class ActivatedValueForPurchasedGiftVouchersCommandHandlerTests
    {
        private Mock<IGiftVoucherService> _giftVoucherMock;
        private Mock<ILanguageService> _langService;
        private Mock<IMessageProviderService> _messageProviderMock;
        private ActivatedValueForPurchasedGiftVouchersCommandHandler _handler;

        [TestInitialize]
        public void Init()
        {
            _giftVoucherMock = new Mock<IGiftVoucherService>();
            _langService = new Mock<ILanguageService>();
            _messageProviderMock = new Mock<IMessageProviderService>();
            _handler = new ActivatedValueForPurchasedGiftVouchersCommandHandler(_giftVoucherMock.Object, _langService.Object, _messageProviderMock.Object);
        }

        [TestMethod]
        public async Task Handle_InvokeExpectedMethods()
        {
            var command = new ActivatedValueForPurchasedGiftVouchersCommand()
            {
                Order = new Order()
            };
            var gift = new GiftVoucher() { RecipientEmail = "fdsf", SenderEmail = "aaa",IsGiftVoucherActivated=false };
            command.Activate = true;
            command.Order.OrderItems.Add(new OrderItem() { });
            _langService.Setup(c => c.GetLanguageById(It.IsAny<string>())).ReturnsAsync(new Language());
            _giftVoucherMock.Setup(c => c.GetAllGiftVouchers(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PagedList<GiftVoucher>() {gift });

            await _handler.Handle(command, default);
            Assert.IsTrue(gift.IsGiftVoucherActivated);
            _messageProviderMock.Verify(c => c.SendGiftVoucherMessage(gift, command.Order, It.IsAny<string>()), Times.Once);
            _giftVoucherMock.Verify(c => c.UpdateGiftVoucher(gift));
        }

        [TestMethod]
        public async Task Handle_Deactivate()
        {
            var command = new ActivatedValueForPurchasedGiftVouchersCommand()
            {
                Order = new Order()
            };
            var gift = new GiftVoucher() { RecipientEmail = "fdsf", SenderEmail = "aaa", IsGiftVoucherActivated = true };
            command.Activate = false;
            command.Order.OrderItems.Add(new OrderItem() { });
            _langService.Setup(c => c.GetLanguageById(It.IsAny<string>())).ReturnsAsync(new Language());
            _giftVoucherMock.Setup(c => c.GetAllGiftVouchers(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new PagedList<GiftVoucher>() { gift });

            await _handler.Handle(command, default);
            Assert.IsFalse(gift.IsGiftVoucherActivated);
            _giftVoucherMock.Verify(c => c.UpdateGiftVoucher(gift));
        }
    }
}
