using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Module.ScheduledTasks.BackgroundServices;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Services.BackgroundService;

[TestClass]
public class EndAuctionsTaskTests
{
    private Mock<IAuctionService> _auctionMock;
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<ILogger<EndAuctionsTask>> _loggerMock;
    private Mock<IMessageProviderService> _messageProviderMock;
    private LanguageSettings _settings;
    private Mock<IShoppingCartService> _shoppingCartMock;
    private EndAuctionsTask _task;

    [TestInitialize]
    public void Init()
    {
        _auctionMock = new Mock<IAuctionService>();
        _messageProviderMock = new Mock<IMessageProviderService>();
        _shoppingCartMock = new Mock<IShoppingCartService>();
        _customerServiceMock = new Mock<ICustomerService>();
        _loggerMock = new Mock<ILogger<EndAuctionsTask>>();
        _settings = new LanguageSettings { DefaultAdminLanguageId = "admin-lang" };
        _languageServiceMock = new Mock<ILanguageService>();
        _languageServiceMock.Setup(c => c.GetLanguageById("admin-lang"))
            .ReturnsAsync(new Language { Id = "admin-lang" });
        _task = new EndAuctionsTask(_auctionMock.Object, _messageProviderMock.Object, _settings,
            _shoppingCartMock.Object, _customerServiceMock.Object, _languageServiceMock.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task Execute_NotBids_InvokeExpectedMethos()
    {
        _auctionMock.Setup(c => c.GetAuctionsToEnd()).ReturnsAsync(new List<Product> { new() { Id = "id" } });
        _auctionMock.Setup(c => c.GetBidsByProductId(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(() => new PagedList<Bid>());
        await _task.Execute();
        _auctionMock.Verify(c => c.UpdateAuctionEnded(It.IsAny<Product>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Once);
        _messageProviderMock.Verify(
            c => c.SendAuctionEndedStoreOwnerMessage(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<Bid>()),
            Times.Once);
    }


    [TestMethod]
    public async Task Execute_HasWarnings_InvokeExpectedMethos()
    {
        _auctionMock.Setup(c => c.GetAuctionsToEnd()).ReturnsAsync(new List<Product> { new() { Id = "id" } });
        _auctionMock.Setup(c => c.GetBidsByProductId(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(() => new PagedList<Bid> { new() });
        _shoppingCartMock.Setup(c => c.AddToCart(It.IsAny<Customer>(),
                It.IsAny<Language>(), It.IsAny<string>(), It.IsAny<ShoppingCartType>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IList<CustomAttribute>>(), It.IsAny<double?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ShoppingCartValidatorOptions>()))
            .ReturnsAsync((new List<string> { "warning" }, null));
        await _task.Execute();

        //the warnings only reach the log, so they are written in the admin language
        _shoppingCartMock.Verify(c => c.AddToCart(It.IsAny<Customer>(),
            It.Is<Language>(l => l.Id == "admin-lang"), It.IsAny<string>(),
            It.IsAny<ShoppingCartType>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<IList<CustomAttribute>>(), It.IsAny<double?>(), It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<ShoppingCartValidatorOptions>()), Times.Once);
        _loggerMock.Verify(c => c.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Once);
    }

    [TestMethod]
    public async Task Execute_Valid_InvokeExpectedMethos()
    {
        _auctionMock.Setup(c => c.GetAuctionsToEnd()).ReturnsAsync(new List<Product> { new() { Id = "id" } });
        _auctionMock.Setup(c => c.GetBidsByProductId(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(() => new PagedList<Bid> { new() });
        _shoppingCartMock.Setup(c => c.AddToCart(It.IsAny<Customer>(),
                It.IsAny<Language>(), It.IsAny<string>(), It.IsAny<ShoppingCartType>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IList<CustomAttribute>>(), It.IsAny<double?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<ShoppingCartValidatorOptions>()))
            .ReturnsAsync((new List<string>(), null));
        await _task.Execute();

        _loggerMock.Verify(c => c.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Never);
        _auctionMock.Verify(c => c.UpdateBid(It.IsAny<Bid>()), Times.Once);
        _auctionMock.Verify(c => c.UpdateAuctionEnded(It.IsAny<Product>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Once);
        _messageProviderMock.Verify(
            c => c.SendAuctionEndedStoreOwnerMessage(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<Bid>()),
            Times.Once);
        _messageProviderMock.Verify(
            c => c.SendAuctionWinEndedCustomerMessage(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<Bid>()),
            Times.Once);
        _messageProviderMock.Verify(
            c => c.SendAuctionEndedLostCustomerMessage(It.IsAny<Product>(), It.IsAny<string>(), It.IsAny<Bid>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Execute_DeletedAdminLanguage_UsesAPublishedLanguage()
    {
        _languageServiceMock.Setup(c => c.GetLanguageById("admin-lang")).ReturnsAsync((Language)null);
        _languageServiceMock.Setup(c => c.GetAllLanguages(false, ""))
            .ReturnsAsync(new List<Language> { new() { Id = "published-lang" } });
        _auctionMock.Setup(c => c.GetAuctionsToEnd()).ReturnsAsync(new List<Product> { new() { Id = "id" } });
        _auctionMock.Setup(c => c.GetBidsByProductId(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(() => new PagedList<Bid> { new() });
        _shoppingCartMock.Setup(c => c.AddToCart(It.IsAny<Customer>(), It.IsAny<Language>(), It.IsAny<string>(),
                It.IsAny<ShoppingCartType>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IList<CustomAttribute>>(), It.IsAny<double?>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<ShoppingCartValidatorOptions>()))
            .ReturnsAsync((new List<string>(), null));

        await _task.Execute();

        _shoppingCartMock.Verify(c => c.AddToCart(It.IsAny<Customer>(),
            It.Is<Language>(l => l.Id == "published-lang"), It.IsAny<string>(), It.IsAny<ShoppingCartType>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<CustomAttribute>>(), It.IsAny<double?>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ShoppingCartValidatorOptions>()), Times.Once);
    }

    [TestMethod]
    public async Task Execute_NoLanguageAtAll_SkipsTheBidWithoutThrowing()
    {
        _languageServiceMock.Setup(c => c.GetLanguageById("admin-lang")).ReturnsAsync((Language)null);
        _languageServiceMock.Setup(c => c.GetAllLanguages(It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(new List<Language>());
        _auctionMock.Setup(c => c.GetAuctionsToEnd()).ReturnsAsync(new List<Product> { new() { Id = "id" } });
        _auctionMock.Setup(c => c.GetBidsByProductId(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(() => new PagedList<Bid> { new() });

        await _task.Execute();

        _shoppingCartMock.VerifyNoOtherCalls();
        _auctionMock.Verify(c => c.UpdateAuctionEnded(It.IsAny<Product>(), It.IsAny<bool>(), It.IsAny<bool>()),
            Times.Never);
    }
}