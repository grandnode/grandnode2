using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class ReturnBackRedeemedLoyaltyPointsCommandHandlerTests
{
    private ReturnBackRedeemedLoyaltyPointsCommandHandler _handler;
    private Mock<ILoyaltyPointsService> _loyaltyPointsServiceMock;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Init()
    {
        _loyaltyPointsServiceMock = new Mock<ILoyaltyPointsService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _handler = new ReturnBackRedeemedLoyaltyPointsCommandHandler(_loyaltyPointsServiceMock.Object,
            _translationServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new ReturnBackRedeemedLoyaltyPointsCommand {
            Order = new Order { RedeemedLoyaltyPoints = 10 }
        };
        _translationServiceMock.Setup(x => x.GetResource(It.IsAny<string>())).Returns("Name");
        //Act
        await _handler.Handle(command, CancellationToken.None);

        _loyaltyPointsServiceMock.Verify(
            c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), It.Is<int>(g => g < 0), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()), Times.Once);
    }
}