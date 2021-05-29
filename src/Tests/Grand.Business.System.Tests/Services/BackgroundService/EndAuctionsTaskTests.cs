using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Messages.Interfaces;
using Grand.Business.System.Services.BackgroundServices.ScheduleTasks;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.System.Tests.Services.BackgroundService
{
    [TestClass]
    public class EndAuctionsTaskTests
    {
        private Mock<IAuctionService> _auctionMock;
        private Mock<IMessageProviderService> _messageProviderMock;
        private Mock<IShoppingCartService> _shoppingCartMock;
        private Mock<ICustomerService> _customerServiceMock;
        private Mock<ILogger> _loggerMock;
        private LanguageSettings _settings;
        private EndAuctionsTask _task;

        [TestInitialize]
        public void Init()
        {
            _auctionMock = new Mock<IAuctionService>();
            _messageProviderMock = new Mock<IMessageProviderService>();
            _shoppingCartMock = new Mock<IShoppingCartService>();
            _customerServiceMock = new Mock<ICustomerService>();
            _loggerMock = new Mock<ILogger>();
            _settings = new LanguageSettings();
            _task = new EndAuctionsTask(_auctionMock.Object,_messageProviderMock.Object,_settings,_shoppingCartMock.Object,_customerServiceMock.Object,
                _loggerMock.Object);
        }

        [TestMethod]
        public async Task Execute_NotBids_InvokeExpectedMethos()
        {
            _auctionMock.Setup(c => c.GetAuctionsToEnd()).ReturnsAsync(new List<Product>() { new Product() {Id="id" } });
            _auctionMock.Setup(c => c.GetBidsByProductId(It.IsAny<string>(),It.IsAny<int>(),It.IsAny<int>())).ReturnsAsync(()=> new PagedList<Bid>());
            await _task.Execute();
            _auctionMock.Verify(c => c.UpdateAuctionEnded(It.IsAny<Product>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            _messageProviderMock.Verify(c => c.SendAuctionEndedStoreOwnerMessage(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<Bid>()), Times.Once);
        }


        [TestMethod]
        public async Task Execute_HasWarnings_InvokeExpectedMethos()
        {
            _auctionMock.Setup(c => c.GetAuctionsToEnd()).ReturnsAsync(new List<Product>() { new Product() { Id = "id" } });
            _auctionMock.Setup(c => c.GetBidsByProductId(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(() => new PagedList<Bid>() { new Bid()});
            _shoppingCartMock.Setup(c => c.AddToCart(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<ShoppingCartType>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IList<CustomAttribute>>(), It.IsAny<double?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<string>() { "warning" });
            await _task.Execute();

            _loggerMock.Verify(c => c.InsertLog(Domain.Logging.LogLevel.Error, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Customer>()), Times.Once);
        }

        [TestMethod]
        public async Task Execute_Valid_InvokeExpectedMethos()
        {
            _auctionMock.Setup(c => c.GetAuctionsToEnd()).ReturnsAsync(new List<Product>() { new Product() { Id = "id" } });
            _auctionMock.Setup(c => c.GetBidsByProductId(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(() => new PagedList<Bid>() { new Bid() });
            _shoppingCartMock.Setup(c => c.AddToCart(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<ShoppingCartType>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IList<CustomAttribute>>(), It.IsAny<double?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(new List<string>() );
            await _task.Execute();

            _loggerMock.Verify(c => c.InsertLog(Domain.Logging.LogLevel.Error, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Customer>()), Times.Never);
            _auctionMock.Verify(c => c.UpdateBid(It.IsAny<Bid>()), Times.Once);
            _auctionMock.Verify(c => c.UpdateAuctionEnded(It.IsAny<Product>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            _messageProviderMock.Verify(c => c.SendAuctionEndedStoreOwnerMessage(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<Bid>()), Times.Once);
            _messageProviderMock.Verify(c => c.SendAuctionWinEndedCustomerMessage(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<Bid>()), Times.Once);
            _messageProviderMock.Verify(c => c.SendAuctionEndedLostCustomerMessage(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<Bid>()), Times.Once);
        }
    }
}
