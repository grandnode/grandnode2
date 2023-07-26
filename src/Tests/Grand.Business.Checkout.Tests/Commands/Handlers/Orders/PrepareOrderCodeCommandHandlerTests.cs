using Grand.Business.Checkout.Commands.Handlers.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders
{
    [TestClass()]
    public class PrepareOrderCodeCommandHandlerTests
    {
        private PrepareOrderCodeCommandHandler _handler;
        const int LengthCode = 6;

        [TestInitialize]
        public void Init()
        {
            _handler = new PrepareOrderCodeCommandHandler(new Domain.Orders.OrderSettings() { LengthCode = LengthCode });
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Act
            var result = await _handler.Handle(new Core.Commands.Checkout.Orders.PrepareOrderCodeCommand(), CancellationToken.None);
            //Assert
            Assert.AreEqual(LengthCode, result.Length);
        }
    }
}