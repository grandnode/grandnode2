using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class ReturnBackRedeemedLoyaltyPointsCommandHandlerTests
{
    private ReturnBackRedeemedLoyaltyPointsCommandHandler _handler;
    private Mock<ILoyaltyPointsService> _loyaltyPointsServiceMock;
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Init()
    {
        _loyaltyPointsServiceMock = new Mock<ILoyaltyPointsService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _languageServiceMock = new Mock<ILanguageService>();
        _languageServiceMock.Setup(c => c.GetLanguageById("lang1")).ReturnsAsync(new Language { Id = "lang1" });
        _storeServiceMock = new Mock<IStoreService>();
        _handler = new ReturnBackRedeemedLoyaltyPointsCommandHandler(_loyaltyPointsServiceMock.Object,
            _translationServiceMock.Object, _languageServiceMock.Object,
            _storeServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var command = new ReturnBackRedeemedLoyaltyPointsCommand {
            Order = new Order { RedeemedLoyaltyPoints = 10, CustomerLanguageId = "lang1" }
        };
        //the message is written in the language the order was placed in
        _translationServiceMock.Setup(x => x.GetResource(It.IsAny<string>(), "lang1", It.IsAny<string>(),
            It.IsAny<bool>())).Returns("Name");
        //Act
        await _handler.Handle(command, CancellationToken.None);

        _loyaltyPointsServiceMock.Verify(
            c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), It.Is<int>(g => g < 0), It.IsAny<string>(),
                "Name", It.IsAny<string>(), It.IsAny<double>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_OrderWithoutLanguage_WritesTheMessageInTheStoreDefaultLanguage()
    {
        //Arrange
        var command = new ReturnBackRedeemedLoyaltyPointsCommand {
            Order = new Order { RedeemedLoyaltyPoints = 10, StoreId = "store1", CustomerLanguageId = null }
        };
        _storeServiceMock.Setup(c => c.GetStoreById("store1"))
            .ReturnsAsync(new Store { Id = "store1", DefaultLanguageId = "store-lang" });
        _languageServiceMock.Setup(c => c.GetAllLanguages(false, "store1"))
            .ReturnsAsync(new List<Language> { new() { Id = "first-lang" }, new() { Id = "store-lang" } });
        _translationServiceMock.Setup(x => x.GetResource(It.IsAny<string>(), "store-lang", It.IsAny<string>(),
            It.IsAny<bool>())).Returns("Store");
        //Act
        await _handler.Handle(command, CancellationToken.None);

        _loyaltyPointsServiceMock.Verify(
            c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), It.Is<int>(g => g < 0), "store1",
                "Store", It.IsAny<string>(), It.IsAny<double>()), Times.Once);
        _translationServiceMock.Verify(x => x.GetResource(It.IsAny<string>(), null, It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
    }
}