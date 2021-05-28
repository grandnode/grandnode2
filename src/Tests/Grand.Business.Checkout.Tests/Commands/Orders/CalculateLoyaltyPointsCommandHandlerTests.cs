using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Tests.Commands.Orders
{
    [TestClass]
    public class CalculateLoyaltyPointsCommandHandlerTests
    {
        private LoyaltyPointsSettings _settings;
        private CalculateLoyaltyPointsCommandHandler _handler;

        [TestInitialize]
        public void Init()
        {
            _settings = new LoyaltyPointsSettings();
            _handler = new CalculateLoyaltyPointsCommandHandler(_settings);
        }

        [TestMethod]
        public async Task Handle_NullCustomer_ReturnZero()
        {
            _settings.Enabled = true;
            _settings.PointsForPurchases_Amount = 20;
            var command = new CalculateLoyaltyPointsCommand()
            {
                Customer = null
            };
            var result = await _handler.Handle(command, default);
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public async Task Handle_NotEnable_ReturnZero()
        {
            _settings.Enabled = false;
            _settings.PointsForPurchases_Amount = 20;
            var command = new CalculateLoyaltyPointsCommand()
            {
                Customer = new Domain.Customers.Customer()
            };
            var result = await _handler.Handle(command, default);
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public async Task Handle_ReturnExpectedValue()
        {
            _settings.Enabled = true;
            _settings.PointsForPurchases_Amount = 10;
            _settings.PointsForPurchases_Points = 2;
            var command = new CalculateLoyaltyPointsCommand()
            {
                Customer = new Domain.Customers.Customer(),
                Amount=100
            };
            
            Assert.AreEqual(20, await _handler.Handle(command, default));
            command.Amount = 200;
            Assert.AreEqual(40, await _handler.Handle(command, default));
            command.Amount = 375;
            Assert.AreEqual(75, await _handler.Handle(command, default));
        }
    }
}
