using Grand.Business.Checkout.Events.Customers;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Customers;

[TestClass]
public class CustomerRegisteredEventHandlerTests
{
    private CustomerRegisteredEventHandler _customerRegisteredEventHandler;
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<ILoyaltyPointsService> _loyaltyPointsServiceMock;
    private LoyaltyPointsSettings _loyaltyPointsSettings;
    private Mock<IStoreService> _storeServiceMock;

    [TestInitialize]
    public void Init()
    {
        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(x => x.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>()))
            .Returns((string name, string languageId, string _, bool _) => $"{name}@{languageId}");
        _loyaltyPointsServiceMock = new Mock<ILoyaltyPointsService>();
        _storeServiceMock = new Mock<IStoreService>();
        _languageServiceMock = new Mock<ILanguageService>();
        _loyaltyPointsSettings = new LoyaltyPointsSettings { Enabled = true, PointsForRegistration = 10 };
        _customerRegisteredEventHandler = new CustomerRegisteredEventHandler(translationServiceMock.Object,
            _loyaltyPointsServiceMock.Object, _loyaltyPointsSettings, _storeServiceMock.Object,
            _languageServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest_LoyaltyPointsSettings_Enabled()
    {
        //Act
        await _customerRegisteredEventHandler.Handle(
            new CustomerRegisteredEvent(new Customer(), new Language { Id = "lang1" }), CancellationToken.None);
        //Assert
        _loyaltyPointsServiceMock.Verify(
            c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<double>()), Times.Once);
    }

    [TestMethod]
    public async Task HandleTest_LoyaltyPointsSettings_Disabled()
    {
        _loyaltyPointsSettings.Enabled = false;
        //Act
        await _customerRegisteredEventHandler.Handle(
            new CustomerRegisteredEvent(new Customer(), new Language { Id = "lang1" }), CancellationToken.None);
        //Assert
        _loyaltyPointsServiceMock.Verify(
            c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<double>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_WritesTheMessageInTheLanguageOfTheEvent()
    {
        //Act
        await _customerRegisteredEventHandler.Handle(
            new CustomerRegisteredEvent(new Customer { StoreId = "store1" }, new Language { Id = "lang1" }),
            CancellationToken.None);
        //Assert
        _loyaltyPointsServiceMock.Verify(
            c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), 10, "store1",
                "LoyaltyPoints.Message.EarnedForRegistration@lang1", It.IsAny<string>(), It.IsAny<double>()),
            Times.Once);
        _storeServiceMock.Verify(c => c.GetStoreById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Handle_WithoutLanguage_WritesTheMessageInTheStoreDefaultLanguage()
    {
        _storeServiceMock.Setup(c => c.GetStoreById("store1"))
            .ReturnsAsync(new Store { Id = "store1", DefaultLanguageId = "store-lang" });
        //Act
        await _customerRegisteredEventHandler.Handle(
            new CustomerRegisteredEvent(new Customer { StoreId = "store1" }, null), CancellationToken.None);
        //Assert
        _loyaltyPointsServiceMock.Verify(
            c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), 10, "store1",
                "LoyaltyPoints.Message.EarnedForRegistration@store-lang", It.IsAny<string>(), It.IsAny<double>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithoutLanguageAndStoreDefault_WritesTheMessageInTheFirstStoreLanguage()
    {
        _storeServiceMock.Setup(c => c.GetStoreById("store1")).ReturnsAsync(new Store { Id = "store1" });
        _languageServiceMock.Setup(c => c.GetAllLanguages(false, "store1"))
            .ReturnsAsync(new List<Language> { new() { Id = "first-lang" } });
        //Act
        await _customerRegisteredEventHandler.Handle(
            new CustomerRegisteredEvent(new Customer { StoreId = "store1" }, null), CancellationToken.None);
        //Assert
        _loyaltyPointsServiceMock.Verify(
            c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), 10, "store1",
                "LoyaltyPoints.Message.EarnedForRegistration@first-lang", It.IsAny<string>(), It.IsAny<double>()),
            Times.Once);
    }
}