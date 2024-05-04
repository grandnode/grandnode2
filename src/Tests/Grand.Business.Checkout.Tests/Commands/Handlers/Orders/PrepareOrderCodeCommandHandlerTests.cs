using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class PrepareOrderCodeCommandHandlerTests
{
    private const int LengthCode = 6;
    private PrepareOrderCodeCommandHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _handler = new PrepareOrderCodeCommandHandler(new OrderSettings { LengthCode = LengthCode });
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Act
        var result = await _handler.Handle(new PrepareOrderCodeCommand(), CancellationToken.None);
        //Assert
        Assert.AreEqual(LengthCode, result.Length);
    }
}